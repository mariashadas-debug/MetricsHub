# MetricsHub

MetricsHub is a real-time infrastructure and system monitoring platform built as a professional portfolio project. It will collect CPU, RAM, disk, and network metrics, retain historical measurements, maintain fast current-state data, and present live operational insight in a web dashboard.

## Purpose

The project demonstrates how to incrementally design and deliver a production-minded monitoring system with clear architectural boundaries.

## Technology stack

- .NET 10, C# 14, ASP.NET Core 10 Web API, and REST APIs
- Blazor Web App
- Entity Framework Core and MySQL (planned)
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
- **MetricsHub.Application**: Use cases, services, DTOs, and interfaces. It references only Domain.
- **MetricsHub.Infrastructure**: Future persistence, caching, and infrastructure services. It references Domain and Application; no external systems are connected yet.
- **MetricsHub.Api**: ASP.NET Core HTTP host. It exposes `GET /health`, uses dependency injection, enables HTTPS redirection, and publishes an OpenAPI document in Development.
- **MetricsHub.Web**: Minimal Blazor Web App. No monitoring dashboard or external connections have been added.
- **MetricsHub.UnitTests**: xUnit tests for Domain and, in future phases, Application behavior.
- **MetricsHub.IntegrationTests**: xUnit project prepared for future API and Infrastructure integration tests. It contains no database tests.

## Development roadmap

1. **Phase 1 - Solution architecture** — complete
2. **Phase 2 - Domain model** — complete/current
3. **Phase 3 - MySQL and EF Core**
4. **Phase 4 - REST API**
5. **Phase 5 - Metrics simulator**
6. **Phase 6 - Redis**
7. **Phase 7 - SignalR and alert processing**
8. **Phase 8 - Blazor monitoring dashboard**
9. **Phase 9 - Real system metrics agent**
10. **Phase 10 - Docker, testing and production hardening**

## Current status

**Phase 2 - Domain model** is complete. Phase 1 architecture remains intact, and the dependency-free domain now models monitored devices, historical telemetry, alert rules, and alerts. Database persistence will be added in Phase 3; no database or infrastructure implementation exists yet.

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

`src/MetricsHub.Api/appsettings.json` contains empty `MySql` and `Redis` connection-string placeholders. They are unused in Phase 1. Keep future credentials outside tracked configuration by using .NET user secrets, environment variables, or ignored local configuration files.
