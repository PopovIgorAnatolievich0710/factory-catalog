# Factory Catalog + Balancer — AI Agent Guide

## What this project is

Phase 1 of the Tenengroup factory catalog: components (read-only from SAP, stock per factory, alternatives, manual
OOS), work processes and per-factory capacity, SKUs (read-only from the master catalog, with components, processes
and factory share) and a new balancer that routes order items on total cost and delivery time.

Two .NET 10 backends and one React admin UI live in this repository:

| Deployable | Projects | Port | DB schema |
|---|---|---|---|
| Factory Catalog API | `Tenengroup.FactoryCatalog.*` | 5010 | `catalog`, `hangfire` |
| Balancer API | `Tenengroup.Balancer.*` | 5011 | `balancer`, `hangfire_balancer` |
| Factory Catalog UI | `frontend/FactoryCatalogUI` | 8080 | — |

---

## Specification — read this first

All documentation lives in [`/spec`](./spec/README.md).

| What you need | File |
|---|---|
| Requirements (Confluence pages, verbatim — they win over the plan) | `spec/requirements/*.md` |
| UI mockup and its change log | `spec/mockup/` |
| Implementation plan, module ownership | `spec/implementation-plan.md` |
| Architecture, layers, DI, shared building blocks | `spec/backend/overview.md` |
| Shared REST conventions and endpoints | `spec/backend/api.md` |
| Shared domain entities and enums | `spec/backend/domain.md` |
| DB schema (every table and column), migrations | `spec/backend/database.md` |
| Integrations (SAP, master catalog, AWS) | `spec/backend/integrations.md` |
| Roles and permission matrix | `spec/backend/permissions.md` |
| Env vars, middleware, background jobs | `spec/backend/infrastructure.md` |
| Module specs (written by the module owners) | `spec/modules/*.md` |
| Frontend stack | `spec/frontend/overview.md` |
| Docker, Helm, Jenkins | `spec/devops/overview.md` |
| Open questions and assumptions | `spec/open-questions.md`, `spec/questions/*.md` |

When working on a feature, **read the relevant requirement page and spec file before touching code**.
When a change affects the architecture, domain, API contract or database — **update the spec**.

---

## Project layout

```
backend/src/
├── factory-catalog.sln
├── infrastructure/
│   └── Tenengroup.Infrastructure.Common              # mediator, exceptions, auth + permissions, paging/filters, excel, web setup, AWS
├── app/
│   ├── Tenengroup.FactoryCatalog.Api                 # Controllers, request models, Startup
│   ├── Tenengroup.FactoryCatalog.Business.Abstractions  # CQRS command/query records, DTOs, service interfaces
│   ├── Tenengroup.FactoryCatalog.Business            # Handlers, services
│   ├── Tenengroup.FactoryCatalog.Domains             # Entities, enums
│   ├── Tenengroup.FactoryCatalog.DataAccess.Abstractions  # IFactoryCatalogDbContext
│   ├── Tenengroup.FactoryCatalog.DataAccess          # EF Core context, configurations, migrations
│   ├── Tenengroup.FactoryCatalog.CompositionRoot     # DI wiring, Hangfire, recurring jobs
│   └── Tenengroup.Balancer.*                         # Balancer service (owned by the Balancer module)
└── tests/unit-tests/
    └── Tenengroup.FactoryCatalog.Business.UnitTests
frontend/FactoryCatalogUI/                            # React + TypeScript admin UI
devops/                                               # Dockerfiles, Helm, Jenkinsfile
spec/                                                 # Requirements and technical documentation
```

Every backend project has one folder per module: `Shared`, `Components`, `Processes`, `Skus`, `Quotes`,
`LegacyBalancer` (for example `Domains/Components`, `DataAccess/Configurations/Components`,
`Business/Components`, `Business.Abstractions/Components`, `Api/Controllers/Components`, `Api/Models/Components`).

---

## Coding conventions

- **No comments in code.** Code must be self-explanatory through naming.
- **No unused variables** — remove them entirely, do not rename to `_`.
- **No backwards-compatibility shims** — if something is removed, delete it completely.
- **Minimal abstraction** — do not create helpers for single-use operations.
- **No speculative code** — only implement what is explicitly required.
- **snake_case in DB** — the context uses `UseSnakeCaseNamingConvention()`; do not set column names by hand
  (only `jsonb` columns drop the `Json` suffix with `HasColumnName`). Enums are stored as strings.
- **CQRS strictly** — queries/commands are records in `Business.Abstractions`, handlers in `Business` implement
  `IRequestHandler<,>`, controllers only call `IRequestRunner`. No handler-to-handler calls.
- **Explicit scoped DI** — every service and handler is registered with `AddScoped` in the module file of
  `CompositionRoot/Modules`. There is no attribute scanning in this project. Never register a singleton unless it
  is stateless and thread-safe.
- **No `Async` suffix** on method names (`Get`, `Resolve`, `Save`, not `GetAsync`).
- **nuget.org only** — the private `TG` feed is not used; shared pieces live in `Tenengroup.Infrastructure.Common`.
- **API contract** — camelCase JSON, enums as PascalCase strings, ISO-8601 UTC dates, routes under `/v1`,
  list/export/error shapes exactly as in `spec/backend/api.md`.
- **Derived values are never stored** — stock status, OOS conflict, "used in SKUs", default and effective share,
  today's used/remaining capacity, validation of a quote are computed in handlers.
- **Time** — inject `TimeProvider`, do not call `DateTime.UtcNow` in business code.

---

## Module ownership (parallel work — never edit another owner's files)

| Owner | Folders it may edit |
|---|---|
| Components | `*/Components/**`, `CompositionRoot/Modules/ComponentsModule.cs`, `CompositionRoot/Jobs/ComponentsJobs.cs`, `spec/modules/1-components.md` |
| Processes | `*/Processes/**`, `Modules/ProcessesModule.cs`, `Jobs/ProcessesJobs.cs`, `spec/modules/2-process-capacity.md` |
| SKUs | `*/Skus/**`, `Modules/SkusModule.cs`, `Jobs/SkusJobs.cs`, `spec/modules/3-skus.md` |
| Catalog balancer API | `*/Quotes/**`, `*/LegacyBalancer/**`, `Modules/QuotesModule.cs`, `Modules/LegacyBalancerModule.cs`, `Jobs/QuotesJobs.cs`, `Jobs/LegacyBalancerJobs.cs`, `spec/modules/4a-quotes.md` |
| Balancer | all `Tenengroup.Balancer.*`, `spec/modules/4b-balancer.md` |
| DevOps | `devops/**`, Dockerfiles, `spec/devops/overview.md` |
| Foundation | everything else: `Tenengroup.Infrastructure.Common`, every `Shared` folder, `Root.cs`, `Startup.cs`, `FactoryCatalogDbContext`, `IFactoryCatalogDbContext`, migrations, `.sln`, `appsettings*.json`, `spec/backend/*` |

Rules:

- A module is added **only by adding files in its own folders and editing its own module / jobs file**. `Root.cs`
  already calls every `Add*Module`, `RecurringJobsHostedService` already calls every `Register*Jobs`, the
  `DbContext` already has every `DbSet`, controllers are discovered automatically.
- **Module owners do not add or change migrations.** If an entity or configuration in your module folder must
  change, change the entity/configuration only; the single `Initial` migration is regenerated after the merge.
- Do not edit shared files (`Root.cs`, `Startup.cs`, DbContext, `Permission` enum, `.sln`, `appsettings*.json`).
  Need something there? Write it to `spec/questions/<your-module>.md` and work around it inside your module
  (module-specific settings are read with `configuration.GetSection("<YourSection>")` in your module file).
- Every assumption goes to `spec/questions/<your-module>.md` as a row: question | implemented | alternatives.

---

## Key patterns to follow

### Adding an API endpoint

1. Command/Query record (+ DTOs) in `Business.Abstractions/<Module>`.
2. Handler in `Business/<Module>` implementing `IRequestHandler<TRequest, TResult>`.
3. `services.AddScoped<IRequestHandler<TRequest, TResult>, THandler>()` in `CompositionRoot/Modules/<Module>Module.cs`.
4. Controller action in `Api/Controllers/<Module>` with `[AzureAuthorize(Permission.X)]`; request models with
   validation attributes in `Api/Models/<Module>`.
5. Own-factory edits call `ICurrentUser.EnsureCanEditFactory(factoryId)` in the handler; every edit writes
   `IHistoryWriter.Record(...)` before `SaveChangesAsync`.
6. Unit tests in `tests/unit-tests/.../<Module>`; update `spec/modules/<module>.md`.

Lists follow `POST /v1/{resource}/search` and `POST /v1/{resource}/export` with `ListRequest` / `ExportRequest`
(see "Shared building blocks" in `spec/backend/overview.md`).

### Changing an entity

1. Edit the entity in `Domains/<Module>` and its configuration in `DataAccess/Configurations/<Module>`.
2. Do **not** run `dotnet ef migrations add`. Describe the change in `spec/modules/<module>.md`.

Foundation regenerates the migration:

```bash
dotnet ef migrations add Initial --context FactoryCatalogDbContext \
  --project backend/src/app/Tenengroup.FactoryCatalog.DataAccess \
  --startup-project backend/src/app/Tenengroup.FactoryCatalog.DataAccess
```

### Adding a background job

1. Job interface in `Business.Abstractions/<Module>`, implementation in `Business/<Module>`, `AddScoped` in the module file.
2. `recurringJobs.AddOrUpdate<IYourJob>("job-id", job => job.Run(CancellationToken.None), cron)` in
   `CompositionRoot/Jobs/<Module>Jobs.cs`.
3. Sync jobs wrap their work in `ISyncRunTracker.Start / Complete / Fail`.

---

## Build and test

```bash
dotnet build backend/src/factory-catalog.sln
dotnet test backend/src/factory-catalog.sln
```

Neither needs a database. Unit tests use NUnit + Moq + `Moq.EntityFrameworkCore`
(`TestDb.SetupSet` / `TestDb.FixedTime` helpers in the test project).

---

## Environment

- Runtime: .NET 10 / ASP.NET Core
- Database: PostgreSQL 15+, database `factory_catalog`, schemas `catalog`, `balancer`, `hangfire`, `hangfire_balancer`
- Ports: catalog `5010`, balancer `5011`, UI `8080`
- Auth: Basic Auth (machine-to-machine) + Azure AD JWT (UI users); roles from `GROUP_PERMISSIONS_URL`
- Logging: Serilog → stdout, levels from the `Serilog` config section
- Background jobs: Hangfire (PostgreSQL storage), dashboard at `/hangfire` (Basic auth)
- Branch / commit conventions: no Jira key yet (see `spec/open-questions.md` A-4); use plain messages
