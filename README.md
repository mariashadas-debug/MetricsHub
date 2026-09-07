# MetricsHub

MetricsHub is a real-time infrastructure and system monitoring platform built as a professional portfolio project. It will collect CPU, RAM, disk, and network metrics, retain historical measurements, maintain fast current-state data, and present live operational insight in a web dashboard.

## Purpose

The project demonstrates how to incrementally design and deliver a production-minded monitoring system with clear architectural boundaries.

## Technology stack

- .NET 10, C# 14, ASP.NET Core 10 Web API, and REST APIs
- Blazor Web App
- Entity Framework Core and MySQL
- Redis, SignalR, and background services (planned)
- Dependency injection and structured logging
- xUnit
- Docker (planned)

## High-level architecture

MetricsHub follows Clean Architecture. Domain is the dependency-free core. Application builds future use cases on Domain. Infrastructure will implement technical concerns required by Application. Api is the HTTP host and composition root. Web is the separate user interface.

```text
Api -> Infrastructure -> Application -> Domain
 |                         ^
 +-------------------------+

Web (independent in Phase 1)
```

## Solution structure

```text
MetricsHub/
|-- src/
|   |-- MetricsHub.Domain/
|   |-- MetricsHub.Application/
|   |-- MetricsHub.Infrastructure/
|   |-- MetricsHub.Api/
|   `-- MetricsHub.Web/
|-- tests/
|   |-- MetricsHub.UnitTests/
|   `-- MetricsHub.IntegrationTests/
|-- .gitignore
|-- MetricsHub.sln
`-- README.md
```

## Project responsibilities

- **MetricsHub.Domain**: Core entities, enums, and domain rules. Its objects protect required values and lifecycle state without depending on persistence or web frameworks.
- **MetricsHub.Application**: Device and telemetry use cases, response models, validation, and focused persistence interfaces. It references only Domain and DI abstractions.
- **MetricsHub.Infrastructure**: EF Core persistence, MySQL mappings, migrations, and future infrastructure services. It references Domain and Application.
- **MetricsHub.Api**: Versioned REST controllers, request contracts, centralized ProblemDetails handling, dependency composition, health endpoint, and development OpenAPI document.
- **MetricsHub.Web**: Minimal Blazor Web App. No monitoring dashboard or external connections have been added.
- **MetricsHub.UnitTests**: xUnit tests for Domain and, in future phases, Application behavior.
- **MetricsHub.IntegrationTests**: xUnit project prepared for future API and Infrastructure integration tests. It contains no database tests.

## Development roadmap

1. **Phase 1 - Solution architecture** — complete
2. **Phase 2 - Domain model** — complete
3. **Phase 3 - MySQL and EF Core** — complete
4. **Phase 4 - REST API** — complete/current
5. **Phase 5 - Metrics simulator**
6. **Phase 6 - Redis**
7. **Phase 7 - SignalR and alert processing**
8. **Phase 8 - Blazor monitoring dashboard**
9. **Phase 9 - Real system metrics agent**
10. **Phase 10 - Docker, testing and production hardening**

## Current status

**Phase 4 - REST API** is complete. Devices can be managed and telemetry can be ingested and queried through application-layer use cases backed by MySQL. Redis and SignalR are not implemented; they belong to later phases.

## REST API

The `/api/v1` API exposes application use cases without returning EF entities or navigation collections. Controllers translate HTTP contracts while Application owns validation and orchestration.

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/devices` | List devices |
| `GET` | `/api/v1/devices/{id}` | Get one device |
| `POST` | `/api/v1/devices` | Register a device |
| `PUT` | `/api/v1/devices/{id}` | Update mutable device details |
| `DELETE` | `/api/v1/devices/{id}` | Delete a device without historical records |
| `POST` | `/api/v1/telemetry` | Ingest a metric batch |
| `GET` | `/api/v1/devices/{deviceId}/telemetry` | Query telemetry history |
| `GET` | `/api/v1/devices/{deviceId}/telemetry/latest` | Get the latest value per metric type |

Register a device:

```json
{
  "deviceKey": "windows-pc-01",
  "name": "Development PC",
  "type": "Workstation",
  "hostname": "DEV-PC-01",
  "operatingSystem": "Windows 11",
  "location": "Zlin"
}
```

`DeviceKey` is the stable unique identity used by a simulator or agent when sending telemetry. Updates deliberately cannot change it.

Ingest multiple measurements atomically:

```json
{
  "deviceKey": "windows-pc-01",
  "timestamp": "2026-09-07T18:30:00Z",
  "metrics": [
    { "type": "CpuUsage", "value": 43.8, "unit": "%" },
    { "type": "MemoryUsage", "value": 68.2, "unit": "%" }
  ]
}
```

Each metric becomes one `TelemetryPoint`; the batch and device state update use one save operation. Successful ingestion sets the device `Online` and advances `LastSeenAt` when the accepted timestamp is newer. Disabled devices return a conflict.

All enums use readable JSON strings. API timestamps must be explicit ISO-8601 UTC values ending in `Z`; unspecified and local timestamps are rejected.

History is newest-first and supports `metricType`, `from`, `to`, and `limit`:

```text
GET /api/v1/devices/{deviceId}/telemetry?metricType=CpuUsage&from=2026-09-07T00:00:00Z&to=2026-09-08T00:00:00Z&limit=500
```

The limit defaults to 500 and must be between 1 and 1,000. Filtering, ordering, and limiting run in MySQL with no-tracking queries.

The latest endpoint returns one newest point per metric type. Metrics may have different timestamps. Its top-level `timestamp` is the newest timestamp among returned metrics, or `null` when none exist.

Status behavior:

- `200 OK` for reads.
- `201 Created` for device registration.
- `204 No Content` for updates, deletion, and telemetry acceptance.
- `400 Bad Request` for malformed input, invalid enum strings, UTC values, ranges, or limits.
- `404 Not Found` for unknown devices or device keys.
- `409 Conflict` for duplicate keys, disabled devices, or deletion blocked by history.
- `500` ProblemDetails for unexpected errors, without internal details.

In Development, the OpenAPI document is available at `/openapi/v1.json`.

## Domain model

- **Device** represents a uniquely keyed monitored server, workstation, virtual machine, container, or custom system. It owns collections of its telemetry, alerts, and alert rules and starts enabled with an unknown status.
- **TelemetryPoint** represents one historical metric measurement for a device, including its metric type, numeric value, unit, and UTC timestamp.
- **AlertRule** describes a threshold comparison for a metric. A rule can belong to one device or remain global for future default-rule support.
- **Alert** records a detected condition for a device. It starts unresolved and can be resolved once through domain behavior that records the UTC resolution time.

The domain uses these enums:

- **DeviceType** classifies devices as `Server`, `Workstation`, `VirtualMachine`, `Container`, or `Custom`.
- **DeviceStatus** represents `Unknown`, `Online`, `Offline`, `Warning`, or `Critical` health.
- **MetricType** identifies CPU, memory, disk, inbound network, outbound network, or uptime measurements.
- **AlertSeverity** classifies alerts and rules as `Info`, `Warning`, or `Critical`.
- **ComparisonOperator** expresses greater-than, greater-than-or-equal, less-than, less-than-or-equal, or equality threshold comparisons.

## MySQL persistence

MySQL provides the relational store for device metadata, historical telemetry, alert rules, and alerts. EF Core maps domain objects to that schema through `MetricsHubDbContext`; the Domain project remains unaware of EF Core and MySQL. Oracle's `MySql.EntityFrameworkCore` provider is used because it has a stable EF Core 10-compatible release, while Pomelo does not yet have a stable EF Core 10 release.

The initial schema contains:

- `Devices`, with a unique device key.
- `TelemetryPoints`, related to devices and indexed for device/time and metric/time queries.
- `AlertRules`, optionally related to a device so global rules remain possible.
- `Alerts`, related to a device and optionally to the rule that produced them.

Deleting a device is restricted while historical telemetry or alerts still reference it. Deleting optional associations clears their foreign key instead of removing alert history. UTC value converters restore `DateTimeKind.Utc` when MySQL values are materialized.

### Start MySQL locally

Create a local environment file and replace its example-only passwords:

```powershell
Copy-Item .env.example .env
docker compose up -d
docker compose ps
```

Docker Compose starts only MySQL 8.4, maps port 3306 by default, performs a health check, and stores database files in the `metricshub_mysql_data` volume.

### Configure the application

.NET maps a double underscore in environment-variable names to a configuration section separator. Set `ConnectionStrings__MySql` to the connection string matching your `.env` values:

```powershell
$env:ConnectionStrings__MySql='Server=localhost;Port=3306;Database=metricshub;User=metricshub_dev;Password=your-local-password'
```

No connection credentials are committed. `appsettings.json` retains an empty placeholder, and environment variables override it locally.

### Apply and inspect migrations

Restore the repository-local EF tool, then apply the migration:

```powershell
dotnet tool restore
dotnet ef database update --project src/MetricsHub.Infrastructure --startup-project src/MetricsHub.Infrastructure
dotnet ef migrations list --project src/MetricsHub.Infrastructure --startup-project src/MetricsHub.Infrastructure
```

The design-time context reads `ConnectionStrings__MySql`. The application does not automatically apply migrations at startup.

Inspect the local database with the MySQL client inside the container:

```powershell
docker compose exec mysql sh -c 'mysql -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" metricshub'
```

Useful SQL commands include `SHOW TABLES;` and `DESCRIBE TelemetryPoints;`.

### Run integration tests

Persistence integration tests use Testcontainers to create and automatically remove an isolated MySQL 8.4 container. Docker Desktop must be running:

```powershell
$env:METRICSHUB_RUN_INTEGRATION_TESTS='true'
dotnet test tests/MetricsHub.IntegrationTests
```

Without that explicit opt-in, the Docker-backed tests are reported as skipped. They never substitute EF Core's in-memory provider.

The suite includes direct persistence tests and full REST API tests hosted through `WebApplicationFactory` against real MySQL.

## Restore and build

Install the .NET 10 SDK, then run from the repository root:

```powershell
dotnet restore
dotnet build
dotnet test
```

## Run MetricsHub.Api

```powershell
dotnet run --project src/MetricsHub.Api
```

Open the HTTPS URL printed in the terminal and append `/health`. In Development, the OpenAPI document is available at `/openapi/v1.json`.

## Run MetricsHub.Web

```powershell
dotnet run --project src/MetricsHub.Web
```

Open the URL printed in the terminal. The current UI is the minimal Blazor starter application; the monitoring dashboard is planned for Phase 8.

## Configuration and secrets

`src/MetricsHub.Api/appsettings.json` contains empty `MySql` and `Redis` placeholders. MySQL uses `ConnectionStrings__MySql`; Redis remains unused until Phase 6. Keep credentials outside tracked configuration through user secrets, environment variables, or ignored local files.
