# MetricsHub

MetricsHub is a real-time infrastructure and system monitoring platform built as a professional portfolio project. It will collect CPU, RAM, disk, and network metrics, retain historical measurements, maintain fast current-state data, and present live operational insight in a web dashboard.

## Purpose

The project demonstrates how to incrementally design and deliver a production-minded monitoring system with clear architectural boundaries.

## Technology stack

- .NET 10, C# 14, ASP.NET Core 10 Web API, and REST APIs
- Blazor Web App
- Entity Framework Core and MySQL
- Redis current-state storage; SignalR and background services (planned)
- Dependency injection and structured logging
- xUnit
- Docker (planned)

## High-level architecture

MetricsHub follows Clean Architecture. Domain is the dependency-free core. Application owns use cases and infrastructure-facing abstractions. Infrastructure implements MySQL history and Redis current state. Api is the HTTP host and composition root. Web remains a separate user interface.

```text
DeviceSimulator
      |
      v
 ASP.NET API
      |
 Application
    /     \
   v       v
MySQL    Redis
History  Current state

Web (independent; real-time dashboard not implemented)
```

## Solution structure

```text
MetricsHub/
|-- src/
|   |-- MetricsHub.Domain/
|   |-- MetricsHub.Application/
|   |-- MetricsHub.Infrastructure/
|   |-- MetricsHub.Api/
|   |-- MetricsHub.DeviceSimulator/
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
- **MetricsHub.Application**: Device and telemetry use cases, response/state models, validation, and focused persistence interfaces. It references only Domain and framework abstractions.
- **MetricsHub.Infrastructure**: EF Core/MySQL history persistence plus the StackExchange.Redis current-state implementation. It references Domain and Application.
- **MetricsHub.Api**: Versioned REST controllers, request contracts, centralized ProblemDetails handling, dependency composition, health endpoint, and development OpenAPI document.
- **MetricsHub.DeviceSimulator**: Standalone development console client that registers fake infrastructure machines and sends realistic telemetry over the public REST API.
- **MetricsHub.Web**: Minimal Blazor Web App. No monitoring dashboard or external connections have been added.
- **MetricsHub.UnitTests**: xUnit tests for Domain and, in future phases, Application behavior.
- **MetricsHub.IntegrationTests**: xUnit project prepared for future API and Infrastructure integration tests. It contains no database tests.

## Development roadmap

1. **Phase 1 - Solution architecture** — complete
2. **Phase 2 - Domain model** — complete
3. **Phase 3 - MySQL and EF Core** — complete
4. **Phase 4 - REST API** — complete
5. **Phase 5 - Metrics simulator** — complete
6. **Phase 6 - Redis** — complete/current
7. **Phase 7 - SignalR and alert processing**
8. **Phase 8 - Blazor monitoring dashboard**
9. **Phase 9 - Real system metrics agent**
10. **Phase 10 - Docker, testing and production hardening**

## Current status

**Phase 6 - Redis current state** is complete. MySQL remains durable history while Redis supplies reconstructable, low-latency current device state.

## Device simulator

`MetricsHub.DeviceSimulator` lets MetricsHub behave like a populated monitoring platform before a real Windows/Linux agent exists. It references no server project and communicates only through HTTP, using its own small copies of the public request contracts.

The default configuration runs five independent fake machines:

- `windows-dev-01` — Windows development workstation.
- `windows-server-01` — Windows application server.
- `linux-server-01` — Linux database server.
- `vm-app-01` — lightly loaded application virtual machine.
- `container-host-01` — busy container host.

Each device reports `CpuUsage`, `MemoryUsage`, `DiskUsage`, `NetworkIn`, `NetworkOut`, and `Uptime`. Percent metrics remain between 0 and 100. Network traffic consistently uses `KB/s`, and uptime uses seconds.

Values use stateful bounded random walks rather than unrelated random samples. CPU and memory move toward profile-specific baselines, disk changes very slowly, and network traffic follows a noisier bounded path. Low-probability CPU, memory, and network spikes create short bursts that gradually return toward normal. Uptime advances by elapsed interval time and is never regenerated per request.

At startup, the client queries `GET /api/v1/devices`, reuses matching `DeviceKey` values, and registers missing devices through `POST /api/v1/devices`. A registration race returning 409 is treated as success. Each device then sends batches independently to `POST /api/v1/telemetry`. Temporary API failures are logged and retried in later cycles without terminating other simulations.

Configuration lives in `src/MetricsHub.DeviceSimulator/appsettings.json`. Useful environment overrides include:

```powershell
$env:MetricsHub__ApiBaseUrl='https://localhost:7091'
$env:Simulation__IntervalSeconds='5'
$env:Simulation__Devices__0__DeviceKey='custom-dev-01'
```

Double underscores map to nested .NET configuration keys, and numeric segments address array entries.

Run MySQL and apply migrations as described below, start the API, then launch the simulator in another terminal:

```powershell
dotnet run --project src/MetricsHub.Api
dotnet run --project src/MetricsHub.DeviceSimulator
```

Press `Ctrl+C` to cancel all device loops and shut down gracefully. For local HTTPS certificate trust, run:

```powershell
dotnet dev-certs https --trust
```

Do not bypass certificate validation. Verify generated data with:

```text
GET /api/v1/devices
GET /api/v1/devices/{deviceId}/telemetry?limit=100
GET /api/v1/devices/{deviceId}/telemetry/latest
```

The simulator remains unaware of Redis and sends the same public HTTP payloads. SignalR, the Blazor real-time dashboard, offline detection, alert evaluation, and real Windows/Linux metric collection are not implemented yet; they belong to later phases.

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
| `GET` | `/api/v1/devices/{deviceId}/state` | Get reconstructable current state, primarily from Redis |

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

Each metric becomes one historical `TelemetryPoint`. The batch and device changes are committed to MySQL with one save operation before Redis is updated. Successful ingestion sets the device `Online` and advances `LastSeenAt` when the accepted timestamp is newer. Disabled devices return a conflict. If Redis is temporarily unavailable after that durable commit, the request still succeeds and the failure is logged; current state can be rebuilt from MySQL.

All enums use readable JSON strings. API timestamps must be explicit ISO-8601 UTC values ending in `Z`; unspecified and local timestamps are rejected.

History is newest-first and supports `metricType`, `from`, `to`, and `limit`:

```text
GET /api/v1/devices/{deviceId}/telemetry?metricType=CpuUsage&from=2026-09-07T00:00:00Z&to=2026-09-08T00:00:00Z&limit=500
```

The limit defaults to 500 and must be between 1 and 1,000. Filtering, ordering, and limiting run in MySQL with no-tracking queries.

The latest endpoint returns one newest point per metric type. Metrics may have different timestamps. Its top-level `timestamp` is the newest timestamp among returned metrics, or `null` when none exist.

## Redis current state

Redis is an optimization for dashboard-oriented current reads; it never replaces MySQL historical storage. Each device uses one Redis hash named `metricshub:device:{deviceId}:state`. Metadata fields store the device key, status, and last-seen time. Each metric has a readable JSON hash field and a companion numeric timestamp field.

Updates run through one atomic Lua script. Partial batches update only their included metric fields, so other latest values remain. Per-metric timestamp comparisons prevent older or concurrently delayed telemetry from moving current values backward. The same comparison protects `LastSeenAt` and status. Every successful state update refreshes a configurable 24-hour TTL (`Redis:StateTtlHours`); expiration is cache cleanup only, not offline detection.

`GET /api/v1/devices/{deviceId}/state` first verifies the device in MySQL and reads Redis. On a cache miss it executes the existing server-side latest-row-per-metric query, rebuilds the state (including an empty metric list for a new device), repopulates Redis, and returns it. A successful physical device deletion removes its state key; a history-blocked deletion leaves it intact.

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

### Start MySQL and Redis locally

Create a local environment file and replace its example-only passwords:

```powershell
Copy-Item .env.example .env
docker compose up -d
docker compose ps
```

Docker Compose starts MySQL 8.4 and Redis 8.10.1, health-checks both services, maps ports 3306 and 6379 by default, and persists them in separate named volumes.

### Configure the application

.NET maps a double underscore in environment-variable names to a configuration section separator. Set `ConnectionStrings__MySql` to the connection string matching your `.env` values:

```powershell
$env:ConnectionStrings__MySql='Server=localhost;Port=3306;Database=metricshub;User=metricshub_dev;Password=your-local-password'
$env:ConnectionStrings__Redis='localhost:6379'
```

Double underscores map to `ConnectionStrings:MySql` and `ConnectionStrings:Redis`. No credentials are committed; `appsettings.json` keeps empty connection placeholders. The Redis key prefix and TTL can also be overridden with `Redis__KeyPrefix` and `Redis__StateTtlHours`.

Inspect current state without logging secrets:

```powershell
docker compose exec redis redis-cli
SCAN 0 MATCH metricshub:device:*:state
HGETALL metricshub:device:{deviceId}:state
TTL metricshub:device:{deviceId}:state
```

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

Persistence/API integration tests use Testcontainers to create and automatically remove isolated MySQL 8.4 and Redis 8.10.1 containers. Docker Desktop must be running:

```powershell
$env:METRICSHUB_RUN_INTEGRATION_TESTS='true'
dotnet test tests/MetricsHub.IntegrationTests
```

Without that explicit opt-in, the Docker-backed tests are reported as skipped. They never substitute EF Core's in-memory provider.

The suite includes direct persistence tests and full REST API tests hosted through `WebApplicationFactory` against real MySQL and Redis. Redis tests cover partial and out-of-order updates, atomic same-device concurrency, cache rebuild, key cleanup, and durable MySQL ingestion during a Redis outage. Tests use unique device IDs and an isolated test Redis database; cache-sensitive cases flush that isolated database.

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

`src/MetricsHub.Api/appsettings.json` contains empty `MySql` and `Redis` connection placeholders. Use `ConnectionStrings__MySql` and `ConnectionStrings__Redis`. Keep credentials outside tracked configuration through user secrets, environment variables, or ignored local files.

SignalR and browser push are not implemented. The Blazor real-time dashboard is not implemented. Offline detection and alert evaluation are not implemented. The real system Agent is not implemented. Those capabilities remain later phases.
