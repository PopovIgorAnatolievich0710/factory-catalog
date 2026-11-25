# Backend — Database

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

PostgreSQL 15+, database `factory_catalog`. This file documents the `catalog` schema owned by the Factory Catalog
API (`FactoryCatalogDbContext`). The `balancer` schema is documented in
[`modules/4b-balancer.md`](../modules/4b-balancer.md) (section "Database — schema `balancer`"); `hangfire` and
`hangfire_balancer` are created by Hangfire. Module rules behind the columns are in `spec/modules/*.md`.

## Conventions

- snake_case table and column names (`UseSnakeCaseNamingConvention()`), singular table names.
- Natural string keys where the business has one: `factory.id`, `component.erp_id`, `process.process_id`,
  `sku.sku_uuid` (uuid from the master catalog). Log-like tables use `bigint` identity ids.
- Enums are stored as their C# member name in `character varying` columns.
- Timestamps are `timestamp with time zone` in UTC; date-only values are `date`.
- Structured snapshots are `jsonb` (`quote_set.cbd`, `quote.components`, `quote.processes`,
  `quote.production_log`); string lists are `text[]`.
- User columns (`*_by`, `user_email`) hold the lower-cased email (320 chars) or a system actor such as `SAP sync`.
- Derived values are not stored (see `domain.md`).

## Migrations

| Migration | Content |
|---|---|
| `Initial` | whole `catalog` schema below, seed rows for `factory` (110, 125, 315) and `process` (`PACKING`) |

Module owners do not add migrations; `Initial` is regenerated after the module branches are merged:

```bash
dotnet ef migrations add Initial --context FactoryCatalogDbContext \
  --project backend/src/app/Tenengroup.FactoryCatalog.DataAccess \
  --startup-project backend/src/app/Tenengroup.FactoryCatalog.DataAccess
```

History table: `catalog.__EFMigrationsHistory`.

## Tables by module

| Module | Tables | Module spec |
|---|---|---|
| Shared | `factory`, `user_factory`, `user_list_settings`, `history_entry`, `sync_run`, `feature_toggle` | — |
| Components | `component`, `component_factory`, `component_alternative`, `component_replacement` | [`1-components.md`](../modules/1-components.md) |
| Processes | `process`, `process_factory` | [`2-process-capacity.md`](../modules/2-process-capacity.md) |
| SKUs | `sku`, `sku_alias`, `sku_component`, `sku_process`, `sku_factory`, `global_factory_share` | [`3-skus.md`](../modules/3-skus.md) |
| Quotes | `quote_set`, `quote` | [`4a-quotes.md`](../modules/4a-quotes.md) |
| Legacy balancer | `balancer_skip_log` | [`4a-quotes.md`](../modules/4a-quotes.md) |
| Balancer service (schema `balancer`) | see module spec | [`4b-balancer.md`](../modules/4b-balancer.md) |

`production_event` from the plan was dropped: after the decision of Nov 24 the balancer consumes the TG Supplier
events and the catalog only stores the produced marking on `quote`.

## Column notes

| Column | Meaning |
|---|---|
| `component.critical_stock_level` | global critical level; `component_factory.critical_stock_level_override` wins when set (C-09) |
| `component.measured_by / measure_value / measure_unit` | `measure {measuredBy, value, unit}` |
| `component.unit_of_measure_type / _quantity / _unit` | `unitOfMeasure {type, quantity, unit}` (stock unit) |
| `component.unit_cost_amount / _currency / _source` | `unitCost {amount, currency, source}` |
| `component_factory.manual_oos_*` | `manualOos {active, setBy, setAt, note}`; never changed by the SAP sync |
| `component_alternative.for_balancer / for_planner` | the two use flags; the row means `alternative_erp_id` may replace `component_erp_id` (one-way) |
| `process.measured_in` | `Minutes`, `Seconds`, `Hours`, `Units`, `Pieces`, `Cm`, `Mm`, `Cm2`, `Mm2` |
| `process_factory.capacity_pct` | 0…100 in steps of 10, default 100 |
| `process_factory.capacity_reason / expected_recovery` | `capacityNote`, information only |
| `process_factory.max_per_day` | what 100% means, order items per day (P-13) |
| `process_factory.labor_cost_*` | `laborCost {amountPerUnit, currency, effectiveFrom}` |
| `sku.site_sku` | live identifier used on the sites; legacy factory SKUs are rows of `sku_alias` |
| `sku.validation_status / validation_issues` | result of the mapping validation (S-11) |
| `sku_component.usage` | `Static` or `Dynamic` |
| `sku_process.quantity` | quantity in the process unit; there is no step order |
| `sku_factory.override_share_pct` | null = the default share from `global_factory_share` applies |
| `quote_set.request_id` | idempotency key of `POST /v1/quotes` |
| `quote.quote_id` | public id, e.g. `PQ-ORD456789-ITM001-110` |
| `quote.reasons` | `UPPER_SNAKE` reason codes of the quote (`quotes[].reasons[]`, e.g. `COMPONENT_OOS`); see "Reason codes" in [`modules/4a-quotes.md`](../modules/4a-quotes.md) |
| `quote.status` | `Quoted` → `Selected` → `Produced`; `Transferred` when the item moved to another factory |
| `quote.reproduction_of_quote_id` | set on the produced record created for a reproduction |
| `quote.production_log` | processes and machines actually used, as reported with the "sent" event (S-20) |

## Tables

### `balancer_skip_log`

Why the legacy balancer skipped a factory (C-17, C-18, S-17, S-18).

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `order_id` | character varying(100) | no |  |
| `order_item_id` | character varying(100) | yes |  |
| `sku_uuid` | uuid | yes |  |
| `factory_id` | character varying(20) | no |  |
| `reason` | character varying(30) | no |  |
| `component_erp_id` | character varying(100) | yes |  |
| `process_id` | character varying(50) | yes |  |
| `created_at` | timestamp with time zone | no |  |

Primary key: `id`.
Indexes:
- `ix_balancer_skip_log_created_at` (created_at)
- `ix_balancer_skip_log_order_id` (order_id)

### `component`

Component master from SAP (read only) plus the catalog-owned global critical stock level.

| Column | Type | Null | Default |
|---|---|---|---|
| `erp_id` | character varying(100) | no |  |
| `name` | character varying(300) | no |  |
| `status` | character varying(20) | no |  |
| `family` | character varying(100) | yes |  |
| `material` | character varying(200) | yes |  |
| `measured_by` | character varying(50) | yes |  |
| `measure_value` | numeric(18,4) | yes |  |
| `measure_unit` | character varying(20) | yes |  |
| `unit_of_measure_type` | character varying(50) | yes |  |
| `unit_of_measure_quantity` | numeric(18,4) | yes |  |
| `unit_of_measure_unit` | character varying(20) | yes |  |
| `country_of_origin` | character varying(2) | yes |  |
| `hs_code` | character varying(20) | yes |  |
| `unit_cost_amount` | numeric(18,4) | yes |  |
| `unit_cost_currency` | character varying(3) | yes |  |
| `unit_cost_source` | character varying(50) | yes |  |
| `critical_stock_level` | numeric(18,4) | yes |  |
| `last_sap_sync_at` | timestamp with time zone | yes |  |
| `created_at` | timestamp with time zone | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `erp_id`.
Indexes:
- `ix_component_family` (family)
- `ix_component_name` (name)
- `ix_component_status` (status)

### `component_replacement`

Replacement chosen on the TG Supplier replacement screen (C-16), kept for SAP consumption (MCM).

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `order_id` | character varying(100) | no |  |
| `order_item_id` | character varying(100) | no |  |
| `factory_id` | character varying(20) | no |  |
| `original_erp_id` | character varying(100) | no |  |
| `replacement_erp_id` | character varying(100) | no |  |
| `quantity` | numeric(18,4) | yes |  |
| `replaced_by` | character varying(320) | no |  |
| `replaced_at` | timestamp with time zone | no |  |
| `created_at` | timestamp with time zone | no |  |

Primary key: `id`.
Indexes:
- `ix_component_replacement_order_id_order_item_id` (order_id, order_item_id)
- `ix_component_replacement_original_erp_id` (original_erp_id)

### `factory`

Production factories. Seeded: 110, 125, 315. Maintained by migration/seed only.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | character varying(20) | no |  |
| `name` | character varying(200) | no |  |
| `country_code` | character varying(2) | yes |  |
| `is_active` | boolean | no |  |

Primary key: `id`.

### `feature_toggle`

Global feature toggles. `permissions.enable_all` switches role enforcement on.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | uuid | no | `gen_random_uuid()` |
| `name` | character varying(100) | no |  |
| `is_enabled` | boolean | no |  |
| `description` | character varying(500) | yes |  |
| `created_at` | timestamp with time zone | no | `now()` |
| `updated_at` | timestamp with time zone | no | `now()` |

Primary key: `id`.
Indexes:
- `ix_feature_toggle_name` (name) unique

### `history_entry`

Field-level audit log for every entity (Audit tabs). Written by `IHistoryWriter`.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `entity_type` | character varying(50) | no |  |
| `entity_id` | character varying(100) | no |  |
| `factory_id` | character varying(20) | yes |  |
| `field` | character varying(100) | no |  |
| `old_value` | text | yes |  |
| `new_value` | text | yes |  |
| `changed_by` | character varying(320) | no |  |
| `changed_at` | timestamp with time zone | no |  |
| `source` | character varying(20) | no |  |

Primary key: `id`.
Indexes:
- `ix_history_entry_entity_type_entity_id_changed_at` (entity_type, entity_id, changed_at)

### `process`

Machine and work processes. Seeded: `PACKING` (work).

| Column | Type | Null | Default |
|---|---|---|---|
| `process_id` | character varying(50) | no |  |
| `name` | character varying(200) | no |  |
| `type` | character varying(20) | no |  |
| `measured_in` | character varying(20) | no |  |
| `description` | character varying(1000) | yes |  |
| `status` | character varying(20) | no |  |
| `created_at` | timestamp with time zone | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `process_id`.
Indexes:
- `ix_process_name` (name) unique

### `quote_set`

One production quote request (per order item) and its key.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `quote_key` | character varying(200) | no |  |
| `request_id` | uuid | no |  |
| `order_id` | character varying(100) | no |  |
| `order_item_id` | character varying(100) | no |  |
| `sku_uuid` | uuid | no |  |
| `quantity` | integer | no |  |
| `mode` | character varying(20) | no |  |
| `cbd` | jsonb | no |  |
| `valid_until` | timestamp with time zone | no |  |
| `created_at` | timestamp with time zone | no |  |

Primary key: `id`.
Indexes:
- `ix_quote_set_order_id_order_item_id` (order_id, order_item_id)
- `ix_quote_set_quote_key` (quote_key) unique
- `ix_quote_set_request_id` (request_id) unique

### `sku`

SKUs synced read-only from the master catalog, including the limited dropshipper record and validation result.

| Column | Type | Null | Default |
|---|---|---|---|
| `sku_uuid` | uuid | no |  |
| `site_sku` | character varying(200) | no |  |
| `name` | character varying(500) | no |  |
| `product_type` | character varying(20) | no |  |
| `status` | character varying(20) | no |  |
| `dropshipper_id` | character varying(100) | yes |  |
| `dropshipper_name` | character varying(200) | yes |  |
| `dropshipper_total_cost_amount` | numeric(18,4) | yes |  |
| `dropshipper_total_cost_currency` | character varying(3) | yes |  |
| `validation_status` | character varying(20) | no |  |
| `last_master_sync_at` | timestamp with time zone | yes |  |
| `created_at` | timestamp with time zone | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `sku_uuid`.
Indexes:
- `ix_sku_name` (name)
- `ix_sku_product_type_status` (product_type, status)
- `ix_sku_site_sku` (site_sku)
- `ix_sku_validation_status` (validation_status)

### `sync_run`

One row per sync execution (`sap-components`, `master-catalog-skus`). A partial unique index allows one `Running` run per type.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `sync_type` | character varying(50) | no |  |
| `status` | character varying(20) | no |  |
| `started_at` | timestamp with time zone | no |  |
| `finished_at` | timestamp with time zone | yes |  |
| `added` | integer | no |  |
| `updated` | integer | no |  |
| `failed` | integer | no |  |
| `error` | text | yes |  |
| `triggered_by` | character varying(320) | no |  |

Primary key: `id`.
Indexes:
- `ix_sync_run_sync_type_started_at` (sync_type, started_at)
- `ux_sync_run_sync_type_running` (sync_type) unique where `status = 'Running'`

### `user_list_settings`

Column-picker memory per user and list (`columns` holds at most 8 extra columns).

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `user_email` | character varying(320) | no |  |
| `list_key` | character varying(100) | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `id`.
Indexes:
- `ix_user_list_settings_user_email_list_key` (user_email, list_key) unique

### `component_alternative`

Approved one-way alternatives (`alternative_erp_id` may replace `component_erp_id`) with the two use flags.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `component_erp_id` | character varying(100) | no |  |
| `alternative_erp_id` | character varying(100) | no |  |
| `for_balancer` | boolean | no |  |
| `for_planner` | boolean | no |  |
| `created_by` | character varying(320) | no |  |
| `created_at` | timestamp with time zone | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `id`.
Foreign keys: `alternative_erp_id` -> `component.erp_id` (restrict); `component_erp_id` -> `component.erp_id` (cascade).
Indexes:
- `ix_component_alternative_alternative_erp_id` (alternative_erp_id)
- `ix_component_alternative_component_erp_id_alternative_erp_id` (component_erp_id, alternative_erp_id) unique

### `component_factory`

Per-factory stock snapshot (SAP) and catalog-owned critical level override and manual OOS flag.

| Column | Type | Null | Default |
|---|---|---|---|
| `component_erp_id` | character varying(100) | no |  |
| `factory_id` | character varying(20) | no |  |
| `warehouse_stock` | numeric(18,4) | no |  |
| `production_floor_stock` | numeric(18,4) | no |  |
| `stock_synced_at` | timestamp with time zone | yes |  |
| `critical_stock_level_override` | numeric(18,4) | yes |  |
| `manual_oos_active` | boolean | no |  |
| `manual_oos_set_by` | character varying(320) | yes |  |
| `manual_oos_set_at` | timestamp with time zone | yes |  |
| `manual_oos_note` | character varying(1000) | yes |  |

Primary key: `component_erp_id`, `factory_id`.
Foreign keys: `component_erp_id` -> `component.erp_id` (cascade); `factory_id` -> `factory.id` (restrict).
Indexes:
- `ix_component_factory_factory_id_manual_oos_active` (factory_id, manual_oos_active)

### `global_factory_share`

Global default share per factory (Settings). Must total 100%.

| Column | Type | Null | Default |
|---|---|---|---|
| `factory_id` | character varying(20) | no |  |
| `pct` | numeric(5,2) | no |  |
| `updated_by` | character varying(320) | no |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `factory_id`.
Foreign keys: `factory_id` -> `factory.id` (restrict).

### `user_factory`

Binds a user (lower-cased email) to the factories they own ("own factory" rule). Maintained by the catalog admin.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `user_email` | character varying(320) | no |  |
| `factory_id` | character varying(20) | no |  |
| `created_by` | character varying(320) | no |  |
| `created_at` | timestamp with time zone | no | `now()` |

Primary key: `id`.
Foreign keys: `factory_id` -> `factory.id` (restrict).
Indexes:
- `ix_user_factory_factory_id` (factory_id)
- `ix_user_factory_user_email_factory_id` (user_email, factory_id) unique

### `process_factory`

Per-factory setup, capacity and labor cost of a process.

| Column | Type | Null | Default |
|---|---|---|---|
| `process_id` | character varying(50) | no |  |
| `factory_id` | character varying(20) | no |  |
| `runs_here` | boolean | no |  |
| `machine_type` | character varying(100) | yes |  |
| `capacity_pct` | integer | no | `100` |
| `capacity_reason` | character varying(1000) | yes |  |
| `expected_recovery` | date | yes |  |
| `capacity_updated_by` | character varying(320) | yes |  |
| `capacity_updated_at` | timestamp with time zone | yes |  |
| `max_per_day` | numeric(18,4) | yes |  |
| `labor_cost_amount` | numeric(18,4) | yes |  |
| `labor_cost_currency` | character varying(3) | yes |  |
| `labor_cost_effective_from` | date | yes |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `process_id`, `factory_id`.
Foreign keys: `factory_id` -> `factory.id` (restrict); `process_id` -> `process.process_id` (cascade).
Check `ck_process_factory_capacity_pct`: `capacity_pct BETWEEN 0 AND 100 AND capacity_pct % 10 = 0`.
Indexes:
- `ix_process_factory_factory_id` (factory_id)

### `quote`

One quote per factory in a quote set; status runs Quoted -> Selected -> Produced (or Transferred).

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `quote_id` | character varying(220) | no |  |
| `quote_set_id` | bigint | no |  |
| `factory_id` | character varying(20) | no |  |
| `producible` | boolean | no |  |
| `reasons` | text[] | no |  |
| `material_cost` | numeric(18,4) | yes |  |
| `labor_cost` | numeric(18,4) | yes |  |
| `packing_cost` | numeric(18,4) | yes |  |
| `total_cost` | numeric(18,4) | yes |  |
| `currency` | character varying(3) | yes |  |
| `capacity_signal_pct` | integer | yes |  |
| `share_pct` | numeric(5,2) | no |  |
| `components` | jsonb | no |  |
| `processes` | jsonb | no |  |
| `status` | character varying(20) | no |  |
| `selected_at` | timestamp with time zone | yes |  |
| `produced_at` | timestamp with time zone | yes |  |
| `transferred_at` | timestamp with time zone | yes |  |
| `reproduction_of_quote_id` | bigint | yes |  |
| `production_log` | jsonb | yes |  |
| `created_at` | timestamp with time zone | no |  |

Primary key: `id`.
Foreign keys: `factory_id` -> `factory.id` (restrict); `reproduction_of_quote_id` -> `quote.id` (restrict); `quote_set_id` -> `quote_set.id` (cascade).
Indexes:
- `ix_quote_factory_id_status_produced_at` (factory_id, status, produced_at)
- `ix_quote_quote_id` (quote_id) unique
- `ix_quote_quote_set_id` (quote_set_id)
- `ix_quote_reproduction_of_quote_id` (reproduction_of_quote_id)

### `sku_alias`

Legacy factory-version SKUs (TGN/TGO/TGH), deprecated read-only aliases.

| Column | Type | Null | Default |
|---|---|---|---|
| `id` | bigint | no | identity |
| `sku_uuid` | uuid | no |  |
| `alias` | character varying(200) | no |  |
| `type` | character varying(20) | no |  |
| `deprecated` | boolean | no |  |

Primary key: `id`.
Foreign keys: `sku_uuid` -> `sku.sku_uuid` (cascade).
Indexes:
- `ix_sku_alias_alias` (alias) unique
- `ix_sku_alias_sku_uuid` (sku_uuid)

### `sku_component`

SKU to component mapping from the master catalog (no FK to `component`: unknown components make the SKU invalid, S-11).

| Column | Type | Null | Default |
|---|---|---|---|
| `sku_uuid` | uuid | no |  |
| `component_erp_id` | character varying(100) | no |  |
| `quantity` | numeric(18,4) | no |  |
| `usage` | character varying(20) | no |  |

Primary key: `sku_uuid`, `component_erp_id`.
Foreign keys: `sku_uuid` -> `sku.sku_uuid` (cascade).
Indexes:
- `ix_sku_component_component_erp_id` (component_erp_id)

### `sku_factory`

Per-factory producible flag and share override of a SKU.

| Column | Type | Null | Default |
|---|---|---|---|
| `sku_uuid` | uuid | no |  |
| `factory_id` | character varying(20) | no |  |
| `producible` | boolean | no |  |
| `override_share_pct` | numeric(5,2) | yes |  |
| `override_by` | character varying(320) | yes |  |
| `override_at` | timestamp with time zone | yes |  |
| `updated_at` | timestamp with time zone | no |  |

Primary key: `sku_uuid`, `factory_id`.
Foreign keys: `factory_id` -> `factory.id` (restrict); `sku_uuid` -> `sku.sku_uuid` (cascade).
Indexes:
- `ix_sku_factory_factory_id_producible` (factory_id, producible)

### `sku_process`

SKU to process mapping from the migration import; quantity in the process unit, no step order.

| Column | Type | Null | Default |
|---|---|---|---|
| `sku_uuid` | uuid | no |  |
| `process_id` | character varying(50) | no |  |
| `quantity` | numeric(18,4) | no |  |

Primary key: `sku_uuid`, `process_id`.
Foreign keys: `sku_uuid` -> `sku.sku_uuid` (cascade).
Indexes:
- `ix_sku_process_process_id` (process_id)
