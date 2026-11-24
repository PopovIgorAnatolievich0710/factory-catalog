# Module 2 — Process capacity

Requirements: [`spec/requirements/2-process-capacity.md`](../requirements/2-process-capacity.md) (P-01 … P-14).
UI: `spec/mockup/Phase 1 UI Changes.md` sections 1 and 3. Assumptions: [`spec/questions/m2-processes.md`](../questions/m2-processes.md).

## Code map

| Layer | Path |
|---|---|
| Entities | `Domains/Processes` (`Process`, `ProcessFactory`, `ProcessType`, `ProcessStatus`, `ProcessUnit`) — unchanged from the foundation |
| Contracts | `Business.Abstractions/Processes` — requests (`ProcessRequests.cs`), DTOs, `ProcessCapacityCalculator`, `IProcessUsageReader` |
| Handlers | `Business/Processes` (+ `Import/`) |
| API | `Api/Controllers/Processes/{ProcessesController,ProcessCapacityController}.cs`, `Api/Models/Processes`, `Api/Mappers/Processes` |
| DI | `CompositionRoot/Modules/ProcessesModule.cs` (no recurring jobs) |
| Tests | `tests/unit-tests/.../Processes` |
| UI | `frontend/FactoryCatalogUI/src/app/processes` (`pages/`, `components/`, `services/`, `store/`, `entities.ts`) |

Tables written: `process`, `process_factory` (plus `history_entry` through `IHistoryWriter`). Read only: `factory`,
`sku`, `sku_process`, `quote`, `quote_set`.

## Endpoints

All under the Factory Catalog API; every endpoint needs `ProcessesView`, the table lists the additional permission.
Enums are strings: `type` `Machine | Work`; `status` `Active | Inactive`;
`measuredIn` `Minutes | Seconds | Hours | Units | Pieces | Cm | Mm | Cm2 | Mm2`; capacity `status` `Offline | Reduced | Full`.

| Method | Route | Extra permission | Purpose |
|---|---|---|---|
| POST | `/v1/processes/search` | — | list (P-03) |
| POST | `/v1/processes/export` | — | Excel export of the list |
| GET | `/v1/processes/{processId}` | — | single view, all tabs |
| POST | `/v1/processes` | `ProcessesEdit` | new process (P-01) |
| PUT | `/v1/processes/{processId}` | `ProcessesEdit` | edit specifications / deactivate (P-01) |
| PUT | `/v1/processes/{processId}/factories/{factoryId}` | `ProcessesEditFactorySetup` + own factory | runs here, machine type, labor cost, max per day (P-02, P-05, P-11, P-13) |
| POST | `/v1/processes/import/validate` | `ProcessesEdit` | validate an import file, nothing saved (P-04) |
| POST | `/v1/processes/import` | `ProcessesEdit` | import the file (P-04) |
| GET | `/v1/process-capacity` | — | processes × factories grid (P-08) |
| GET | `/v1/process-capacity/factories/{factoryId}` | — | single-factory view: processes the factory runs (P-06, P-14) |
| PUT | `/v1/process-capacity/factories/{factoryId}/processes/{processId}` | `ProcessesEditCapacity` + own factory | set capacity, reason, expected recovery (P-06, P-07, P-09) |

Audit tab: shared `GET /v1/history/process/{processId}`.

### `POST /v1/processes/search` and `/export`

Standard list request (`spec/backend/api.md`). `search` matches process ID or name. Default sort `processId`.
Filter / sort / export column keys: `processId`, `name`, `type`, `machineType`, `runningFactoryIds` (`values` = factory
ids, any of; sorts by count), `lowestCapacityPct`, `avgLaborCost`, `skusUsing`, `measuredIn`, `status`; export only:
`avgLaborCostCurrency`. The table is small, so derived columns are filtered and sorted in memory.

```json
{ "items": [{
    "processId": "ENGRAVING", "name": "Engraving", "type": "Machine", "measuredIn": "Minutes", "status": "Active",
    "machineType": "LASER, CNC", "runningFactoryIds": ["110", "315"], "lowestCapacityPct": 80,
    "avgLaborCost": 0.625, "avgLaborCostCurrency": "USD", "skusUsing": 42 }],
  "totalCount": 1 }
```

- `machineType`: distinct machine types of the factories running a machine process; `null` for work processes.
- `runningFactoryIds`, `lowestCapacityPct`: active factories with `runsHere`; `lowestCapacityPct` is `null` when none.
- `avgLaborCost`: average over running factories that have a cost, only when they all use one currency; `null`
  otherwise and for callers without `ProcessesViewCost`.
- `skusUsing`: number of `sku_process` rows.

### `GET /v1/processes/{processId}`

```json
{
  "processId": "ENGRAVING", "name": "Engraving", "type": "Machine", "measuredIn": "Minutes",
  "description": "Text engraving", "status": "Active", "lastUpdate": "2026-11-22T07:40:00Z",
  "factories": [{
    "factoryId": "110", "factoryName": "Factory 110", "runsHere": true, "machineType": "LASER",
    "capacityPct": 80, "capacityStatus": "Reduced",
    "capacityNote": { "reason": "1 of 5 lasers down", "expectedRecovery": "2026-11-28" },
    "capacityUpdatedBy": "fm.110@tenengroup.com", "capacityUpdatedAt": "2026-11-22T07:40:00Z",
    "laborCost": { "amountPerUnit": 0.55, "currency": "USD", "effectiveFrom": "2026-01-01" },
    "maxPerDay": 1000,
    "today": { "available": 800, "used": 200, "remainingPct": 60 }
  }],
  "skus": [{ "skuUuid": "…", "name": "Name ring", "siteSku": "110-12-4492-33", "quantity": 2 }]
}
```

`factories` has one entry per active factory (defaults when there is no `process_factory` row: `runsHere: false`,
`capacityPct: 100`). `laborCost` is `null` without `ProcessesViewCost`. `today` is `null` when the factory does not
run the process or `maxPerDay` is not set. `skus[].quantity` is in the process unit (P-12). 404 when unknown.

### `POST /v1/processes`

Body `{ "processId": "ENGRAVING" | null, "name", "type", "measuredIn", "description" }` → the process (as GET).
`processId` is upper-cased; when empty it is derived from the name (`"Stone setting"` → `STONE_SETTING`). New
processes are `Active`. 422: `name` (required, ≤ 200, unique case-insensitively), `processId` (≤ 50, letters, digits,
`_ - .`, unique), `description` (≤ 1000).

### `PUT /v1/processes/{processId}`

Body `{ "name", "type", "measuredIn", "status", "description" }` → the process. Same validation as create.
`status: "Inactive"` deactivates; there is no delete endpoint. Changing a machine process to work clears the
machine types of its factories. `PACKING` must stay `Work` and `Active` (422 `type` / `status`).

### `PUT /v1/processes/{processId}/factories/{factoryId}`

Body `{ "runsHere": true, "machineType": "LASER", "laborCostAmount": 0.55, "laborCostCurrency": "USD", "maxPerDay": 1000 }`
→ the process. 403 when the factory is not the caller's own. 404 unknown process / factory. 422: `machineType` on a
work process or > 100 chars, negative `laborCostAmount` / `maxPerDay`, `laborCostCurrency` not 3 letters when an
amount is given. The row is created on first use with capacity 100%. `laborCost.effectiveFrom` is set to the day
the amount or currency changes. Turning `runsHere` off keeps the stored capacity (it never auto-resets).

### `GET /v1/process-capacity` and `GET /v1/process-capacity/factories/{factoryId}`

Both return rows of active processes ordered by name. The overview has one cell per active factory (ordered by
factory id); the factory view has exactly one cell and only the processes that factory runs.

```json
[{ "processId": "ENGRAVING", "name": "Engraving", "type": "Machine",
   "cells": [{ "factoryId": "110", "runsHere": true, "capacityPct": 80, "status": "Reduced",
               "reason": "1 of 5 lasers down", "expectedRecovery": "2026-11-28",
               "updatedBy": "fm.110@tenengroup.com", "updatedAt": "2026-11-22T07:40:00Z",
               "maxPerDay": 1000, "today": { "available": 800, "used": 200, "remainingPct": 60 } },
             { "factoryId": "125", "runsHere": false, "capacityPct": 100, "status": "Full", "reason": null,
               "expectedRecovery": null, "updatedBy": null, "updatedAt": null, "maxPerDay": null, "today": null }] }]
```

`runsHere: false` is the "Doesn't run here" cell.

### `PUT /v1/process-capacity/factories/{factoryId}/processes/{processId}`

Body `{ "capacityPct": 80, "reason": "1 of 5 lasers down", "expectedRecovery": "2026-11-28" }` → the factory-view
row of that process. 403 not own factory; 404 unknown process; 422 `capacityPct` not one of 0, 10 … 100, `reason`
> 1000 chars, `factoryId` when the factory does not run the process.

## Domain rules

- **Capacity (P-06):** 0…100 in steps of 10, default 100; changed only through the capacity endpoint. Nothing else
  (import, setup edits, expected recovery date, jobs) touches it. Readers get the stored value straight from
  `process_factory`, so a change is visible to quotes immediately.
- **Note (P-07):** `reason` and `expectedRecovery` are kept only while capacity is below 100; a save at 100 clears them.
- **Who / when (P-09):** any change of capacity, reason or recovery sets `capacity_updated_by` (email) / `_at` and
  writes history with the factory id: `capacityPct` (`"100%"` → `"80% (reason: …)"`), `capacityReason`,
  `expectedRecovery`. A save that changes nothing writes nothing.
- **History fields:** `process` (Created), `name`, `type`, `measuredIn`, `status`, `description`; per factory
  `runsHere`, `machineType`, `laborCostAmount`, `laborCostCurrency`, `maxPerDay` and the capacity fields above.
  Source `User`, or `Import` for the Excel import.
- **Status:** `ProcessCapacityCalculator.Status` — 0 Offline, 10–90 Reduced, 100 Full.
- **Today (P-13, P-14):** `ProcessCapacityCalculator.Today(maxPerDay, capacityPct, used)`:
  `available = maxPerDay × capacityPct ÷ 100`, `remainingPct = max(0, round((available − used) ÷ maxPerDay × 100))`.
  `used` (`IProcessUsageReader.GetUsedToday`) = sum of `quote_set.quantity` of quotes with status `Produced` and
  `produced_at` within the current UTC day in that factory whose process list contains the process. The process
  list is `quote.processes[].processId`; when the quote stores none, the SKU's `sku_process` rows are used.
- **Packing (P-11):** the seeded `PACKING` work process; its per-factory cost is the ordinary labor-cost field
  (flat cost per unit and factory).
- **Unit (P-12):** `process.measured_in` is the unit of `sku_process.quantity` and of the labor cost.
- **Cost visibility:** labor cost values are returned only to callers with `ProcessesViewCost`.

## Import file (P-04)

`.xlsx`, header in row 1, column order free, header names case-insensitive.

| Sheet | Columns |
|---|---|
| `Processes` (or the first sheet) | **Process ID**, **Name**, **Type** (`Machine` / `Work`), **Measured in** (`minutes`, `seconds`, `hours`, `units`, `pieces`, `cm`, `mm`, `cm2` / `cm²`, `mm2` / `mm²`), Status (`Active` / `Inactive`; empty = keep, `Active` for a new process), Description |
| `Factories` (optional) | **Process ID**, **Factory ID**, **Runs here** (`Yes` / `No` / `true` / `false` / `1` / `0`), Machine type, Labor cost, Currency, Max per day |

Bold columns are required. Rows are upserted by Process ID (upper-cased) and by Process ID + Factory ID; rows not in
the file are left alone; capacity is never changed. Empty optional cells of a `Factories` row clear the value.

`POST /v1/processes/import/validate` (multipart field `file`) →
`{ "totalRows": 6, "validRows": 5, "errors": [{ "row": 4, "field": "Type", "message": "Type must be Machine or Work." }] }`.
`row` is the Excel row number; `field` is the column header, prefixed with `Factories: ` for the second sheet; file
level problems use row 0 / 1. Checks: required and well-formed values, the rules of the edit endpoints, IDs and
names repeated in the file, names taken by a process outside the file, unknown process or factory, repeated
process–factory pairs.

`POST /v1/processes/import` → `{ "totalRows", "processesCreated", "processesUpdated", "factoryRowsChanged" }`. Any
error → 422 (`errors.file`) and nothing is saved. Importing the same file twice changes nothing the second time.

## UI

| Page | Route | Notes |
|---|---|---|
| Processes | `/processes` | `CatalogListPage`; columns Process ID, Process, Type, Machine type, Factories running it, Lowest capacity today, Avg labor cost, SKUs using (+ optional Measured in, Status); "New process" and "Import" only with `Processes.Edit` |
| Process | `/processes/:id` | tabs Specifications (inline edit with `Processes.Edit`, lock tag otherwise) · Factories (`FactoryGrid`, own row editable with `Processes.EditFactorySetup`) · SKUs using this (`ClientDataTable`) · Audit (`HistoryTab`) |
| Process capacity | `/process-capacity` | factory manager: own factory, 10%-step slider, status, reason, expected recovery, last changed, used / remaining today; operations manager (and BO administrator): toggle All factories / By factory with a factory switcher; everyone else: the read-only grid |

State: `state.processes` (`process`, `capacityOverview`, `factoryCapacity`), actions in `store/actions.ts`.

## Story coverage

| Story | Status | Notes |
|---|---|---|
| P-01 | done | create, edit, deactivate; unique name; no delete |
| P-02 | done | own-factory setup; machine type only for machine processes; capacity only where the factory runs the process |
| P-03 | done | list columns, tabs, SKUs with quantity |
| P-04 | done | validate + confirm, idempotent upsert; the file format is ours (no sample of the TG Supplier / Pricing Tool export) |
| P-05 | done | labor cost per unit with currency, own factory, logged; hidden without `ProcessesViewCost` |
| P-06 | done | steps of 10, default 100, own factory, never resets |
| P-07 | done | optional, only below 100, information only |
| P-08 | done | grid, tints, "until", reason on hover, "Doesn't run here", toggle for the operations manager |
| P-09 | done | history with user, time, old / new value and reason; Audit tab |
| P-10 | not in scope | owned by the catalog balancer API module |
| P-11 | partial | `PACKING` with a flat cost per factory; variation by item size waits for the rules; inclusion in quotes is the quotes module |
| P-12 | partial | unit on the process and quantity in "SKUs using this"; the mapping itself and the SKU Processes tab are the SKUs module |
| P-13 | done | max per day on the Factories tab, own factory |
| P-14 | done | used / available / remaining per the story formula; depends on the quotes module marking quotes `Produced` |
