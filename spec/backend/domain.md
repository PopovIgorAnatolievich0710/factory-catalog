# Backend — Domain

Entities live in `Tenengroup.FactoryCatalog.Domains/<Module>`. This file covers the shared part; each module
documents its own entities and rules in `spec/modules/*.md`. Columns: [`database.md`](database.md).

## Shared (`Domains/Shared`)

| Entity | Purpose |
|---|---|
| `Factory` | Production factory (`Id` is the string id used everywhere, e.g. `"110"`) |
| `UserFactory` | User email → owned factory ("own factory" rule) |
| `UserListSettings` | Column picker memory per user and list; `MaxColumns = 8` |
| `HistoryEntry` | Field-level change log; `HistorySource`: `User`, `Sync`, `Import`, `System`; entity types in `HistoryEntityTypes` |
| `SyncRun` | Sync execution; `SyncRunStatus`: `Running`, `Succeeded`, `Failed`; types in `SyncTypes` |
| `FeatureToggle` | Global toggle; names in `FeatureToggleNames` |

## Module entities (owned by the modules)

| Module | Entities | Enums | Module spec |
|---|---|---|---|
| Components | `Component`, `ComponentFactory`, `ComponentAlternative`, `ComponentReplacement` | `ComponentStatus` | [`1-components.md`](../modules/1-components.md) |
| Processes | `Process`, `ProcessFactory` | `ProcessType`, `ProcessStatus`, `ProcessUnit` | [`2-process-capacity.md`](../modules/2-process-capacity.md) |
| Skus | `Sku`, `SkuAlias`, `SkuComponent`, `SkuProcess`, `SkuFactory`, `GlobalFactoryShare` | `SkuProductType`, `SkuStatus`, `SkuValidationStatus`, `SkuAliasType`, `ComponentUsage` | [`3-skus.md`](../modules/3-skus.md) |
| Quotes | `QuoteSet`, `Quote` | `QuoteMode`, `QuoteStatus` | [`4a-quotes.md`](../modules/4a-quotes.md) |
| LegacyBalancer | `BalancerSkipLog` | `SkipReason` | [`4a-quotes.md`](../modules/4a-quotes.md) |

The Balancer service has its own domain (`Tenengroup.Balancer.Domains`: allocations, transfers, production
records, settings): [`modules/4b-balancer.md`](../modules/4b-balancer.md).

## Rules that span modules

- Natural keys: component `erpId`, process `processId`, SKU `skuUuid` (master catalog UUID), factory `id`.
- Derived values are never stored: component `stockStatus` / `oosConflict` / `actionRequired` / `usedInSkus`,
  process `today {available, used, remainingPct}`, SKU `defaultSharePct` and the effective (redistributed) share,
  SKU `productionConflict`.
- `sku_component` and `sku_process` have no foreign key to `component` / `process`: a mapping to something
  unknown makes the SKU `Invalid` (S-11) instead of failing the sync.
- Manual OOS is never touched by the SAP sync (C-12); capacity changes only by hand (P-06).
- "Used today" of a process (P-14) is derived from quotes with status `Produced` and `produced_at` today in that
  factory, joined with the SKU's process mapping.
- Production feedback (decision of Nov 24): the balancer consumes TG Supplier sent / reproduced events and tells
  the catalog to mark the selected quote produced. The catalog stores `status`, `produced_at`,
  `reproduction_of_quote_id` and the reported `production_log` on `quote`; it has no event consumer of its own.
