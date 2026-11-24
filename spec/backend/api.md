# Backend — REST API

Base URL: `http://localhost:5010` (Factory Catalog API). Swagger UI at `/`. All routes are under `/v1` and require
authentication (Azure AD bearer token or Basic). Module endpoints are documented in `spec/modules/*.md`:
[components](../modules/1-components.md), [processes and capacity](../modules/2-process-capacity.md),
[SKUs and factory share](../modules/3-skus.md), [quotes and legacy balancer](../modules/4a-quotes.md). The Balancer
API (`http://localhost:5011`, same conventions) is documented in [`modules/4b-balancer.md`](../modules/4b-balancer.md).

## Conventions

- JSON is camelCase. Enums are strings with the C# member name (`"Active"`, `"Running"`, `"User"`).
- Dates are ISO-8601 UTC (`2026-11-24T10:00:00Z`); date-only values are `yyyy-MM-dd`.
- Every response carries `X-Correlation-Id` (taken from the request header when present).

| Purpose | Endpoint | Shape |
|---|---|---|
| List | `POST /v1/{resource}/search` | list request → `{ items: [], totalCount }` |
| Excel export of the current view | `POST /v1/{resource}/export` | list request + `columns: string[]` → xlsx named `{page}_{yyyyMMdd_HHmmss}.xlsx` |
| Single | `GET /v1/{resource}/{id}` | entity DTO |

### List request

```json
{
  "page": 1,
  "pageSize": 50,
  "search": "gold",
  "sortBy": "name",
  "sortDirection": "asc",
  "filters": {
    "family": { "values": ["Board", "Chain"] },
    "totalStock": { "operator": "between", "value": "2", "valueTo": "10" },
    "name": { "operator": "contains", "value": "white" }
  }
}
```

| Field | Notes |
|---|---|
| `page` | 1-based; values below 1 are treated as 1 |
| `pageSize` | default 50, maximum 500 |
| `search` | free text; each list documents the fields it searches |
| `sortBy` / `sortDirection` | field key of the list; `"asc"` (default) or `"desc"`; unknown field → the list's default sort |
| `filters` | one entry per column key (case-insensitive) |

Filter object `{ values?, operator?, value?, valueTo? }`:

| Member | Meaning |
|---|---|
| `values: string[]` | restricted-value columns (family, status, type, factory, usage, validation): the row matches any of the values; takes precedence over `operator` |
| `operator` | `"eq"`, `"ne"`, `"lt"`, `"lte"`, `"gt"`, `"gte"`, `"between"` for numbers and dates; `"contains"` (case-insensitive) and `"eq"` / `"ne"` for text. Default: `contains` for text, `eq` otherwise |
| `value` | operand as a string, invariant culture (`"12.5"`, `"2026-11-28"`) |
| `valueTo` | upper bound for `between` (inclusive on both ends) |

An entry with neither `values` nor `value` is ignored. A value that cannot be parsed, an unknown operator or an
operator that does not fit the column type returns 422 with the key `filters.{field}`.

### Export request

The list request plus `"columns": ["erpId", "name", ...]` — column keys in the order to export (empty = all
columns of the list). Paging is ignored: the export contains every row matching search and filters. Response:
`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` with
`Content-Disposition: attachment; filename={page}_{yyyyMMdd_HHmmss}.xlsx`.

### Errors

| Status | When | Body |
|---|---|---|
| 401 | not authenticated | empty |
| 403 | permission or own-factory check failed | empty (attribute) or `{ "message" }` (handler) |
| 404 | entity not found | `{ "message" }` |
| 409 | conflict, e.g. a sync of that type is already running | `{ "message" }` |
| 422 | validation failed (model binding or handler) | `{ "message": "Validation failed", "errors": { "field": ["..."] } }` |
| 500 | unhandled | `{ "message" }` |

## Shared endpoints

All of them need only an authenticated caller (`[AzureAuthorize]`).

### `GET /v1/user/me`

```json
{ "email": "fm.110@tenengroup.com", "name": "Avi Levi", "roles": ["FC-FactoryManagers"], "factoryIds": ["110"] }
```

`roles` are the role names of `spec/backend/permissions.md` (AD group names), resolved from the token groups
through `GroupPermissions.json`; empty when the document is unavailable or the user has no recognised group.
`factoryIds` come from `catalog.user_factory`. Basic callers get `email: ""`, `roles: []`, `factoryIds: []`.

### `GET /v1/user/list-settings/{listKey}`

`{ "columns": ["material", "unitCost"] }` — the extra columns the user picked for the list; `[]` when nothing is saved.

### `PUT /v1/user/list-settings/{listKey}`

Body `{ "columns": string[] }`, returns the saved `{ "columns" }`. Blank entries are dropped, values are trimmed
and de-duplicated (case-insensitive). More than 8 columns → 422 (`errors.columns`). Settings are stored per user
email; a caller without an email (Basic) gets 422 (`errors.user`).

### `GET /v1/factories`

```json
[{ "id": "110", "name": "Factory 110", "countryCode": null, "isActive": true }]
```

Ordered by `id`.

### `GET /v1/history/{entityType}/{entityId}?page=1&pageSize=50`

`entityType`: `component`, `process`, `sku`, `global-factory-share`. `entityId`: `erpId`, `processId`, `skuUuid`.
Newest first.

```json
{
  "items": [
    { "changedBy": "sc@tenengroup.com", "changedAt": "2026-11-24T10:00:00Z", "source": "User",
      "factoryId": "125", "field": "criticalStockLevel", "oldValue": "2", "newValue": "5" }
  ],
  "totalCount": 1
}
```

`source`: `User`, `Sync`, `Import`, `System`.

### `GET /v1/sync/{syncType}/status`

`syncType`: `sap-components`, `master-catalog-skus`.

```json
{
  "syncType": "sap-components", "status": "Succeeded",
  "startedAt": "2026-11-24T02:00:00Z", "finishedAt": "2026-11-24T02:04:11Z", "lastSuccessAt": "2026-11-24T02:04:11Z",
  "added": 3, "updated": 120, "failed": 0, "error": null
}
```

`status`: `Running`, `Succeeded`, `Failed`, or `null` when the sync has never run (counts are then 0). The
values describe the latest run; `lastSuccessAt` is the finish time of the latest successful run.

### `GET /v1/feature-toggle`

Same envelope as the shipment service:

```json
{ "data": [{ "id": "…", "name": "permissions.enable_all", "isEnabled": true, "description": null,
             "createdAt": "…", "updatedAt": "…" }] }
```

### Other

| Endpoint | Notes |
|---|---|
| `GET /ping` | anonymous health probe, returns `pong` |
| `GET /hangfire` | Hangfire dashboard, Basic auth with the API service credentials |
