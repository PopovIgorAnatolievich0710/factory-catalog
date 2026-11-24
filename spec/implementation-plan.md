# Factory Catalog + Balancer — Phase 1 implementation plan

> **Revision note (Nov 24).** Sections 2–5 were written from the Nov 23 version of the requirements. The pages were
> revised on Nov 24 and a UI mockup was added (`spec/mockup/`). Where this plan differs, the order of precedence is:
> `spec/requirements/*.md` → `spec/mockup/Phase 1 UI Changes.md` → this plan. Known differences: a Shift manager
> persona; factory managers edit runs-here / machine type / labor cost / max daily output for their own factory;
> alternatives carry "for balancer" / "for S.C. planner" flags; component usage is Static / Dynamic; processes have a
> unit ("measured in") and SKU–process mappings carry a quantity and no step; list filters are column-header filters
> with typed operators (`filters: { [field]: { values?, operator?, value?, valueTo? } }`); the change-log tab is
> "Audit"; the balancer (not the catalog) consumes TG Supplier sent / reproduced events and fires
> `balancer.quote.produced`; Daily plan and Transfers pages were added to the Balancer. The authoritative data model
> and permission matrix are in `spec/backend/database.md` and `spec/backend/permissions.md`.

Source requirements: Confluence space FRBT, folder "Phase 1" (pages copied to `spec/requirements/`).
Reference architecture: `C:\shakuro\shipment service` (backend layering, CQRS, DI, auth, frontend stack, spec approach).

Every decision below that the requirements leave open is implemented with the recommended variant and logged in
`spec/open-questions.md` so it can be answered and re-implemented later.

---

## 1. Shape of the solution

One repository, two deployable backends, one admin UI.

| Deployable | Projects | Port | DB schema | Modules |
|---|---|---|---|---|
| Factory Catalog API | `Tenengroup.FactoryCatalog.*` | 5010 | `catalog`, `hangfire` | 1 Components, 2 Process capacity, 3 SKUs, catalog side of 4 (quotes) |
| Balancer API | `Tenengroup.Balancer.*` | 5011 | `balancer`, `hangfire_balancer` | 4 Balancer |
| Factory Catalog UI | `frontend/FactoryCatalogUI` | 8080 | — | all four |

```
backend/src/
├── factory-catalog.sln
├── app/
│   ├── Tenengroup.FactoryCatalog.Api
│   ├── Tenengroup.FactoryCatalog.Business.Abstractions
│   ├── Tenengroup.FactoryCatalog.Business
│   ├── Tenengroup.FactoryCatalog.Domains
│   ├── Tenengroup.FactoryCatalog.DataAccess.Abstractions
│   ├── Tenengroup.FactoryCatalog.DataAccess
│   ├── Tenengroup.FactoryCatalog.CompositionRoot
│   └── Tenengroup.Balancer.{Api,Business.Abstractions,Business,Domains,DataAccess.Abstractions,DataAccess,CompositionRoot}
├── infrastructure/
│   └── Tenengroup.Infrastructure.Common        # mediator (IRequestRunner), exceptions, paging, auth, excel export
└── tests/unit-tests/
    ├── Tenengroup.FactoryCatalog.Business.UnitTests
    └── Tenengroup.Balancer.Business.UnitTests
frontend/FactoryCatalogUI/                       # React 18 + TS + Vite + Redux + MUI + MSAL + CASL, bo-frontend submodule
devops/                                          # Dockerfiles, docker-compose, Helm, Jenkinsfile
spec/                                            # same layout as the shipment service spec
```

Stack is the shipment service stack: .NET 10, ASP.NET Core, EF Core 10 + Npgsql + snake_case, Hangfire (PostgreSQL),
Serilog, Swagger, Basic Auth + Azure AD JWT, AWS S3/SQS/SNS. No private `TG` NuGet feed packages: the few needed
pieces (mediator, DI helpers) are copied into `Tenengroup.Infrastructure.Common`.

Coding conventions are the shipment service ones (see `CLAUDE.md`): no comments, CQRS strictly, explicit scoped DI
in `CompositionRoot`, no `Async` suffix, no speculative code, snake_case DB.

---

## 2. Shared conventions (contract between backend and frontend)

JSON is camelCase, enums are strings, dates are ISO-8601 UTC. All routes are under `/v1`.

| Purpose | Endpoint | Shape |
|---|---|---|
| List | `POST /v1/{resource}/search` | req `{ page, pageSize, search, sortBy, sortDirection: "asc"\|"desc", filters: { [field]: string[] } }` → `{ items: [], totalCount }` |
| Excel export of the current view | `POST /v1/{resource}/export` | list request + `columns: string[]` → xlsx, file name `{page}_{yyyyMMdd_HHmmss}.xlsx` |
| Single | `GET /v1/{resource}/{id}` | entity DTO |
| History tab | `GET /v1/history/{entityType}/{entityId}?page&pageSize` | `{ items: [{ changedBy, changedAt, source: "User"\|"Sync"\|"Import"\|"System", factoryId, field, oldValue, newValue }], totalCount }` |
| Column picker memory | `GET` / `PUT /v1/user/list-settings/{listKey}` | `{ columns: string[] }` (max 8 extra columns) |
| Current user | `GET /v1/user/me` | `{ email, name, roles: string[], factoryIds: string[] }` |
| Factories | `GET /v1/factories` | `[{ id, name, countryCode, isActive }]` |
| Sync status | `GET /v1/sync/{syncType}/status` | `{ syncType, status, startedAt, finishedAt, lastSuccessAt, added, updated, failed, error }` |
| Feature toggles | `GET /v1/feature-toggle` | as in the shipment service |

Errors: 400/422 with `{ message, errors: { [field]: string[] } }`, 403 for permission failures, 404 for missing entities, 409 for conflicts (e.g. sync already running).

---

## 3. Auth, roles, permissions

Azure AD JWT for UI users, Basic Auth for machine-to-machine (balancer → catalog, legacy balancer → catalog).
Role resolution copies the shipment service (`GroupPermissions.json`, `IRoleResolver`, fail closed, `permissions.enable_all` toggle).

| Role (AD group) | Persona |
|---|---|
| `FC-FactoryManagers` | Factory manager |
| `FC-SupplyChainManagers` | Supply chain manager |
| `FC-CatalogAdmins` | Factory catalog admin |
| `FC-ProductionFloor` | Production floor user |
| `FC-OperationsManagers` | Operations / GO manager |
| `FC-Finance` | Finance |
| `BO_Administrator` | everything |

Permissions (enforced in backend `PermissionsHelper` and mirrored in frontend CASL):

| Permission | FactoryManager | SupplyChain | CatalogAdmin | ProductionFloor | Operations | Finance |
|---|---|---|---|---|---|---|
| `Components.View` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Components.EditManualOos` (own factory) | ✓ | | | | | |
| `Components.EditCriticalLevels` | | ✓ | | | | |
| `Components.EditAlternatives` | | ✓ | | | | |
| `Components.ViewCost` | | | ✓ | | | ✓ |
| `Sync.Monitor` / `Sync.Trigger` | | | ✓ | | | |
| `Processes.View` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Processes.Edit` (processes, factory setup, labor cost, import) | | | ✓ | | | |
| `Processes.EditCapacity` (own factory) | ✓ | | | | | |
| `Processes.ViewCost` | | | ✓ | | | ✓ |
| `Skus.View` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `Skus.EditProducible` / `Skus.EditShareOverride` (own factory) | ✓ | | | | | |
| `Skus.EditGlobalShare` / `Skus.EditMappings` | | | ✓ | | | |
| `Balancer.View` | ✓ | ✓ | ✓ | | ✓ | ✓ |
| `Balancer.ViewDashboards` / `Balancer.EditReservations` | | | | | ✓ | |
| `Balancer.ViewDimCost` | | | | | | ✓ |

"Own factory" is resolved from the `user_factory` table (user email → factory ids), maintained by the catalog admin.

---

## 4. Data model

### `catalog` schema (owned by the foundation step, all four modules' tables are created up front)

Shared: `factory`, `user_factory`, `user_list_settings`, `history_entry`, `sync_run`, `feature_toggle`.

Module 1: `component` (PK `erp_id`), `component_factory` (stock warehouse/production floor, critical level override,
manual OOS set by/at/note), `component_alternative` (one-way), `component_replacement` (C-15).
Derived, never stored: `stockStatus` (ok/low/outOfStock), `oosConflict`, `actionRequired`, `usedInSkus`.

Module 2: `process` (PK `process_id`, unique `name`, type machine/work, status), `process_factory` (runs here,
machine type, capacity pct, reason, expected recovery, updated by/at, labor cost amount/currency/effective from).
`PACKING` is a seeded work process.

Module 3: `sku` (PK `sku_uuid`, product type, status, dropshipper id + total cost, validation status + issues,
last master sync), `sku_alias`, `sku_component`, `sku_process`, `sku_factory` (producible, override share),
`global_factory_share`.

Module 4 (catalog side): `quote_set`, `quote`, `balancer_skip_log`, `production_event`.

### `balancer` schema (owned by the Balancer agent)

`shipping_route_cache`, `allocation` (+ candidates json), `allocation_item_cost` (quoted vs actual DIM),
`legacy_decision` (shadow comparison), `capacity_reservation`, `allocation_transfer`.

Migrations: module agents never generate migrations. One `Initial` migration per DbContext is regenerated by the
orchestrator after all modules are merged (new project, no deployed database yet).

---

## 5. Integrations (all contracts are assumptions until the owning team confirms)

| Integration | Direction | Implementation |
|---|---|---|
| SAP component master + stock | S3 → catalog | Hangfire job daily 02:00 UTC; CSV `components.csv`, `stock.csv` under a dated prefix; `ISapExportReader` |
| Master catalog SKUs | master → catalog | `IMasterCatalogClient` (HTTP, paged "changed since"); daily Hangfire job + "Sync now"; event consumer left for later |
| Legacy balancer checks | supplier_api → catalog | `POST /v1/legacy-balancer/eligibility` (OOS + 0% capacity), skip log; `GET /v1/legacy-balancer/country-sku-lists` |
| TG Supplier replacement screen | supplier_api → catalog | `GET /v1/components/{erpId}/alternatives?factoryId=`, `POST /v1/components/replacements` |
| Production quotes | balancer → catalog | `POST /v1/quotes`, `POST /v1/quotes/{quoteId}/select` |
| Paid order items | OCS → balancer | SQS consumer + `POST /v1/allocations` |
| Shipping routes | shipment service → balancer | daily Hangfire refresh into `shipping_route_cache` via `IShippingRouteClient` |
| Allocation event | balancer → SNS | `balancer.allocation.decided` |
| Item sent / reproduced | TG Supplier → catalog | SQS consumer → `production_event` |

---

## 6. Execution

### Wave 0 — foundation (2 agents in parallel, same checkout, disjoint folders)

| Agent | Owns | Delivers |
|---|---|---|
| F-BE | `backend/`, `spec/backend/`, `spec/requirements/`, `CLAUDE.md` | solution, layering, mediator, auth + permissions, all `catalog` entities + DbContext, shared endpoints from section 2, history writer, excel exporter, sync-run tracking, Hangfire, S3 reader, per-module DI stubs, unit-test project, builds green |
| F-FE | `frontend/`, `spec/frontend/` | Vite app, MSAL, CASL, layout + navigation, http client for both APIs, list-page kit (search, filters, column picker ≤ 8 with memory, excel export), single-view tabs shell, history tab, sync-status badge, per-module route stubs, builds green |

Orchestrator then commits `foundation` and creates one git worktree per module agent.

### Wave 1 — modules (6 agents in parallel, one worktree + branch each)

Story ids below are those of the revised requirement pages (Nov 24, 76 stories) in `spec/requirements/`.
UI follows the mockup in `spec/mockup/` (`Phase 1 UI Changes.md` is the written UI spec).

| Agent | Stories | UI pages | Owns (nobody else edits these) |
|---|---|---|---|
| M1 Components | C-03 … C-16 | Components (list + detail), Component stock levels | `*/Components/**` in every backend project, `src/app/components-catalog/**`, `spec/modules/1-components.md` |
| M2 Process capacity | P-01 … P-09, P-11 … P-14 | Processes (list + detail), Process capacity | `*/Processes/**`, `src/app/processes/**`, `spec/modules/2-process-capacity.md` |
| M3 SKUs | S-01 … S-15, S-19, S-20 | SKU (list + detail), Factory share, global-share Settings | `*/Skus/**`, `src/app/skus/**`, `spec/modules/3-skus.md` |
| M4A Catalog balancer API | C-17, C-18, P-10, S-16, S-17, S-18, B-01, B-02, B-03, catalog side of B-10 / B-21 (quote selected / produced) | — | `*/Quotes/**`, `*/LegacyBalancer/**`, `spec/modules/4a-quotes.md` |
| M4B Balancer service + UI | B-04 … B-24 | Routing dashboard, Balancer decision log, Daily plan, Transfers | all `Tenengroup.Balancer.*`, `src/app/balancer/**`, `spec/modules/4b-balancer.md` |
| DevOps | C-01 (infrastructure part) | — | `devops/**`, Dockerfiles, `docker-compose.yml`, `spec/devops/overview.md` |

C-01 (auth, roles) and C-02 (UI conventions) are delivered by the foundation wave.

Rules for wave 1: stay inside owned paths; shared files (DbContext, `Root.cs`, router, permissions, `.sln`) are
touched only through the stubs prepared by the foundation; no migrations; every assumption goes to
`spec/questions/<agent>.md`; each agent finishes with `dotnet build` + tests and `npm run build` green in its worktree
and commits to its branch.

### Wave 2 — integration (orchestrator)

Merge the six branches, regenerate the `Initial` migrations, full build + unit tests + frontend build,
consolidate `spec/questions/*.md` into `spec/open-questions.md`, finalize the spec index, report.

---

## 7. Out of scope for this pass

- Changes inside other repositories (supplier_api legacy balancer and replacement screen, OCS, shipment service,
  master catalog). This repo exposes the endpoints and consumers those teams will call; the callers are follow-ups.
- Real infrastructure provisioning (Terraform apply, AD groups, Azure app registration, S3 buckets, queues).
- Phase 2 / Phase 3 items listed in the phase overview.
