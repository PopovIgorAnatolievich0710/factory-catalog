# Backend — Infrastructure & Configuration

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

This file covers the Factory Catalog API. The Balancer API (port 5011, schemas `balancer` / `hangfire_balancer`)
shares the database, auth, logging and AWS settings below and adds its own keys (`FactoryCatalog__*`,
`ShipmentService__*`, `Balancer__*`, queues and topics): see "Configuration" in
[`modules/4b-balancer.md`](../modules/4b-balancer.md). Deployment values: [`devops/overview.md`](../devops/overview.md).

## Environment variables (Factory Catalog API)

All configuration comes from `appsettings.json` keys that can be overridden by environment variables
(nested keys use `__`). `appsettings.Development.json` holds local values.

### Database

| Variable | Description |
|---|---|
| `DATABASE_HOST` | PostgreSQL host |
| `DATABASE_PORT` | PostgreSQL port |
| `DATABASE_NAME` | Database name (`factory_catalog`) |
| `DATABASE_USERNAME` | DB user |
| `DATABASE_PASSWORD` | DB password |

### API / auth

| Variable | Description |
|---|---|
| `Kestrel__Endpoints__Http__Url` | Listen address, default `http://0.0.0.0:5010` |
| `API_SERVICE_USERNAME` | Basic auth user for machine-to-machine callers and the Hangfire dashboard |
| `API_SERVICE_USER_PASSWORD` | Basic auth password. Empty user or password disables Basic auth |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` |
| `AzureAd__TenantId` | Azure AD tenant id |
| `AzureAd__ClientId` | App registration (audience) of the API |
| `GROUP_PERMISSIONS_URL` | URL of `GroupPermissions.json` (role → AD groups). Needed once `permissions.enable_all` is on: missing / unreachable → 403 for JWT users |
| `CORS_ALLOWED_HOSTS` | Comma-separated allowed origins (default `*`) |

### Logging (Serilog)

| Variable | Description |
|---|---|
| `Serilog__MinimumLevel__Default` | Global minimum level (`Information`) |
| `Serilog__MinimumLevel__Override__<SourceContext>` | Per-namespace level, e.g. `Serilog__MinimumLevel__Override__Hangfire=Debug` |

Logs go to stdout. `/ping` requests are not logged. Every log event of a request carries `CorrelationId`.

### AWS

| Variable | Description |
|---|---|
| `Aws__DefaultRegion` | AWS region (falls back to `eu-west-1`) |
| `Aws__AccessKey`, `Aws__SecretKey` | Static credentials; empty → default credential chain |
| `Aws__ServiceUrl` | Optional S3 endpoint override (LocalStack); forces path-style addressing |
| `Aws__SqsServiceUrl`, `Aws__SnsServiceUrl` | Optional SQS / SNS endpoint overrides |

### Integrations (read by the owning modules)

Contracts: SAP export in [`modules/1-components.md`](../modules/1-components.md), master catalog in
[`modules/3-skus.md`](../modules/3-skus.md).

| Variable | Description |
|---|---|
| `SapExport__Bucket` | S3 bucket of the daily SAP export |
| `SapExport__Prefix` | Key prefix; files are expected under `{prefix}/{yyyy-MM-dd}/` |
| `SapExport__CronExpression` | Schedule of the SAP sync (default `0 2 * * *`, UTC) |
| `MasterCatalog__BaseUrl` | Master catalog API base URL |
| `MasterCatalog__Username`, `MasterCatalog__Password` | Basic credentials for the master catalog API |
| `MasterCatalog__CronExpression` | Schedule of the SKU sync (default `0 3 * * *`, UTC) |

## Middleware

| Middleware | Purpose |
|---|---|
| Swagger UI | at `/`, document `/swagger/{version}/swagger.json`, Basic and Bearer security schemes |
| `/ping` | anonymous liveness probe (`pong`) |
| `CorrelationIdMiddleware` | reads or creates `X-Correlation-Id`, echoes it, pushes it to the Serilog context |
| CORS `corsapp` | origins from `CORS_ALLOWED_HOSTS`; exposes `Content-Disposition` and `X-Correlation-Id` |
| `ExceptionMiddleware` | maps exceptions to 404 / 409 / 403 / 422 / 500 with `{ message, errors }` |
| Authentication / authorization | see `overview.md` and `permissions.md` |

Model-binding errors are returned as 422 `{ message: "Validation failed", errors }`.

## Database and migrations

`FactoryCatalogDbContext` — default schema `catalog`, snake_case names, split queries, retry on failure,
migrations history table `catalog.__EFMigrationsHistory`. One `Initial` migration exists; it is regenerated after
the module branches are merged (no deployed database yet). `FactoryCatalogDbContextFactory` is the design-time
factory, so `dotnet ef` needs no live database. Apply migrations with the `migrate` argument of the API.

## Background jobs

Hangfire with PostgreSQL storage, schema `hangfire` (created on first start), one server with the `default`
queue. Dashboard: `/hangfire`, Basic auth with `API_SERVICE_USERNAME` / `API_SERVICE_USER_PASSWORD`.

Recurring jobs are registered on startup by `RecurringJobsHostedService`, which calls the per-module
`Register<Module>Jobs` methods in `CompositionRoot/Jobs`. The foundation registers none; the modules register
`sap-components-sync` (`SapExport:CronExpression`, [`modules/1-components.md`](../modules/1-components.md)) and
`master-catalog-skus-sync` (`MasterCatalog:CronExpression`, [`modules/3-skus.md`](../modules/3-skus.md)). The
Balancer's jobs and queue consumers are listed in [`modules/4b-balancer.md`](../modules/4b-balancer.md). The API needs a
reachable PostgreSQL to start (Hangfire storage); build and unit tests do not.

## Local run

```bash
dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api -- migrate
dotnet run --project backend/src/app/Tenengroup.FactoryCatalog.Api
```

Swagger: `http://localhost:5010/`.
