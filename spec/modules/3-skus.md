# Module 3 — SKUs

Stories S-01 … S-15, S-19, S-20 of `spec/requirements/3-skus.md` (S-16 … S-18 are delivered by the quotes module, see
`spec/modules/4a-quotes.md`). UI per `spec/mockup/Phase 1 UI Changes.md` sections 1 and 4.
Assumptions are logged in `spec/questions/m3-skus.md`.

## Domain

Entities (`Domains/Skus`, created by the foundation): `Sku` (PK `skuUuid`), `SkuAlias` (legacy factory SKUs),
`SkuComponent` (`quantity`, `usage` Static / Dynamic), `SkuProcess` (`quantity` in the process unit), `SkuFactory`
(`producible`, `overrideSharePct`, `overrideBy`, `overrideAt`), `GlobalFactoryShare` (one row per factory).
Module enums: `SkuShareSource` (`NotProducible`, `Default`, `Override`, `Rebalanced`) and `ProductionConflictKind`
(`ProcessNotListed`, `ProcessNotUsed`, `MachineMismatch`).

### Rules

- **Read only from the master catalog** (S-01, S-02): UUID, Site SKU, name, type, status, aliases, component mappings
  and dropshipper data are written only by the sync. Process mappings come only from the import (S-10); producible
  flags and overrides only from factory managers (S-13, S-14); the global share only from the catalog admin (S-12).
- **Retired SKUs** stay in the catalog with status `Retired`; nothing is deleted. A component or alias missing from
  a SKU's latest master payload is removed from that SKU (the payload is the full SKU, not a delta).
- **Aliases** are deprecated, read-only factory SKUs (TGN / TGO / TGH). An alias already owned by another SKU fails the
  incoming SKU (counted as `failed`).
- **Dropshipper SKUs** (S-19) keep id, name, dropshipper id / name and total cost. They have no components,
  processes or factory rows (the sync removes any it finds), are never validated as invalid, are left out of the
  Factory share screen and return only the Overview in the detail view.
- **Validation** (S-11), after every sync run (all SKUs) and every process-mapping import (imported SKUs):
  each mapped component must exist and be `Active` in `component`, each mapped process must exist in `process`.
  Issues: `Component {erpId} not found in SAP`, `Component {erpId} is not active in SAP`,
  `Process {processId} not found in the catalog`. Any issue → `validationStatus = Invalid`; the quotes module
  excludes invalid SKUs (`SKU_INVALID`).
- **Component stock per factory** on the SKU Components tab uses the Components module derivation on
  `ComponentFactory` (`GetTotalStock`, `GetStockAlertStatus`); a factory without a `component_factory` row is
  evaluated as an empty row (total 0 → `OutOfStock`).
- **Process capacity per factory** on the Processes tab: `runsHere`, `capacityPct` and `machineType` from
  `process_factory`; a factory without a row does not run the process.

### Factory share (S-12 … S-15)

- Default share of a factory = `global_factory_share.pct` (0 when missing). The global share must list every active
  factory once, each 0–100 with at most 2 decimals, total exactly 100.
- Base share of a producible factory = `sku_factory.override_share_pct ?? default`. Non-producible factories (and
  factories without a `sku_factory` row) get 0.
- **Effective share** (what the UI shows and the balancer uses) = the base shares of the producible factories passed
  through `Business/Quotes/ShareRedistribution.Redistribute` (proportional, rounded to 2 decimals, remainder to the
  largest share, ties by factory id, equal split when all bases are 0). This is the same function the quotes module
  uses (S-15); stored values never change because of redistribution.
- **Setting an override** (factory manager, own factory, producible factories only, 0–100, ≤ 2 decimals):
  the factory's row stores the value with `overrideBy` / `overrideAt`. The remaining `100 − value` is split among
  all other producible factories in proportion to their current effective shares (same rounding: 2 decimals,
  remainder to the largest) and stored in their `override_share_pct`. Rows that only received a rebalanced value
  keep `overrideBy = null` and show as `Rebalanced`; another manager's override keeps its author and stays
  `Override` but its value is rescaled. The only producible factory can only be set to 100.
- **Reset to default** (own factory): clears the factory's override. When no manager override remains among the
  producible factories, every rebalanced value of the SKU is cleared as well (the whole row returns to the global
  defaults). Otherwise the factory goes back to its default and the remaining `100 − default` is split among the
  other producible factories in proportion to their current effective shares.
- **Producible off**: clears that factory's override; when no manager override remains, rebalanced values are
  cleared too. Producible on: the factory starts from its default. In both cases the effective shares are
  redistributed at read time, so the total is always 100 (or 0 when no factory can produce).
- Every change writes history (`entityType = sku`, fields `producible` and `overrideSharePct`, with the factory):
  source `User` (or `Import` for the producible import) for the edited factory, `System` for rebalanced factories.
- Global share edits write history `entityType = global-factory-share`, `entityId = global`, field `pct`.

### Production conflict (S-20)

Derived on read, nothing stored, Quotes tables only read. For every SKU and factory, the latest `quote` with status
`Produced` and a `production_log_json` (`[{ processId, machineType }]`, or `{ processes: [...] }`) is compared with the
SKU's `sku_process` rows and that factory's `process_factory.machine_type`:

| Kind | When | expected → actual |
|---|---|---|
| `ProcessNotListed` | process in the log but not mapped | `not assigned` → `PROC on MACHINE` |
| `MachineMismatch` | both machine types known and different (case-insensitive) | `PROC on A` → `PROC on B` |
| `ProcessNotUsed` | mapped process missing from the log | `PROC on A` → `not used` |

Each issue carries factory, process, machine, order id, order item id and the produced time. The conflict clears as
soon as the mapping is fixed or a later produced quote matches. Shown as a chip in the SKU list (Validation column)
and in the SKU header; the Processes tab lists the issues.

### History (Audit tab)

| Change | `field` | source |
|---|---|---|
| SKU created | `created` (new value = Site SKU) | Sync |
| master field | `siteSku`, `name`, `productType`, `status`, `dropshipperId`, `dropshipperName`, `dropshipperTotalCostAmount`, `dropshipperTotalCostCurrency` | Sync |
| alias added / removed, deprecated flag | `alias`, `alias:{id}:deprecated` | Sync |
| component mapping | `component:{erpId}` (value `"{quantity} {usage}"`) | Sync |
| process mapping | `process:{processId}` (value = quantity) | Import (Sync when a dropshipper loses it) |
| validation | `validationStatus`, `validationIssues` | Sync / Import |
| producible / override | `producible`, `overrideSharePct` (+ factory) | User / Import / System |

Sync entries are written with `changedBy = "Master catalog sync"`.

## Endpoints (Factory Catalog API)

All controllers carry `[AzureAuthorize(Permission.SkusView)]`; stricter permissions are listed. "Own factory" is
checked with `ICurrentUser.EnsureCanEditFactory` (403) and the factory must be active (404).

| Method and route | Permission | Request → response |
|---|---|---|
| `POST /v1/skus/search` | SkusView | list request → `{ items: SkuListItem[], totalCount }` |
| `POST /v1/skus/export` | SkusView | export request → `skus_{yyyyMMdd_HHmmss}.xlsx` |
| `GET /v1/skus/summary` | SkusView | → `{ total, dropshipper, invalid }` (tiles) |
| `GET /v1/skus/{skuUuid}` | SkusView | → `SkuDetails`; 404 |
| `PUT /v1/skus/{skuUuid}/factories/{factoryId}/producible` | SkusEditProducible + own factory | `{ producible: bool }` → `SkuFactoryShares`; 404, 422 dropshipper |
| `PUT /v1/skus/{skuUuid}/factories/{factoryId}/share-override` | SkusEditShareOverride + own factory | `{ sharePct: 0..100 }` → `SkuFactoryShares`; 422 not producible / only factory ≠ 100 / > 2 decimals |
| `DELETE /v1/skus/{skuUuid}/factories/{factoryId}/share-override` | SkusEditShareOverride + own factory | → `SkuFactoryShares` |
| `POST /v1/skus/process-mappings/import/validate` | SkusEditMappings | multipart `file` → `ImportValidationResult`, nothing saved |
| `POST /v1/skus/process-mappings/import` | SkusEditMappings | multipart `file` → `ImportValidationResult`; 422 with row errors when any row is invalid (nothing saved) |
| `POST /v1/factory-share/search` | SkusView | list request → `{ items: FactoryShareRow[], totalCount }` (manufactured SKUs only) |
| `POST /v1/factory-share/export` | SkusView | export request → `factory-share_{yyyyMMdd_HHmmss}.xlsx` |
| `POST /v1/factory-share/producible/bulk` | SkusEditProducible + own factory | `{ factoryId, skuUuids: uuid[], producible }` → `{ updated }` |
| `POST /v1/factory-share/producible/import/validate` | SkusEditProducible | multipart `file` → `ImportValidationResult` |
| `POST /v1/factory-share/producible/import` | SkusEditProducible | multipart `file` → `ImportValidationResult`; 422 when any row is invalid |
| `GET /v1/settings/factory-share` | SkusView | → `GlobalFactoryShare` |
| `PUT /v1/settings/factory-share` | SkusEditGlobalShare | `{ factoryShares: [{ factoryId, pct }] }` → `GlobalFactoryShare`; 422 |
| `POST /v1/sync/master-catalog-skus/run` | SyncTrigger | → `SyncStatus` (status `Running`); 409 when a run is in progress |

Status and history use the shared endpoints: `GET /v1/sync/master-catalog-skus/status`,
`GET /v1/history/sku/{skuUuid}`, `GET /v1/history/global-factory-share/global`.

### Shapes

```
SkuListItem    { skuUuid, siteSku, factorySkus: string[], name, productType: "Manufactured"|"Dropshipper",
                 status: "Active"|"Retired", producibleAt: string[], validationStatus: "Valid"|"Invalid",
                 productionConflictFactoryIds: string[], dropshipperName, lastMasterSyncAt }
SkuDetails     { skuUuid, siteSku, name, productType, status,
                 aliases: [{ id, type: "Factory", deprecated }],
                 dropshipper: { dropshipperId, name, totalCost: { amount, currency } } | null,
                 validation: { status, issues: string[] }, lastMasterSyncAt,
                 components: [{ erpId, name, componentStatus, quantity, usage: "Static"|"Dynamic",
                                stock: [{ factoryId, totalStock, stockStatus: "Ok"|"Low"|"OutOfStock"|"OosConflict" }] }],
                 processes: [{ processId, name, type, measuredIn, quantity,
                               capacity: [{ factoryId, runsHere, capacityPct, machineType }] }],
                 factories: SkuFactoryShare[], shareTotalPct,
                 productionConflict: { status: "None"|"Conflict",
                                       issues: [{ factoryId, kind, processId, processName, processType, machineType,
                                                  expected, actual, orderId, orderItemId, seenAt }] } }
SkuFactoryShare  { factoryId, producible, defaultSharePct, overrideSharePct, effectiveSharePct,
                   source: "NotProducible"|"Default"|"Override"|"Rebalanced", overrideBy, overrideAt }
SkuFactoryShares { skuUuid, factories: SkuFactoryShare[], shareTotalPct }
FactoryShareRow  SkuFactoryShares + { siteSku, name, status }
GlobalFactoryShare { factoryShares: [{ factoryId, pct }], totalPct, updatedBy, updatedAt }
ImportValidationResult { totalRows, validRows, errors: [{ row, field, message }] }   (row 0 = file-level error)
```

List filters (column-header): SKU list — `siteSku`, `factorySku`, `name`, `dropshipperName` (text), `productType`,
`status`, `validationStatus`, `producibleAt` (values); Factory share — `siteSku`, `name` (text), `status`,
`producibleAt` (values). Search (both): UUID, Site SKU, Factory SKU (alias) and name, case-insensitive contains.

## Master catalog sync (S-01, S-02, S-03, S-09, S-19)

`IMasterCatalogClient` (typed `HttpClient`, Basic auth when a username is configured). Daily Hangfire recurring job
`master-catalog-skus-sync` (UTC cron), plus the manual trigger, which starts a `sync_run` through `ISyncRunTracker`
(409 while one runs) and enqueues the job; the UI polls the status endpoint (S-03 progress and result). A scheduled
run that finds a run in progress is skipped with a warning.

Each run pulls every page changed since the start of the last successful run (`changedSince` omitted on the first
run), upserts by UUID, then revalidates all SKUs, then completes the run with `added`, `updated`, `failed`.
A SKU that fails parsing is skipped, logged and counted in `failed`; an HTTP / transport error fails the whole run
(`sync_run.status = Failed` with the error, shown by `SyncStatusBadge`).

### Assumed contract (to be confirmed by the master catalog team)

`GET {BaseUrl}/v1/skus?page={n}&pageSize=500[&changedSince=2026-11-23T02:10:00Z]` →

```json
{
  "items": [
    {
      "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
      "siteSku": "TGI-N-4609-SD-VMGP_1.1",
      "name": "Name necklace with diamond, gold vermeil",
      "productType": "manufactured",
      "status": "active",
      "aliases": [{ "id": "TGN-N-4609-SD-VMGP_1.1", "type": "factory", "deprecated": true }],
      "components": [{ "erpId": "R.VM.BRD.08MM", "quantity": 1, "usage": "static" }],
      "dropshipper": null
    },
    {
      "skuUuid": "0b6c…", "siteSku": "110-12-4492-33", "name": "Leather watch strap",
      "productType": "dropshipper", "status": "active", "aliases": [], "components": [],
      "dropshipper": { "dropshipperId": "DS-17", "name": "Strapworks", "totalCost": { "amount": 12.4, "currency": "USD" } }
    }
  ],
  "hasMore": true
}
```

Enum values are case-insensitive. Required: `skuUuid`, `siteSku` (≤ 200), `name` (≤ 500), `productType`
(`manufactured` / `dropshipper`), `status` (`active` / `retired`); aliases need `id` (≤ 200) and `type` `factory`;
components need `erpId` (≤ 100), `quantity` > 0 and `usage` `static` / `dynamic`, unique per SKU; dropshippers need
`dropshipperId` (≤ 100), `totalCost.amount` ≥ 0 and a 3-letter currency. Retired SKUs are sent with
`status: "retired"`, never omitted.

### Configuration (`MasterCatalog` section, `MasterCatalogOptions`)

| Key | Default | Meaning |
|---|---|---|
| `MasterCatalog:BaseUrl` | — | Master catalog API base URL; a run fails with a clear error when empty |
| `MasterCatalog:Username` / `Password` | — | Basic auth credentials (secret) |
| `MasterCatalog:CronExpression` | `0 3 * * *` | Daily schedule (UTC), after the SAP sync at 02:00 |

## Import formats (.xlsx, first sheet, header row 1, column names case-insensitive)

Validate and confirm both parse the file again; confirm saves only when no row has an error.

**SKU process mappings** (S-10, SkusEditMappings): columns `SKU` (UUID, alias or Site SKU — resolved like
`Business/Quotes/SkuLookup`: UUID, then alias, then an unambiguous Site SKU, case-insensitive), `Process ID`
(must exist), `Quantity` (> 0, in the process unit, invariant decimal). A second sheet `Approval` with key / value
rows `Approved by` and `Approved on` is required (file-level error otherwise) — this is how "only approved files can
be imported" is represented. Dropshipper SKUs and duplicate SKU + process pairs are row errors. The file replaces the
whole process list of every SKU it contains; SKUs not in the file are untouched. History source `Import`; imported
SKUs are revalidated.

**Producible** (S-13, SkusEditProducible): columns `SKU` (as above), `Factory ID` (active and editable by the
caller), `Producible` (`Yes`/`No`, `Y`/`N`, `true`/`false`, `1`/`0`). Dropshipper SKUs and duplicate SKU + factory
pairs are row errors.

## Frontend (`src/app/skus/**`)

| Route | Page |
|---|---|
| `/skus` | SKU list: tiles (Total SKUs, Dropshipper, Invalid), `SyncStatusBadge` "Last master catalog sync" with "Sync now" for `Sync.Trigger`, columns Site SKU, Factory SKU, SKU name, Type, Status, Producible at, Validation (+ production-conflict chip), optional UUID / Dropshipper / Last master catalog sync, export, "Import process mappings" for `Skus.EditMappings` |
| `/skus/:id` | SKU detail: Overview · Components · Processes · Factories · Audit; dropshipper SKUs show Overview only |
| `/factory-share` | Factory share: producible checkbox + share % per factory column, own column highlighted and editable, grey default / blue override, per-row "Reset to default", 100 % check, auto-save, bulk select (producible on / off for own factory), "Import producible", default balance card |
| `/factory-share/settings` | Global share (default balance) editor for `Skus.EditGlobalShare`, total must be 100, with its audit |

Component stock statuses are mapped with `components-catalog/utils/stock-status.ts` (`toKitStockStatus`).

## Schema changes

None. The module uses the foundation entities and `FactoryCatalogDbContext` configuration as they are.

## Requests to orchestrator

- None required to build. Optional: when the SAP components sync deactivates a component or a process is created /
  renamed, SKU validation is refreshed only by the next master catalog run or process-mapping import (see M3-9);
  calling `SkuMappingValidator.ValidateAll` at the end of the SAP sync would need a change in the Components module.

## Story coverage

| Story | Status | Notes |
|---|---|---|
| S-01 Master catalog sync | done | daily job + "changed since" pull; events left for later (M3-1) |
| S-02 UUID, Site SKU, aliases | done | aliases read only and deprecated |
| S-03 Sync now | done | 409 while running; badge polls progress and result |
| S-04 SKU list | done | tiles, columns, search, column filters, export |
| S-05 Components tab | done | stock status per factory via `ComponentFactory` methods |
| S-06 Processes tab | done | quantity in the process unit, capacity per factory, links; no step column (M3-12) |
| S-07 Factories tab | done | producible, share %, Default / Override / Rebalanced, reset |
| S-08 Audit tab | done | shared history tab, newest first |
| S-09 Component mappings | done | only from the master catalog; "SKUs using this" is in Module 1 |
| S-10 Process mapping import | done | validate + confirm, approval sheet |
| S-11 Validation | done | invalid SKUs excluded from quotes by the quotes module |
| S-12 Global share | done | Settings page under `/factory-share/settings` |
| S-13 Producible | done | checkbox, bulk, Excel import, own column only |
| S-14 Override | done | auto-save, reset, rebalancing rule above |
| S-15 Redistribution | done | `ShareRedistribution` reused |
| S-19 Dropshipper | done | limited record, Overview only, excluded from factory share |
| S-20 Production conflict | done | derived from `quote.production_log_json`; depends on Module 4 firing produced events |
