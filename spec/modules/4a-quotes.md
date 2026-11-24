# Module 4A — Catalog balancer API (quotes and legacy balancer checks)

Backend only. Everything other systems ask of the Factory Catalog: the new Balancer service (production quotes and
their lifecycle, dropshipper cost) and the legacy balancer in TG Supplier (eligibility check, skip log, country SKU
lists). Requirements: `spec/requirements/4-balancer.md` (B-01 … B-03, B-07, B-10, B-21, sections A / B / E),
`1-components.md` (C-17, C-18), `2-process-capacity.md` (P-10, P-14), `3-skus.md` (S-11, S-15 … S-18).
Assumptions: `spec/questions/m4a-quotes.md`.

Base URL `http://localhost:5010`. JSON is camelCase, dates are ISO-8601 UTC, errors follow `spec/backend/api.md`
(404 `{ message }`, 409 `{ message }`, 422 `{ message, errors }`).

## Endpoints

| Method and path | Caller | Auth | Purpose |
|---|---|---|---|
| `POST /v1/quotes` | Balancer | Basic only | Production quote per order item (B-01 … B-03) |
| `GET /v1/quotes/{quoteKey}` | Balancer, UI | Basic or `BalancerView` | Stored quote set lookup |
| `POST /v1/quotes/search` | Balancer, UI | Basic or `BalancerView` | Quotes for the daily plan and audit links |
| `POST /v1/quotes/{quoteId}/select` | Balancer | Basic only | Quoted → Selected (B-10) |
| `POST /v1/quotes/{quoteId}/produced` | Balancer | Basic only | Selected → Produced, reproductions (B-21) |
| `POST /v1/quotes/{quoteId}/transferred` | Balancer | Basic only | Selected → Transferred (B-23) |
| `GET /v1/quotes/dropshipper-cost?sku=` | Balancer, UI | Basic or `BalancerView` | Dropshipper id, name and total cost (B-07) |
| `POST /v1/legacy-balancer/eligibility` | Legacy balancer | Basic only | OOS and 0 % capacity check per candidate factory (C-17, S-17, P-10 / S-18) |
| `POST /v1/legacy-balancer/skip-log/search` | UI, ops | Basic or `BalancerView` | Why a factory was skipped (C-18) |
| `POST /v1/legacy-balancer/skip-log/export` | UI, ops | Basic or `BalancerView` | Excel export of the skip log |
| `GET /v1/legacy-balancer/country-sku-lists?factoryId=` | Legacy balancer | Basic or `BalancerView` | Country SKU lists generated from the new data (S-16) |

"Basic only": the handler rejects a JWT user with 403 (`ICurrentUser.IsServiceAccount` must be true), whatever the
permission toggle says. Read endpoints use `[AzureAuthorize(Permission.BalancerView)]`.

Enum values in responses are PascalCase strings (`"Live"`, `"Quoted"`, `"ComponentOos"`); requests are parsed
case-insensitively, so `"mode": "live"` from section A works. Reason codes, warning codes and `stockStatus` are
plain strings exactly as listed below. Fields without a value are returned as `null` (not omitted).

---

## `POST /v1/quotes`

Request — section A of the requirement page, unchanged:

```json
{
  "requestId": "b7e1c0d4-1f2a-4c55-9a10-3e2f9d8c7a61",
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
  "quantity": 1,
  "cbd": [
    { "erpId": "R.VM.BRD.08MM", "quantity": 1 },
    { "erpId": "STN.DIA.RND.002", "quantity": 1 }
  ],
  "mode": "live"
}
```

| Field | Rules |
|---|---|
| `requestId` | required uuid; idempotency key for `live` and `shadow` |
| `orderId`, `orderItemId` | required for `live` and `shadow`, optional for `estimate`; at most 90 characters each |
| `skuUuid` | required; the SKU UUID, a legacy alias (`sku_alias.alias`) or a Site SKU, matched case-insensitively |
| `quantity` | integer ≥ 1 — pieces of the order item |
| `cbd[]` | required (may be empty); `erpId` required, `quantity` > 0 **per piece**; repeated `erpId`s are summed |
| `mode` | `live` (default), `shadow`, `estimate` |

Response `200` — section B plus the extra fields marked ⊕:

```json
{
  "quoteKey": "PQ-ORD456789-ITM001",
  "validUntil": "2026-11-24T10:15:00Z",
  "quotes": [
    {
      "quoteId": "PQ-ORD456789-ITM001-110",
      "factoryId": "110",
      "producible": true,
      "reasons": [],
      "cost": { "material": 46.0, "labor": 1.1, "packing": 1.4, "total": 48.5, "currency": "USD" },
      "capacitySignalPct": 80,
      "sharePct": 100,
      "components": [
        { "erpId": "R.VM.BRD.08MM", "quantity": 1, "unitCost": 21.0, "stockStatus": "ok", "alternativeUsed": null, "cost": 21.0, "currency": "USD" },
        { "erpId": "STN.DIA.RND.002", "quantity": 1, "unitCost": 25.0, "stockStatus": "outOfStock", "alternativeUsed": "STN.DIA.RND.003", "cost": 25.0, "currency": "USD" }
      ],
      "processes": [
        { "processId": "ENGRAVING", "quantity": 2, "capacityPct": 100, "laborCost": 1.1, "remainingPct": 80, "currency": "USD" },
        { "processId": "PACKING", "quantity": 1, "capacityPct": 100, "laborCost": 1.4, "remainingPct": 100, "currency": "USD" }
      ],
      "status": "Quoted",
      "selectedAt": null,
      "producedAt": null,
      "transferredAt": null,
      "reproductionOf": null,
      "productionLog": null,
      "warnings": []
    },
    {
      "quoteId": "PQ-ORD456789-ITM001-125",
      "factoryId": "125",
      "producible": false,
      "reasons": ["COMPONENT_OOS"],
      "cost": null,
      "capacitySignalPct": null,
      "sharePct": 0,
      "components": [
        { "erpId": "R.VM.BRD.08MM", "quantity": 1, "unitCost": 21.0, "stockStatus": "outOfStock", "alternativeUsed": null, "cost": 21.0, "currency": "USD" },
        { "erpId": "STN.DIA.RND.002", "quantity": 1, "unitCost": 25.0, "stockStatus": "ok", "alternativeUsed": null, "cost": 25.0, "currency": "USD" }
      ],
      "processes": [
        { "processId": "ENGRAVING", "quantity": 2, "capacityPct": 100, "laborCost": 1.1, "remainingPct": 100, "currency": "USD" },
        { "processId": "PACKING", "quantity": 1, "capacityPct": 100, "laborCost": 1.4, "remainingPct": 100, "currency": "USD" }
      ],
      "status": "Quoted",
      "selectedAt": null, "producedAt": null, "transferredAt": null,
      "reproductionOf": null, "productionLog": null, "warnings": []
    }
  ],
  "requestId": "b7e1c0d4-1f2a-4c55-9a10-3e2f9d8c7a61",
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
  "quantity": 1,
  "mode": "Live",
  "createdAt": "2026-11-24T10:00:00Z"
}
```

| Field | Notes |
|---|---|
| `quoteKey` | `PQ-{orderId}-{orderItemId}`; a later request for the same order item (new `requestId`, e.g. a transfer re-quote or shadow next to live) gets `…-R2`, `…-R3`. Estimates: `EST-{requestId}` |
| `validUntil` | `createdAt` + `Quotes:ValidityMinutes` (default 15). Informational: the lifecycle endpoints do not reject an expired quote |
| `quotes[]` | one per **active** factory, ordered by factory id, producible or not |
| `quotes[].quoteId` | `{quoteKey}-{factoryId}` |
| `quotes[].reasons[]` | empty when producible; every applicable code otherwise (see Reason codes) |
| `quotes[].cost` | `null` when not producible. `total = material + labor + packing`; `labor` excludes packing; `currency` is `Quotes:Currency` (default `USD`) |
| `quotes[].capacitySignalPct` | 0–100, `null` when not producible |
| `quotes[].sharePct` | share after redistribution (S-15); 0 when not producible; producible quotes add up to 100 |
| `components[].quantity` | CBD quantity × item `quantity` |
| `components[].unitCost` | unit cost of the component that will be consumed (the alternative's when `alternativeUsed` is set); `null` when the catalog has no cost |
| `components[].stockStatus` | status of the **requested** component in that factory: `ok`, `low`, `outOfStock`, or `unknown` (not in the catalog) |
| `components[].alternativeUsed` | ERP ID of the "For balancer" alternative that stands in for an OOS component, else `null` |
| ⊕ `components[].cost`, `.currency` | line cost (`unitCost × quantity`) and the currency of the unit cost |
| `processes[].quantity` | mapped quantity in the process unit × item `quantity` |
| `processes[].capacityPct` | capacity % set by the factory manager; `null` when the factory does not run the process |
| `processes[].laborCost` | line cost: `quantity ×` the factory's labor cost per unit; `null` when no labor cost is set |
| ⊕ `processes[].remainingPct`, `.currency` | today's remaining capacity (P-14) and the labor cost currency |
| ⊕ `status`, `selectedAt`, `producedAt`, `transferredAt`, `reproductionOf`, `productionLog` | lifecycle state of the quote |
| ⊕ `warnings[]` | data-quality codes for producible quotes (see Warning codes) |
| ⊕ `requestId`, `orderId`, `orderItemId`, `skuUuid`, `quantity`, `mode`, `createdAt` | the stored request; `skuUuid` is the resolved UUID |

Errors: `403` JWT caller; `404` SKU not found; `409` `requestId` already used for another order item; `422`
validation (`requestId`, `skuUuid`, `quantity`, `cbd`, `orderId`, `orderItemId`, or `sku` when a Site SKU is
ambiguous).

### Quote algorithm

1. **Validate** the request. `estimate` skips steps 2 and 9 and stores nothing.
2. **Idempotency**: a `quote_set` with the same `request_id` is returned as stored (nothing is recalculated, so the
   answer does not change when stock changed in between). A different order item under the same `requestId` → 409.
   Two concurrent first requests: the loser of the unique index re-reads and returns the winner's set.
3. **Resolve the SKU**: UUID → alias → Site SKU (must be unambiguous). Not found → 404.
4. **Dropshipper SKU** (B-07): every factory gets `producible: false`, reason `DROPSHIPPER`, no components or
   processes. The set is stored. Cost comes from `GET /v1/quotes/dropshipper-cost`.
5. **Set-level failures** that mark every factory not producible (the rest is still computed so that the answer
   explains itself): `SKU_INVALID` when the SKU's validation status is `Invalid` (S-11); `UNKNOWN_COMPONENT` when a
   CBD `erpId` is not in the catalog (B-02) — the component line carries `stockStatus: "unknown"`.
6. **Per active factory**
   - `NOT_PRODUCIBLE` unless `sku_factory.producible` is true.
   - **Components** — only the CBD is checked and priced (B-02); the SKU's own Static / Dynamic mapping is not
     consulted here because the CBD already contains every Static component plus the selected Dynamic ones.
     Stock status per the shared rule (below). `outOfStock` → look for an alternative (`component_alternative`
     with `for_balancer`, alternative component `Active`) that is not out of stock in the same factory; prefer
     `ok` over `low`, then the lowest ERP ID. Found → `alternativeUsed`, priced with the alternative's unit cost.
     Not found → `COMPONENT_OOS`.
   - **Processes** — the SKU's `sku_process` rows. No `process_factory` row or `runs_here = false` →
     `PROCESS_NOT_AVAILABLE`. `capacity_pct = 0` → `PROCESS_OFFLINE`. Remaining capacity today (P-14):
     `remainingPct = floor((maxPerDay × capacityPct / 100 − usedToday) / maxPerDay × 100)` clamped to 0–100, where
     `usedToday` = quotes with status `Produced` and `produced_at` in the current UTC day in that factory whose
     SKU maps the process; without `max_per_day` it equals `capacityPct`. `usedToday` comes from the Processes
     module's `IProcessUsageReader` and the formula from `ProcessCapacityCalculator.Today`; the quote handler adds
     the `capacityPct` fallback when the calculator returns no value. Capacity used up (0 % left) does **not**
     make the factory non-producible — it only drives the signal.
   - `producible` = no reasons.
7. **Cost** (producible quotes): `material = Σ unitCost × quantity`; `labor = Σ` process line costs except
   `PACKING`; `packing` = the `PACKING` line; missing costs count as 0 and raise a warning; amounts are rounded to
   4 decimals.
8. **Capacity signal** (producible quotes) = the minimum of: 100 / 50 / 0 for each component consumed with stock
   `ok` / `low` / `outOfStock` (the alternative's status when one is used) and `remainingPct` of each process;
   100 when there is nothing to check.
9. **Share** (S-15): base share = `sku_factory.override_share_pct ?? global_factory_share.pct ?? 0` for producible
   quotes; scaled so that they add up to 100 (`base / Σ base × 100`, 2 decimals, the rounding remainder goes to the
   largest share; all bases 0 → equal split). Non-producible quotes get 0. Stored shares never change.
10. **Store** `quote_set` + one `quote` per factory with status `Quoted` (`live`, `shadow`).

### Shared derivation rules as implemented

- Stock status: `total = warehouse + production floor`; `outOfStock` when the manual OOS flag is active or
  `total <= 0` (a missing `component_factory` row counts as 0); else `low` when
  `total < (critical_stock_level_override ?? critical_stock_level)`; else `ok` (no critical level set → never
  `low`). The rule is `ComponentFactory.GetStockStatus` from the Components module; `ComponentStock.Status` in
  `Business/Quotes` maps it to the quote strings and treats a missing `component_factory` row as `outOfStock`
  (also used by the legacy-balancer eligibility check).
- Share redistribution: `ShareRedistribution` in `Business/Quotes`.

### Reason codes

| Code | Meaning | Scope |
|---|---|---|
| `NOT_PRODUCIBLE` | the factory is not marked producible for the SKU (S-13) | factory |
| `COMPONENT_OOS` | a CBD component is out of stock there and no "For balancer" alternative is in stock there | factory |
| `PROCESS_OFFLINE` | a mapped process is at 0 % in that factory (P-10) | factory |
| `PROCESS_NOT_AVAILABLE` | the factory does not run a mapped process (no row or `runs_here = false`) | factory |
| `UNKNOWN_COMPONENT` | a CBD component is not in the catalog (B-02) | all factories |
| `SKU_INVALID` | the SKU failed mapping validation (S-11) | all factories |
| `DROPSHIPPER` | dropshipper SKU, never routed to a TG factory (B-07) | all factories |

### Warning codes (producible quotes only, derived on read)

| Code | Meaning |
|---|---|
| `COMPONENT_COST_MISSING` | a consumed component has no unit cost; counted as 0 |
| `LABOR_COST_MISSING` | a process has no labor cost in that factory; counted as 0 |
| `PACKING_NOT_MAPPED` | the SKU has no `PACKING` process mapping; `cost.packing` is 0 |
| `CURRENCY_MISMATCH` | a line cost is in another currency than `cost.currency`; amounts were summed without conversion |

---

## `GET /v1/quotes/{quoteKey}`

Returns the stored quote set in the same shape as the `POST /v1/quotes` response, with the current lifecycle state
of every quote. Reproduction records (see `produced`) appear as additional entries with `reproductionOf` set.
`404` when the key is unknown (estimates are never stored).

## `POST /v1/quotes/search`

Standard list request (`spec/backend/api.md`). `search` matches order id, order item id and quote id (contains,
case-insensitive).

| Filter key | Type | Example |
|---|---|---|
| `factoryId`, `orderId`, `orderItemId`, `quoteId`, `quoteKey` | text | `{ "values": ["110"] }` |
| `status` | `Quoted`, `Selected`, `Produced`, `Transferred` | `{ "values": ["Selected"] }` |
| `mode` | `Live`, `Shadow` | `{ "values": ["Live"] }` |
| `producible` | boolean | `{ "value": "true" }` |
| `skuUuid` | uuid | `{ "value": "8f3c1a2e-…" }` |
| `createdAt`, `selectedAt`, `producedAt`, `transferredAt` | datetime | `{ "operator": "between", "value": "2026-11-24T00:00:00Z", "valueTo": "2026-11-24T23:59:59Z" }` |

Sort keys: `createdAt` (default), `selectedAt`, `producedAt`, `transferredAt`, `orderId`, `factoryId`, `status`, `quoteId`.

Daily plan input (B-22) — items allocated to a factory and not produced yet:

```json
{
  "page": 1, "pageSize": 500, "sortBy": "selectedAt",
  "filters": {
    "factoryId": { "values": ["110"] },
    "status": { "values": ["Selected"] },
    "mode": { "values": ["Live"] }
  }
}
```

Response — each item is the set header plus the quote (same `quote` object as in a quote set):

```json
{
  "items": [
    {
      "quoteKey": "PQ-ORD456789-ITM001",
      "requestId": "b7e1c0d4-1f2a-4c55-9a10-3e2f9d8c7a61",
      "orderId": "ORD456789",
      "orderItemId": "ITM001",
      "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
      "quantity": 1,
      "mode": "Live",
      "validUntil": "2026-11-24T10:15:00Z",
      "createdAt": "2026-11-24T10:00:00Z",
      "quote": { "quoteId": "PQ-ORD456789-ITM001-110", "factoryId": "110", "producible": true, "status": "Selected", "…": "…" }
    }
  ],
  "totalCount": 1
}
```

## Quote lifecycle

`Quoted → Selected → Produced`, `Selected → Transferred`. All three endpoints return the item shape of
`/v1/quotes/search` (`{ quoteKey, …, quote }`), `404` for an unknown `quoteId`, `403` for a JWT caller.

### `POST /v1/quotes/{quoteId}/select` (no body)

| Current state | Result |
|---|---|
| `Quoted`, producible, no other quote of the set is `Selected` / `Produced` | → `Selected`, `selectedAt` = now |
| `Selected` | 200, unchanged (idempotent) |
| `Quoted`, not producible | 409 |
| `Quoted`, another quote of the same set is `Selected` or `Produced` | 409 (mark that one `transferred` first, or re-quote) |
| `Produced`, `Transferred` | 409 |

Quotes of other sets for the same order item are not checked (a transfer selects the quote of a new set while the
old one is marked transferred, in either order). Shadow sets can be selected too; filter on `mode` when reading.

### `POST /v1/quotes/{quoteId}/produced`

```json
{
  "producedAt": "2026-11-27T15:20:00Z",
  "productionLog": [
    { "processId": "ENGRAVING", "machineType": "Laser X2" },
    { "processId": "PACKING", "machineType": null }
  ],
  "reproductionOfQuoteId": null
}
```

`producedAt` is required (the "sent" time). `productionLog` is stored as reported in `quote.production_log`
(S-20 reads it). Without `reproductionOfQuoteId`:

| Current state | Result |
|---|---|
| `Selected` | → `Produced`, `producedAt` and the log stored |
| `Produced` with the same `producedAt` | 200, unchanged (idempotent) |
| `Produced` with another `producedAt` | 409 — send `reproductionOfQuoteId` for a reproduction |
| `Quoted`, `Transferred` | 409 |

With `reproductionOfQuoteId` (the public id of the originally produced quote; it may equal `{quoteId}`):

| Situation | Result |
|---|---|
| the original is not `Produced` | 409; unknown original → 404 |
| a reproduction of that original with the same `producedAt` in that factory exists | 200, that record (idempotent) |
| `{quoteId}` is `Selected` and is not the original (the item was re-quoted and reproduced under the new quote) | that quote → `Produced`, linked to the original |
| `{quoteId}` is `Produced` (typically the original itself) | a **new** quote row is created in the same set: copy of `{quoteId}` (factory, costs, components, processes), status `Produced`, `quoteId` = `{quoteId}-RP{n}`, linked to the original |
| `{quoteId}` is `Quoted` or `Transferred` | 409 |

The response is the produced record; for a reproduction `quote.reproductionOf` is the original's `quoteId`
(`reproductionOf` of event E):

```json
{
  "quoteKey": "PQ-ORD456789-ITM001", "orderId": "ORD456789", "orderItemId": "ITM001", "…": "…",
  "quote": {
    "quoteId": "PQ-ORD456789-ITM001-110-RP1", "factoryId": "110", "status": "Produced",
    "producedAt": "2026-11-30T09:00:00Z", "reproductionOf": "PQ-ORD456789-ITM001-110",
    "cost": { "material": 38.2, "labor": 6.1, "packing": 1.4, "total": 45.7, "currency": "USD" },
    "productionLog": [{ "processId": "ENGRAVING", "machineType": "Laser X2" }], "…": "…"
  }
}
```

Reproduction records count as produced items in "used today" (P-14).

### `POST /v1/quotes/{quoteId}/transferred` (no body)

| Current state | Result |
|---|---|
| `Selected` | → `Transferred`, `transferredAt` = now |
| `Transferred` | 200, unchanged (idempotent) |
| `Quoted`, `Produced` | 409 |

## `GET /v1/quotes/dropshipper-cost?sku={uuidOrAlias}`

```json
{
  "skuUuid": "5d0f6a1c-0a53-4d0b-8a57-1f2f6f6f9c11",
  "siteSku": "DS-1001",
  "dropshipperId": "DS-77",
  "dropshipperName": "Print Partner",
  "totalCost": { "amount": 18.5, "currency": "USD" }
}
```

`totalCost` is `null` when the master catalog sent none. `404` unknown SKU; `422` (`errors.sku`) when the SKU is
not a dropshipper SKU or `sku` is missing.

---

## `POST /v1/legacy-balancer/eligibility`

```json
{
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "sku": "TGN-N-4609-SD-VMGP_1.1",
  "cbd": [{ "erpId": "R.VM.BRD.08MM", "quantity": 1 }, { "erpId": "STN.DIA.RND.002" }],
  "factoryIds": ["125", "110", "315"]
}
```

| Field | Rules |
|---|---|
| `orderId` | required, ≤ 100 characters |
| `orderItemId` | optional |
| `sku` | optional; UUID, alias or Site SKU. Missing or unknown → only the CBD is checked (Module 1 behaviour, C-17) |
| `cbd[]` | the order's CBD from the current flow; only `erpId` is used |
| `factoryIds[]` | required; candidate factories in the caller's order; unknown ids → 422 |

Rules per candidate factory, in the caller's order:

1. Counted components = every CBD component + every `Static` component of the SKU mapping (S-17). A `Dynamic`
   component counts only when it is in the CBD. Components unknown to the catalog cannot be checked: they never
   cause a skip and are listed in `unknownComponents`.
2. A counted component that is `outOfStock` in the factory (shared stock rule) skips the factory unless a
   "For balancer" alternative is in stock there → skip `ComponentOos` with the component.
3. A process mapped to the SKU with `capacity_pct = 0` in the factory → skip `ProcessOffline` with the process.
   Partial capacity (10–90 %) and "runs here" are ignored (P-10, S-18).
4. Every skip cause is written to `balancer_skip_log` (order, order item, SKU, factory, reason, component or
   process). The same cause for the same order item and factory is logged once per UTC day, so retries do not
   multiply rows.
5. `firstEligibleFactoryId` is the first candidate without skips. When none qualifies `anyEligible` is false and
   the caller keeps its current behaviour (the skips are already logged).

```json
{
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
  "anyEligible": true,
  "firstEligibleFactoryId": "110",
  "message": null,
  "factories": [
    { "factoryId": "125", "eligible": false, "skips": [
      { "reason": "ComponentOos", "componentErpId": "R.VM.BRD.08MM", "processId": null },
      { "reason": "ProcessOffline", "componentErpId": null, "processId": "ENGRAVING" }
    ] },
    { "factoryId": "110", "eligible": true, "skips": [] },
    { "factoryId": "315", "eligible": true, "skips": [] }
  ],
  "unknownComponents": []
}
```

No factory qualifies: `"anyEligible": false, "firstEligibleFactoryId": null,
"message": "No candidate factory qualifies; keep the current allocation behaviour."`

## `POST /v1/legacy-balancer/skip-log/search` and `/export`

Standard list / export requests. `search` matches the order id (contains). Filters: `orderId`, `orderItemId`,
`skuUuid`, `factoryId`, `reason` (`ComponentOos`, `ProcessOffline`), `componentErpId`, `processId`, `createdAt`.
Sort keys: `createdAt` (default), `orderId`, `orderItemId`, `factoryId`, `reason`, `componentErpId`, `processId`.

```json
{ "search": "ORD456789", "sortBy": "createdAt", "sortDirection": "desc" }
```

```json
{
  "items": [
    { "id": 17, "orderId": "ORD456789", "orderItemId": "ITM001", "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
      "factoryId": "125", "reason": "ComponentOos", "componentErpId": "R.VM.BRD.08MM", "processId": null,
      "createdAt": "2026-11-24T10:00:00Z" }
  ],
  "totalCount": 1
}
```

Export: `balancer_skip_log_{yyyyMMdd_HHmmss}.xlsx`; column keys `createdAt`, `orderId`, `orderItemId`, `skuUuid`,
`factoryId`, `reason`, `componentErpId`, `processId`.

## `GET /v1/legacy-balancer/country-sku-lists?factoryId=`

The current list lives in TG Supplier as `CountryBalanceSku { Sku, Producible, Removed }` per country balance
(`CountryBalanceSkuResponse { countryName, skus[{ id, sku, producible, removed }] }` in
`Tenengroup.Supplier.Api/Models/CountryBalance`); the balancer matches `SupplierItemId.Trim().ToLower()` against
the non-removed `sku` values. The endpoint keeps those field names and generates one list per active factory
(optionally only `factoryId`; unknown → 404):

```json
[
  {
    "factoryId": "110",
    "countryName": "Factory 110",
    "countryCode": null,
    "skus": [
      { "id": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90", "sku": "TGI-N-4609-SD-VMGP_1.1", "producible": true, "removed": false },
      { "id": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90", "sku": "TGN-N-4609-SD-VMGP_1.1", "producible": true, "removed": false }
    ]
  }
]
```

- A list contains the manufactured SKUs that have a `sku_factory` row for the factory, one entry per identifier:
  the Site SKU and every alias (legacy factory ids resolve through the aliases, S-16). Dropshipper SKUs are left out.
- `id` is the SKU UUID (the legacy row id was a list-row guid), `producible` is `sku_factory.producible`,
  `removed` is true when the SKU is retired or not producible there, so a consumer that only looks at membership
  (not removed) and one that looks at `producible` get the same answer.
- Entries are ordered by `sku`.

---

## Data

Tables written by this module: `quote_set`, `quote`, `balancer_skip_log` (see `spec/backend/database.md`). No
entity or configuration was changed. Everything else is read through `IFactoryCatalogDbContext`.

Configuration (optional, read in `QuotesModule`): `Quotes:ValidityMinutes` (default 15), `Quotes:Currency`
(default `USD`). No recurring jobs.

## Code map

| Layer | Files |
|---|---|
| Contracts | `Business.Abstractions/Quotes/*`, `Business.Abstractions/LegacyBalancer/*` |
| Quote engine | `Business/Quotes/RequestQuotesCommandHandler.cs`, `ComponentStock.cs`, `ShareRedistribution.cs`, `SkuLookup.cs`, `QuoteMapper.cs` |
| Lifecycle and reads | `Business/Quotes/{SelectQuote,MarkQuoteProduced,MarkQuoteTransferred}CommandHandler.cs`, `{GetQuoteSet,SearchQuotes,GetDropshipperCost}QueryHandler.cs` |
| Legacy balancer | `Business/LegacyBalancer/*` |
| HTTP | `Api/Controllers/Quotes/QuotesController.cs`, `Api/Controllers/LegacyBalancer/LegacyBalancerController.cs`, `Api/Models/{Quotes,LegacyBalancer}`, `Api/Mappers/Quotes` |
| DI | `CompositionRoot/Modules/{Quotes,LegacyBalancer}Module.cs` |
| Tests | `tests/unit-tests/…/Quotes`, `tests/unit-tests/…/LegacyBalancer` (130 tests) |

## Story coverage

| Story | Status | Notes |
|---|---|---|
| B-01 Production quote per order item | done | `POST /v1/quotes`, idempotent on `requestId`, unique `quoteKey`, stored, `GET /v1/quotes/{quoteKey}` |
| B-02 CBD-only stock check and pricing | done | processes from the SKU mapping; unknown CBD component → `UNKNOWN_COMPONENT` on every factory |
| B-03 Producible + reason, costs, capacity signal, share | done | signal formula and missing-cost handling are assumptions (Q-6, Q-8) |
| B-07 Dropshipper cost (catalog side) | done | `GET /v1/quotes/dropshipper-cost`; dropshipper SKUs get `DROPSHIPPER` quotes |
| B-10 Mark the winning quote selected (catalog side) | done | `POST /v1/quotes/{quoteId}/select`; the event is the Balancer's |
| B-16 Estimate (catalog side) | done | `mode: "estimate"` stores nothing |
| B-19 Shadow mode (catalog side) | done | `mode: "shadow"` is stored and flagged |
| B-21 Mark produced, reproductions (catalog side) | done | `POST /v1/quotes/{quoteId}/produced`; the `balancer.quote.produced` event is the Balancer's |
| B-22 Daily plan input | done | `POST /v1/quotes/search`; the plan itself is the Balancer's |
| B-23 / B-24 Transfer marking (catalog side) | done | `POST /v1/quotes/{quoteId}/transferred`; search gives the audit links |
| C-17 Legacy OOS check | done (catalog side) | endpoint only; the call from `supplier_api` is outside this repository |
| C-18 Why a factory was skipped | partial | log, search and Excel export exist; no UI page was requested for this module |
| P-10 / S-18 Process at 0 % blocks the SKU | done (catalog side) | part of the eligibility endpoint; partial capacity ignored |
| P-14 Remaining capacity today | done (read side) | used for `remainingPct` and the signal; uses the Processes module's `IProcessUsageReader` and `ProcessCapacityCalculator` |
| S-11 Invalid SKUs excluded from quotes | done | `SKU_INVALID` |
| S-15 Share redistribution | done | applied per quote set, stored shares untouched |
| S-16 Country SKU lists | partial | generated on request in the TG Supplier field names; how TG Supplier pulls or stores them and the factory ↔ country-balance mapping are open (Q-17) |
| S-17 Static / Dynamic rule in the legacy check | done | |
| S-20 Production log | partial | the reported log is stored on the quote; deriving "Production conflict" belongs to the SKUs module |
| P-11 Packing in every quote | partial | packing is priced and reported separately when the SKU maps `PACKING`; otherwise 0 with `PACKING_NOT_MAPPED` (Q-9) |

## Unverified

- No PostgreSQL run: handlers are unit-tested against mocked `DbSet`s; the SQL of the non-trivial queries
  (used-today aggregation, quote search with filters and paging, skip-log filters) was generated with
  `ToQueryString()` against the Npgsql provider and translates, but was not executed.
- The HTTP layer (model binding, enum casing, auth attributes) has no automated test.
