# Module 1 — Components

Stories C-03 … C-16 of `spec/requirements/1-components.md`. UI per `spec/mockup/Phase 1 UI Changes.md` sections 1–2.
Assumptions are logged in `spec/questions/m1-components.md`.

## Domain

Entities (`Domains/Components`): `Component` (PK `erpId`), `ComponentFactory` (PK `erpId + factoryId`),
`ComponentAlternative` (one-way link), `ComponentReplacement` (log of replacements chosen in TG Supplier).
Enums: `ComponentStatus` (`Active`, `Inactive`), `StockStatus` (`Ok`, `Low`, `OutOfStock`),
`StockAlertStatus` (`Ok`, `Low`, `OutOfStock`, `OosConflict` — the status shown in the UI).

### Derivation rule (shared with the other modules, implemented on `ComponentFactory`)

| Value | Rule | Method |
|---|---|---|
| total | `warehouseStock + productionFloorStock` | `GetTotalStock()` |
| effective critical level | `criticalStockLevelOverride ?? component.criticalStockLevel` | `GetEffectiveCriticalLevel(global)` |
| `stockStatus` | `OutOfStock` when the manual OOS flag is active or total <= 0; else `Low` when total < critical level; else `Ok` | `GetStockStatus(global)` |
| `oosConflict` | manual OOS active and total >= critical level | `IsOosConflict(global)` |
| UI status | `OosConflict` when `oosConflict`, otherwise `stockStatus` | `GetStockAlertStatus(global)` |

When no critical level is set (neither override nor global) a row is never `Low` and never in conflict.
Nothing derived is stored.

- Component status in lists = the worst `StockAlertStatus` over its `component_factory` rows
  (`Ok < Low < OutOfStock < OosConflict`) plus the factories that have that status; a component without rows is `Ok`.
- `actionRequired` = component is `Active` and its worst status is `OutOfStock` or `OosConflict`.
- `skusUsing` / `usedInSkus` are read from `sku_component` joined with `sku`, manufactured SKUs only.
- Alternatives are global and one-way; an alternative must be an active component and not the component itself.
- Unit cost is view-only and returned only to callers with `ComponentsViewCost` (otherwise `null`).
- Every edit writes `history_entry` rows for the component (`entityType = component`, `entityId = erpId`):

| Change | `field` | `factoryId` |
|---|---|---|
| manual OOS flag / note | `manualOos`, `manualOosNote` | factory |
| global critical level | `criticalStockLevel` | — |
| factory override | `criticalStockLevelOverride` | factory |
| alternative added / removed | `alternative` (old or new value = alternative ERP id) | — |
| alternative use flags | `alternative {erpId} forBalancer`, `alternative {erpId} forPlanner` | — |
| replacement recorded | `replacement in order {orderId} item {orderItemId}` (source `System`, changed by `replacedBy`) | factory |
| SAP sync | SAP field name (`name`, `status`, `family`, …), source `Sync`, changed by `SAP sync` | — |

## Endpoints (Factory Catalog API)

All under `[AzureAuthorize(Permission.ComponentsView)]` unless a stricter permission is listed. Basic callers pass.

| Method and route | Permission | Request → response |
|---|---|---|
| `POST /v1/components/search` | ComponentsView | list request → `{ items: ComponentListItem[], totalCount }` |
| `POST /v1/components/export` | ComponentsView | export request → `components_{yyyyMMdd_HHmmss}.xlsx` |
| `GET /v1/components/filter-options` | ComponentsView | → `{ families: string[], materials: string[] }` |
| `GET /v1/components/{erpId}` | ComponentsView | → `ComponentDetails`; 404 |
| `PUT /v1/components/{erpId}/factories/{factoryId}/manual-oos` | ComponentsEditManualOos + own factory | `{ active: bool, note?: string (≤ 1000) }` → 200; 403, 404 |
| `PUT /v1/components/{erpId}/critical-level` | ComponentsEditCriticalLevels | `{ level: number \| null }` (≥ 0) → 200; 404, 422 |
| `PUT /v1/components/{erpId}/factories/{factoryId}/critical-level-override` | ComponentsEditCriticalLevels | `{ level: number \| null }` (`null` clears) → 200; 404, 422 |
| `POST /v1/components/{erpId}/alternatives` | ComponentsEditAlternatives | `{ alternativeErpId, forBalancer, forPlanner }` → 200; 404, 409 duplicate, 422 itself / inactive / unknown |
| `PUT /v1/components/{erpId}/alternatives/{alternativeErpId}` | ComponentsEditAlternatives | `{ forBalancer, forPlanner }` → 200; 404 |
| `DELETE /v1/components/{erpId}/alternatives/{alternativeErpId}` | ComponentsEditAlternatives | → 200; 404 |
| `GET /v1/components/{erpId}/alternatives?factoryId=` | ComponentsView (TG Supplier, Basic) | → `ReplacementAlternative[]`; 404 unknown component / factory |
| `POST /v1/components/replacements` | ComponentsView (TG Supplier, Basic) | `RecordReplacement` → `ComponentReplacement`; 422 |
| `POST /v1/component-stock-levels/search` | ComponentsView | list request → `{ items: ComponentStockLevel[], totalCount }` |
| `POST /v1/component-stock-levels/export` | ComponentsView | export request → `component-stock-levels_{yyyyMMdd_HHmmss}.xlsx` |
| `POST /v1/sync/sap-components/run` | SyncTrigger | → sync status (same shape as `GET /v1/sync/sap-components/status`); 409 when a run is in progress |

### Shapes

```json
// ComponentListItem
{ "erpId": "R.WG.10K.BRD.08MM", "name": "White gold board 10K 0.8", "family": "Board", "material": "10K white gold",
  "componentStatus": "Active", "status": "OosConflict", "statusFactoryIds": ["125"], "actionRequired": true,
  "totalStock": 20.0, "criticalLevel": 2, "hasCriticalLevelOverrides": true,
  "unitCost": 245.5, "unitCostCurrency": "USD", "skusUsing": 12,
  "stockByFactory": { "110": 13.5, "125": 6.5 }, "lastSapSyncAt": "2026-11-24T02:04:11Z" }
```

List keys for `sortBy`, `filters` and export `columns`: `erpId`, `name`, `family`, `material`, `componentStatus`
(`Active` / `Inactive`), `status` (`Ok` / `Low` / `OutOfStock` / `OosConflict`), `totalStock`, `criticalLevel`,
`unitCost`, `skusUsing` and `stock_{factoryId}` (total stock in that factory). Export also knows `statusFactories`
and `unitCostCurrency`. `search` matches ERP ID and name. Default sort: `erpId`.

```json
// ComponentDetails
{ "erpId": "…", "name": "…", "status": "Active", "family": "Board", "material": "10K white gold",
  "measure": { "measuredBy": "thickness", "value": 0.8, "unit": "mm" },
  "unitOfMeasure": { "type": "weight", "quantity": 1, "unit": "kg" },
  "countryOfOrigin": "US", "hsCode": "7113.11.00",
  "unitCost": { "amount": 245.5, "currency": "USD", "source": "SAP" },
  "criticalStockLevel": 2,
  "stockStatus": "OosConflict", "stockStatusFactoryIds": ["125"], "actionRequired": true,
  "factories": [
    { "factoryId": "125", "warehouseStock": 6.0, "productionFloorStock": 0.5, "totalStock": 6.5,
      "criticalLevel": 5, "criticalLevelOverride": 5, "stockStatus": "OutOfStock", "oosConflict": true,
      "status": "OosConflict",
      "manualOos": { "active": true, "setBy": "Zsófia Nagy", "setAt": "2026-11-20T08:10:00Z", "note": "Supply scheduled for Feb 1" },
      "stockSyncedAt": "2026-11-24T02:04:11Z" } ],
  "alternatives": [
    { "erpId": "R.WG.10K.BRD.09MM", "name": "…", "status": "Active", "forBalancer": true, "forPlanner": true,
      "factories": [ { "factoryId": "110", "totalStock": 4, "status": "Low" } ] } ],
  "usedInSkus": [ { "skuUuid": "8f3c1a2e-…", "name": "…", "siteSku": "…", "quantity": 1, "usage": "Static" } ],
  "lastSapSyncAt": "2026-11-24T02:04:11Z" }
```

`factories` holds only the factories that have a `component_factory` row. `unitCost` is `null` without
`ComponentsViewCost`.

```json
// ComponentStockLevel — one row per active component × factory whose status is Low, OutOfStock or OosConflict
{ "erpId": "…", "name": "…", "family": "Board", "factoryId": "125", "warehouseStock": 6.0, "productionFloorStock": 0.5,
  "totalStock": 6.5, "criticalLevel": 5, "isCriticalLevelOverride": true, "status": "OosConflict",
  "manualOosSetBy": "Zsófia Nagy", "manualOosSetAt": "2026-11-20T08:10:00Z", "manualOosNote": "Supply scheduled for Feb 1" }
```

Keys: `erpId`, `name`, `family`, `factoryId`, `warehouseStock`, `productionFloorStock`, `totalStock`,
`criticalLevel`, `status`. Default sort `erpId`, then `factoryId` (the UI asks for `status` descending).

```json
// ReplacementAlternative (C-16): active alternatives flagged "for S.C. planner", with stock in the given factory
{ "erpId": "…", "name": "…", "unitOfMeasureUnit": "kg", "warehouseStock": 3, "productionFloorStock": 1,
  "totalStock": 4, "stockStatus": "Low" }

// RecordReplacement (request)
{ "orderId": "…", "orderItemId": "…", "factoryId": "110", "originalErpId": "…", "replacementErpId": "…",
  "quantity": 1.5, "replacedBy": "floor.user", "replacedAt": "2026-11-24T09:00:00Z" }

// ComponentReplacement (response): the request fields plus "id"; replacedAt defaults to now
```

The replacement must be an alternative of the original that is flagged for the S.C. planner, the factory must
exist and `quantity`, when sent, must be positive.

### Manual OOS behaviour (C-12)

- Ticking sets `active`, `setBy` (user name, e-mail when there is no name) and `setAt`; a `component_factory` row
  is created when the factory had none.
- Sending `active: true` again only changes the note; who and when stay.
- Unticking clears the flag, author, date and note.
- The handler calls `ICurrentUser.EnsureCanEditFactory(factoryId)`; the SAP sync never touches these columns.

## SAP sync (C-03, C-04)

`ISapComponentsSyncJob` → recurring Hangfire job `sap-components-sync`, cron `SapExport:CronExpression`
(default `0 2 * * *`, UTC). `POST /v1/sync/sap-components/run` runs the same sync inside the request. Both go
through `ISapComponentsSyncService`, tracked with `ISyncRunTracker` (`SyncTypes.SapComponents`). The scheduled job
skips (warning in the log) when a run is already in progress; any other failure fails the run and is retried by
Hangfire.

### Files (assumed, open question I-1)

Read with `IS3FileReader` from `s3://{SapExport:Bucket}/{SapExport:Prefix}/{yyyy-MM-dd}/` (UTC date of the run).
UTF-8 CSV, comma separated, header row, RFC 4180 quoting. Column order is free, header names are case-insensitive.
The parser is behind `ISapExportReader` (`SapCsvExportReader`), so another format replaces one class.

`components.csv` — full component master:

| Column | Required | Notes |
|---|---|---|
| `erp_id` | yes | ≤ 100 |
| `name` | yes | ≤ 300 |
| `status` | yes | `active` / `inactive` |
| `family`, `material` | | ≤ 100 / ≤ 200 |
| `measured_by`, `measure_value`, `measure_unit` | | e.g. `thickness`, `0.8`, `mm` |
| `uom_type`, `uom_quantity`, `uom_unit` | | e.g. `weight`, `1`, `kg` |
| `country_of_origin`, `hs_code` | | ISO-2 / ≤ 20 |
| `unit_cost_amount`, `unit_cost_currency` | | ISO-3 currency |

`stock.csv` — full stock snapshot, one row per component, factory and location:

| Column | Required | Notes |
|---|---|---|
| `erp_id` | yes | |
| `factory_id` | yes | `factory.id` |
| `location` | yes | `warehouse` / `production_floor` |
| `quantity` | yes | decimal, invariant culture |

### Rules

- New ERP ids are added, changed ones updated (a `history_entry` per changed field, source `Sync`), ERP ids missing
  from the file are marked `Inactive` — nothing is deleted.
- Unit cost is written only when the file carries an amount (source `SAP`).
- Stock replaces the previous snapshot: rows in the file are summed per component / factory / location, a missing
  location is 0, existing `component_factory` rows that are not in the file are set to 0. `stockSyncedAt` is set
  on every row. Manual OOS columns and the critical-level override are never written.
- Row-level problems (missing or invalid value, duplicate `erp_id`, unknown component or factory in `stock.csv`)
  are logged, counted as `failed` and skipped; the run goes on.
- The run fails as a whole when a file is missing, a required column is missing or `components.csv` has no valid
  row (so an empty file cannot deactivate the catalog).
- Counts: `added` = new components, `updated` = existing components whose master data or stock changed.
- After the changes are saved, every SKU's mapping is revalidated (`SkuMappingValidator.ValidateAll` from the SKUs
  module, history source `Sync`, changed by `SAP sync`) before the run is completed, so a SKU using a component
  that has just become inactive is flagged `Invalid` right away. A validation failure fails the run.

### Config keys

| Key | Meaning |
|---|---|
| `SapExport:Bucket` | S3 bucket of the export (required to run) |
| `SapExport:Prefix` | key prefix, may be empty |
| `SapExport:CronExpression` | schedule, default `0 2 * * *` (UTC) |

## Frontend (`src/app/components-catalog`)

| Route | Page |
|---|---|
| `/components` | list: ERP ID, Name, Family, Status (worst + factories), Total stock, Critical level, SKUs using; column picker adds Material, Unit cost (with `Components.ViewCost`) and one stock column per factory; header "Last SAP sync"; action-required marker |
| `/components/:erpId` | tabs Specifications · Stock by factory · Alternatives · SKUs using this · Settings · Audit |
| `/component-stock-levels` | rows below critical level, OOS or OOS conflict per factory; header filters incl. Factory; export |

State is in `store/` (`componentsCatalog`: loaded component, loading / saving flags, filter options). Every edit is
a thunk that saves, reloads the component and shows a notification. Lock tags, own-factory gating of the OOS
checkbox (`canInFactory(Components.EditManualOos)`), the inline note with ✓ / Enter, the two "use" checkboxes and
the critical-level inputs follow the UI spec; controls are shown to everyone and disabled for those who may not edit.

## Story coverage

| Story | Status | Notes |
|---|---|---|
| C-03 SAP component master sync | done | file format is an assumption (I-1); never run against a real export |
| C-04 Stock per factory and location | done | stored as two columns per component / factory |
| C-05 Component list | done | |
| C-06 Stock tab | done | |
| C-07 Alternatives tab | done | |
| C-08 "SKUs using this" tab | done | reads `sku_component` / `sku`; empty until Module 3 fills them |
| C-09 Critical levels | done | |
| C-10 Unit cost | done | view only, gated by `ComponentsViewCost`; no edit endpoint (`ComponentsEditCost` unused) |
| C-11 Audit tab | partial | user changes and SAP master-data changes are logged; daily stock quantity changes are not |
| C-12 Manual OOS | done | |
| C-13 Stock levels view | done | |
| C-14 Action required / OOS conflict | done | |
| C-15 Approved alternatives | done | "top 100 SKUs first" is a data-entry scope, nothing in code |
| C-16 TG Supplier replacement | partial | API only (list + record); the TG Supplier screen is another repository |
