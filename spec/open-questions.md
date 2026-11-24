# Open questions and assumed answers

Each row was implemented with the **recommended** variant. Answer in the last column; anything that differs from the
recommendation gets re-implemented. Agent-level questions are collected in `spec/questions/*.md` and merged here at
the end of the implementation pass.

## Architecture

| # | Question | Recommended (implemented) | Alternatives | Your answer |
|---|---|---|---|---|
| A-1 | Where does the factory catalog live: TG Supplier, back office, or standalone? (Phase overview, discovery Q) | Standalone service + back-office style UI, same stack as the shipment service, Active Directory roles | Inside TG Supplier (users not in AD); inside the BO monolith | |
| A-2 | Is the balancer a separate service? | Yes: `Tenengroup.Balancer.Api` in the same repo, own schema, calls the catalog over HTTP (`POST /v1/quotes`) as drawn in Module 4 | One service with a balancer module; separate repo | |
| A-3 | Repository name / location | `C:\shakuro\factory-catalog`, namespaces `Tenengroup.FactoryCatalog.*`, `Tenengroup.Balancer.*` | — | |
| A-4 | Jira project key for branch / commit conventions | Not set; commits use plain messages until a key exists | `FC-`, `FAST-` | |
| A-5 | One PostgreSQL database with two schemas, or two databases? | One database `factory_catalog`, schemas `catalog` and `balancer` | Database per service | |
| A-6 | Azure AD app registration for UI and API | Config placeholders; local dev reuses the shipment service registration ids and UI port 8080 | Dedicated registrations (needed before any deployed environment) | |
| A-7 | AD group names per persona | `FC-FactoryManagers`, `FC-SupplyChainManagers`, `FC-CatalogAdmins`, `FC-ProductionFloor`, `FC-OperationsManagers`, `FC-Finance`, plus `BO_Administrator` | Reuse existing groups | |
| A-8 | How is a factory manager bound to "own factory"? | `user_factory` table (email → factory ids) maintained by the catalog admin | One AD group per factory; claim in the token | |
| A-9 | Source of the factory list | `factory` table seeded with 110, 125, 315 (ids from the requirement samples), editable by migration/seed only | Sync from TG Supplier / shipment service packing facilities | |
| A-10 | Field-level history implementation | Explicit `history_entry` table written by command handlers and sync jobs | Audit.NET as in the shipment service | |

## Integrations (contracts assumed until the owning team confirms)

| # | Question | Recommended (implemented) | Your answer |
|---|---|---|---|
| I-1 | SAP export: S3 file format, schema, schedule, owner | Two UTF-8 CSV files per day under `s3://{bucket}/{prefix}/{yyyy-MM-dd}/`: `components.csv`, `stock.csv`; job at 02:00 UTC | |
| I-2 | Component unit cost source: SAP purchase terms or "Pricing Tool" sheet (C-09) | Optional columns in the SAP `components.csv`, stored with `source = "SAP"` | |
| I-3 | `countryOfOrigin` / `hsCode` needed in Phase 1? | Stored and shown when present in the export | |
| I-4 | Master catalog sync: events, API, or daily job | Daily pull through an HTTP client ("changed since" paging) + manual "Sync now"; event consumer deferred | |
| I-5 | Canonical SKU key | UUID from the master catalog; legacy Site / Factory SKUs kept as read-only deprecated aliases | |
| I-6 | Shipment service route cache contract | Shape from Module 4 section C, pulled once a day over HTTP | |
| I-7 | OCS → balancer order item contract (with CBD) | SQS message mirroring the quote request plus destination country, delivery promise, SLA, event-order flag | |
| I-8 | TG Supplier sent / reproduced events | SQS messages `{ type, orderId, orderItemId, factoryId, occurredAt }` | |
| I-9 | Allocation event transport | SNS topic, event name `balancer.allocation.decided`, shape from Module 4 section D | |
| I-10 | Legacy balancer integration | Catalog exposes eligibility + country SKU list endpoints; the call from supplier_api is a follow-up in that repo | |
