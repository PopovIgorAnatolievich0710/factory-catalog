# Open questions and assumed answers

Every row below is already implemented with the "Implemented (recommended)" variant. Write your answer in the last
column; any answer that differs from the recommendation gets re-implemented.

| Section | Rows |
|---|---|
| 1. Blocking for go-live | 18 (OQ-1 – OQ-18) |
| 2. Business rules likely to cause rework | 25 (OQ-19 – OQ-43) |
| 3. Per module: Components 7, Process capacity 12, SKUs 12, Quotes / legacy 6, Balancer 8, UI 8, Architecture and permissions 12 | 65 (OQ-44 – OQ-108) |
| 4. Technical decisions (FYI, no answer needed) | 38 bullets (6 already decided by the business, 32 technical) |

Source ids point to the detailed per-agent files, which stay as the reference: `A-*`, `I-*` (earlier version of this
file), `FB-*` `questions/foundation-backend.md`, `FE-*` `questions/foundation-frontend.md`, `D-*` / `P-*`
`questions/devops.md`, `M1-*` `questions/m1-components.md`, `M2-*` `questions/m2-processes.md`, `M3-*`
`questions/m3-skus.md`, `Q-* (4A)` `questions/m4a-quotes.md`, `Q-* (4B)` `questions/m4b-balancer.md`. Story ids
(C-, P-, S-, B-) refer to `requirements/`.

## 1. Blocking for go-live

External contracts, credentials and values needed before dev / prod can run.

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-1 | SAP export: file format, columns, schedule, owner of the job | Two UTF-8 CSV files with a header, `components.csv` and `stock.csv`, under `s3://{bucket}/{prefix}/{yyyy-MM-dd}/` (columns in `modules/1-components.md`); sync at 02:00 UTC reads the folder of the run's UTC date; a missing file fails the run and Hangfire retries | JSON / fixed-width / one file; SAP's own column names; read the newest folder not later than today | I-1, M1-1, M1-2 | |
| OQ-2 | Are the SAP files full snapshots or deltas? | Full: an ERP id missing from `components.csv` becomes `Inactive` (a file with no valid row fails instead); a component / factory missing from `stock.csv` keeps its row (manual OOS, override) with stock 0; duplicate rows per component, factory and location are summed | Delta with an explicit status; delete the row or keep the last stock; reject duplicates | M1-3, M1-4, M1-5 | |
| OQ-3 | Who owns the SAP export bucket, and which IAM principal uploads? | This project creates `tg-factory-catalog-<env>`, prefix `sap-export`; the SAP job gets `PutObject` through `sap_export_writer_arns` (empty today) | Read an existing bucket of the SAP / data team | D-10, P-6 | |
| OQ-4 | Master catalog sync mechanism | Daily pull (`0 3 * * *` UTC) plus "Sync now" on the SKU list; the event consumer waits until the master catalog team publishes events | Migration + event-based updates (the discovery Q's preferred option); hourly job | I-4, M3-1 | |
| OQ-5 | Master catalog API contract | `GET v1/skus?page&pageSize&changedSince` → `{ items, hasMore }`, item = the requirement SKU schema incl. dropshipper products, Basic auth; each item is the full SKU (aliases / components missing from it are removed); `changedSince` = start of the last successful run; retired SKUs arrive with `status: "retired"` and are never deleted | Full export file on S3; cursor paging; OAuth client credentials; field deltas; tombstones / deleted list | M3-2, M3-3, M3-4, M3-5 | |
| OQ-6 | Is the "master catalog" the `tg-catalog` service, and with which user? | Yes: `tg-catalog-develop.dev…`, `tg-catalog-release.stg…`, `catalog.tenengroup.com` (confirmed by OCS `CATALOG_URL`); the Balancer reads the component breakdown there with `POST auth/local` → JWT, as OCS does; the user is still a placeholder (`REPLACE_WITH_MASTER_CATALOG_USERNAME`; OCS uses `api-order-service` / `order_service`) | A dedicated machine user per environment | D-12, P-3 | **Partly answered from the OCS code (2026-11-25):** URLs confirmed; `tg-catalog` signs in with `POST auth/local` (JWT), not Basic — the SKU sync of OQ-5 assumes Basic and an endpoint not seen anywhere. Still open: the machine user, and the real SKU list endpoint |
| OQ-7 | OCS → balancer paid-order message (with CBD) | SQS fed by `order-core-service-<env>-domain-events`, filter `event_type = paid_order_created_v2`; the OCS v2 order is mapped: order id = `om_id`, one item per product item (quantity 1), item id = product item `om_id`, site SKU, destination `shipping_address.country_code`, promised date = earliest factory `estimated_delivery_date`, paid at = `created_date`, `fulfillment_type` `virtual` / `dropshipper` / other = Factory; CBD read from the master catalog `component-breakdown/v2/{site}` with the item's `user_selections`; SLA date and event flag not sent | OCS adds CBD, SLA and event flag to the event; one balancer call from OCS (`POST /v1/allocations`) | I-7, Q-2 (4B), P-5, D-11 | **Answered from the OCS code (2026-11-25), re-implemented:** event `paid_order_created_v2`, envelope `{ EventType, DataJson }`, attribute `event_type`; no CBD, no quantity, no SLA / event flag, no paid-at (see `questions/external-systems-findings.md`). Still open: (a) is `production_due_date` / `pickup_due_date` the SLA (ship-by) date? (b) how is an event order recognised (only `event_level` exists)? (c) the event fires before verification — should a later hold / cancel undo the allocation? |
| OQ-8 | Shipment service routes, costs and timeframes | Daily pull of `POST v1/shippingRoute/getByCriteria` (`statuses: [0]`, pages of 500), Basic user `admin` + the shipment service `API_SERVICE_USER_PASSWORD`; only routes of `Balancer:ShippingServiceLevel` (Basic); per (facility, country) a `priority` route wins, then the cheapest; `packingFacilityIds: null` = every facility; transit min = `shippingTimeFrame`, max = + `packingTimeFrame`, counted on the route's `shippingWorkingDays` | Subscribe to `shipment-service-<env>-shipping-routes-events` (off by default, the daily cost import does not publish); live `getOptimal` per order; service level per order from the OCS `carrier_service_type_id` | I-6, Q-5 (4B), D-13 | **Answered from the shipment service code (2026-11-25), re-implemented:** enums are numbers, time frames are working days, the service itself picks priority then cheapest per service level (see `questions/external-systems-findings.md`). Still open: should express / expedited orders use their own service level (needs the carrier service type → method map)? |
| OQ-9 | TG Supplier sent / reproduced events | SQS fed by `supplier-<env>-events`, types `TenenGroup.Supplier.Events.ItemSent` (`{ OrderId, ItemId, SupplierId, SupplierType }`, sent time = SQS `SentTimestamp`) and `…ItemProductionStatusChanged` (`ProductionStatus = "Reproduction"` marks the allocation; the next `ItemSent` is the reproduction); SQS message id is the idempotency key; production log empty | TG Supplier adds `sentAt`, the quote id and the production log to `ItemSent` | I-8, Q-14 (4B), P-4, D-11 | **Answered from the TG Supplier code (2026-11-25), re-implemented** (see `questions/external-systems-findings.md`). Still open: TG Supplier has no production log / machine type in any event, so OQ-41 (actual cost) and OQ-72 (production conflict) have no data until TG Supplier adds it — ask the TG Supplier team? |
| OQ-10 | Transfers (B-23): how does TG Supplier move the item to the new supplier page? | Unchanged: Balancer publishes `balancer.allocation.transferred`; production pages from `Balancer:ProductionPages:{factoryId}` plus pages already used | Call the existing TG Supplier API `Transfer/transferitems` (`items, newSupId, newPage`), which already publishes `ItemSupplierChanged`; read pages (`TABLE_SUP_PAGE`) from TG Supplier | Q-16 (4B), Q-17 (4B) | **Partly answered (2026-11-25):** TG Supplier already has a transfer API and pages are `TABLE_SUP_PAGE` rows, but nothing in TG Supplier consumes our event (see `questions/external-systems-findings.md`). Decision needed: keep the event (TG Supplier work) or call their API (their auth is a JSON `Auth-Token` header) |
| OQ-11 | TG Supplier replacement screen contract (C-16) | Unchanged: `GET /v1/components/{erpId}/alternatives?factoryId=` and `POST /v1/components/replacements` | — | M1-18, M1-19 | **Answered (2026-11-25):** TG Supplier has no replacement / alternative screen or code (see `questions/external-systems-findings.md`); the screen is new TG Supplier work on top of these endpoints |
| OQ-12 | Legacy balancer integration until Module 4 retires it | Unchanged: catalog exposes the eligibility check and `GET /v1/legacy-balancer/country-sku-lists`; shadow decisions go to Balancer `POST /v1/legacy-decisions` | Push lists through TG Supplier `CountryBalancer/saveSkus` | I-10, Q-17 (4A), 4B "Requests to orchestrator" | **Answered from the TG Supplier code (2026-11-25):** lists live in TG Supplier (`CountryBalanceSku`, edited via `saveSkus`), nothing is pulled; country → factory: Israel TGI 120 / TGN 130, others master of [702, 126, 218]; shadow hook point `WorkloadBalancer.cs` after the country result (see `questions/external-systems-findings.md`). Decision needed: TG Supplier pulls our lists, or we push via `saveSkus` |
| OQ-13 | Allocation / produced events for BI: transport and shape | Two SNS topics: `…-balancer-allocation-decided` (`balancer.allocation.decided`, Module 4 section D shape; `balancer.allocation.transferred` on the same topic) and `…-balancer-quote-produced` | One `…-balancer-events` topic, consumers filter on `EventType` | I-9, D-3 | |
| OQ-14 | Azure AD app registrations | Placeholders per environment; local dev reuses the shipment service tenant / client ids (UI port 8080); both APIs share one API registration and scope | Dedicated registrations (required before any deployed environment); one scope per API | A-6, P-2, FB-23, FE-22, D-14 | |
| OQ-15 | AD groups per persona | `FC-FactoryManagers`, `FC-ShiftManagers` (same rights as factory manager), `FC-SupplyChainManagers`, `FC-CatalogAdmins`, `FC-ProductionFloor`, `FC-OperationsManagers`, `FC-Finance`, plus `BO_Administrator`; mappings and the "Factory Catalog" app entry still to be added to the CDN `GroupPermissions` / `BackOfficeSystems` files | Reuse existing groups; shift managers inside `FC-FactoryManagers` | A-7, FB-2, FB-12, P-9 | |
| OQ-16 | Real factory list: ids, names, countries | Factory ids are TG Supplier supplier ids; packing facility via `Balancer:PackingFacilities` (`110` → `1` Kiryat Gat, `125` → `7` Thailand, from the shipment service `supplier_facility` table) | Read the mapping from the shipment service; a `packing_facility_id` column on `factory` | A-9, FB-1, M1-24, Q-6 (4B) | **Answered from the shipment service / TG Supplier code (2026-11-25), re-implemented:** factory ids ≠ facility ids (see `questions/external-systems-findings.md`). Still open: **`315` exists in none of the repos** — which supplier is it? Real names / countries (TG Supplier: TGI 120 and TGN 130 Israel, TGO 126 Thailand, TGH 702 Hungary) |
| OQ-17 | Who binds users to their own factory? | Unchanged: `user_factory` table (email → factory ids), no UI | Seed from TG Supplier `TABLE_SUP_USERS.SupplierId` | A-8, FB-19 | **Partly answered (2026-11-25):** TG Supplier users have their own logins (no AD), one `SupplierId` each (see `questions/external-systems-findings.md`); production floor users need AD accounts (OQ-97) |
| OQ-18 | Platform resources and secrets | Names follow the shipment service convention (KMS alias, namespaces `factory-catalog-{dev,stg,prod}`, IRSA role, Jenkins job, Slack channel, Docker Hub repos `tenen/factory-catalog-{main,balancer,ui}`, GitHub repo); 7 KMS ciphertexts per environment and the webhook token are placeholders for the platform team | — | P-1, P-7, P-8 | |

## 2. Business rules likely to cause rework

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-19 | Component unit cost source (C-10, business stakeholder Q) | View only: the SAP sync stores the cost when `components.csv` carries it (source `SAP`); no edit endpoint, although `ComponentsEditCost` is granted to the supply chain manager | SAP purchase terms; import from the "Pricing Tool" sheet; supply chain manager edits | I-2, M1-7, FB-4 | |
| OQ-20 | Missing component or labor cost in a quote | Counts as 0; the quote carries `COMPONENT_COST_MISSING` / `LABOR_COST_MISSING` and stays producible | Factory not producible while a cost is missing | Q-8 (4A) | |
| OQ-21 | Currencies | Unchanged: quote currency `Quotes:Currency` = USD; other currencies summed as is and flagged `CURRENCY_MISMATCH` | Convert with the existing `exchange-rate-service` (`GET /api/ExchangeRate/GetExchangeRate?from&to`, used by OCS and the shipment service) at quote time | M2-10, M2-12, Q-8 (4A), Q-26 (4B) | **Premise corrected (2026-11-25):** an FX source exists, OCS orders are multi-currency, shipping costs are USD (see `questions/external-systems-findings.md`). Decision needed before re-implementing: convert at quote time (rate frozen in the quote) and drop `CURRENCY_MISMATCH` unless a rate is missing? |
| OQ-22 | Capacity signal formula (0–100) and partial capacity (B-08) | Minimum of 100 / 50 per consumed component with stock `ok` / `low` (the alternative's status when used) and today's remaining % of each mapped process; the balancer scales the quoted share by it, 0 excludes the factory; event reservations subtract from it for regular orders | Process capacity only; weighted average; stock coverage (stock ÷ required quantity) | Q-6 (4A), Q-9 (4B) | |
| OQ-23 | "Used today" of a process (P-14): what counts | One per `Produced` quote (order item, reproductions included), not × item quantity, via the SKU's current process mapping; `Selected` items are not subtracted; remaining % floored and clamped 0–100; without max per day remaining % = the set capacity % | × quantity or × process units; subtract `Selected`; use the reported production log; round half up | M2-2, M2-3, M2-5, M2-6, Q-7 (4A) | |
| OQ-24 | What is "today" for capacity? | The UTC day | The factory's local day (needs a time zone per factory) | M2-4, Q-7 (4A) | |
| OQ-25 | Process with capacity > 0 but nothing left today | Factory stays producible with `remainingPct = 0` and capacity signal 0; the balancer decides | Reason `PROCESS_FULL`, not producible | Q-27 (4A) | |
| OQ-26 | Packing cost model (P-11) | Flat per factory: the `PACKING` process's labor cost per unit | Size / complexity tiers; a cost per packing type on the SKU | M2-1 | **Checked (2026-11-25):** TG Supplier stores no packing or labor cost, so it cannot be the source; still open |
| OQ-27 | SKU with no `PACKING` mapping | Packing priced only from the mapping (quantity × factory rate); without it packing = 0 and the quote carries `PACKING_NOT_MAPPED` | Add packing implicitly, quantity 1 per piece, for every SKU | Q-9 (4A) | |
| OQ-28 | Which components a quote checks, and in what quantity | Only the CBD (B-02); CBD quantity is per piece (required = `cbd.quantity × quantity`). The legacy eligibility check additionally adds the SKU's Static components (S-17) | Add Static components missing from the CBD; fail the quote when one is missing; CBD quantity covers the whole item | Q-4 (4A), Q-5 (4A) | |
| OQ-29 | Stock status edge cases | No critical level set → never `Low`, never OOS conflict; no `component_factory` row → no status in that factory (shown "—", manager can still flag OOS); a CBD component `Inactive` in SAP is judged by stock like any other | Missing level = 0; missing row = OOS; reason `COMPONENT_INACTIVE` | M1-9, M1-10, Q-22 (4A) | |
| OQ-30 | "For balancer" alternatives in quotes | Used when the original is OOS: must be active and not OOS in that factory; prefer `ok` over `low`, then lowest ERP id; priced with the alternative's cost; one-way, not chained; stock not compared with the required quantity | Price with the original's cost; require stock ≥ required; follow alternatives of alternatives | Q-11 (4A) | |
| OQ-31 | CBD component unknown to the catalog (B-02) | `200`, every factory not producible with `UNKNOWN_COMPONENT`, line `stockStatus: unknown`; the set is stored for the decision log | `422` with the unknown ERP ids, nothing stored | Q-1 (4A) | |
| OQ-32 | Mapped process that the factory does not run | Factory not producible, reason `PROCESS_NOT_AVAILABLE` (new code); the legacy check ignores "runs here" and only looks at 0% | Ignore "runs here" in quotes; reason `PROCESS_OFFLINE` | Q-10 (4A) | |
| OQ-33 | Invalid, dropshipper, unknown and retired SKUs | Invalid (S-11) → every factory `SKU_INVALID`; dropshipper → `DROPSHIPPER`, no lines; unknown → `404`; retired → still quoted; the legacy lists and legacy check ignore validation status | `422` for all; route invalid SKUs with a warning; `SKU_RETIRED`; mark invalid SKUs not producible in legacy lists | Q-2 (4A), Q-3 (4A), Q-28 (4A) | |
| OQ-34 | Share rebalancing when a manager overrides (S-14) | The rest (100 − value) is split over the other producible factories in proportion to their current effective shares and stored (`Rebalanced`); another manager's override is rescaled but keeps its author; reset clears own override; none left → defaults | Store only the editor's override and normalise at read time; reject totals ≠ 100 | M3-14 | |
| OQ-35 | Effective share when a factory is not producible (S-15) | Base shares redistributed over producible factories (same function as quotes), stored values untouched; if all remaining have 0 → equal split | Store redistributed values; leave all at 0 | M3-15, Q-25 (4A) | |
| OQ-36 | Share precision and global share | 0–100 with ≤ 2 decimals, rounding remainder to the largest share; global share lists every active factory exactly once, total 100 | Whole percents; missing factories mean 0 | M3-16, M3-17 | |
| OQ-37 | Routing score and weights | `total × (1 − ShareWeight × shareDeficit/100) + LatePenaltyPerDay × daysLate` (+ success-rate penalty); defaults 0.1 / $5 per day / split penalty $3; versioned; SLA misses rank before score | Pure lexicographic rules; normalised multi-criteria score | Q-8 (4B) | |
| OQ-38 | "Meets the delivery promise" | `today + production days` (calendar) `+ max transit` counted on the route's shipping working days `≤ promisedDeliveryDate` | Also use the shipment service country holidays, weekends and facility working days | Q-7 (4B) | **Partly answered (2026-11-25), re-implemented for route working days:** the shipment service has holiday / weekend / facility calendars (see `questions/external-systems-findings.md`). Decision needed: read them too? |
| OQ-39 | Fallback when no factory can be chosen (B-11) | Shares of producible quotes, else `Balancer:FallbackShares`, else equal split; largest deficit wins; Error log `BALANCER FALLBACK` is the alert hook (Datadog monitor still to create) | SNS alert topic; email | Q-10 (4B) | |
| OQ-40 | Target share for the share dashboard (B-14) | Average quoted `sharePct` of the routed items' candidates in the period | Global factory share from the catalog | Q-22 (4B) | |
| OQ-41 | DIM actual cost (B-12, source to confirm) | Actual line cost = `productionLog` quantity × quoted unit cost; actual labor summed per allocation; material and packing stay quoted | Actual cost from SAP / finance; actual = quoted until a source exists | Q-12 (4B) | **Checked (2026-11-25):** TG Supplier events carry no production log, so actual = quoted for now (the code already falls back); still open |
| OQ-42 | "On time" (B-15) without delivery data | Unchanged: sent on or before the ship-by date | Consume `ShippingService.Events.ShipmentDelivered` (carrier, `DeliveryStatus`, `EstimatedDeliveryDate`) from `shipment-service-<env>-domain-events` | Q-13 (4B) | **Premise corrected (2026-11-25):** delivery events exist (see `questions/external-systems-findings.md`). Decision needed: measure on time by real delivery? |
| OQ-43 | Packing facility | The producing factory's facility from `Balancer:PackingFacilities` | Resolve like the shipment service `FacilityResolver` (mixed-country orders packed in Kiryat Gat with a transfer) | Q-11 (4B) | **Answered (2026-11-25), re-implemented:** packing is per fulfillment in the shipment service and may differ from the producing factory (see `questions/external-systems-findings.md`). Still open: should the balancer price the extra transfer of split orders? |

## 3. Per module

### Components

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-44 | `countryOfOrigin` / `hsCode` in Phase 1 | Stored and shown when the export has them | Leave out; make them required | I-3 | |
| OQ-45 | History for daily stock quantities | Not logged: only master-data changes, new components and deactivation | Log every stock change; log only status transitions | M1-6 | |
| OQ-46 | Inactive components | Listed with an "Inactive in SAP" tag, never "action required", left out of stock levels; cannot be added as an alternative, existing links stay and are tagged | Hide them; remove their alternative links | M1-11 | |
| OQ-47 | Default use flags of a new alternative | Both on (for balancer, for S.C. planner) | Both off; ask in the add form | M1-15 | |
| OQ-48 | Editing the note of an active OOS flag | Changes the note only; "set by" and date stay | Update author and date on every edit | M1-17 | |
| OQ-49 | Who sees component unit cost | Callers with `ComponentsViewCost`; others get `null`, column hidden | Everyone | M1-8 | |
| OQ-50 | Component `supplier` (in mockup data, not in the schema) | Not stored | Add when SAP exports it | FB-18 | |

### Process capacity

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-51 | No max per day set | "Today" not shown ("Maximum daily output is not set") | Treat as unlimited; require max per day before "Runs here" | M2-7 | |
| OQ-52 | Capacity reason / recovery | Reason logged inside the history value (`80% (reason: …)`) and as its own fields; cleared when capacity returns to 100% | `note` column on history; keep the last note | M2-8, M2-9 | |
| OQ-53 | Labor cost effective date | Set by the system to the change day; only one rate kept, quotes use the current rate whatever the date | Editable date; dated cost rows used by date | M2-11, Q-26 (4A) | |
| OQ-54 | Process ID on create | Optional, derived from the name when empty, upper-case letters / digits / `_ - .`, not editable later | Always generated (`PRC-001`); always typed | M2-13 | |
| OQ-55 | Can `PACKING` be deactivated or made a machine process? | No (422), every quote needs it | No restriction | M2-14 | |
| OQ-56 | Process type changed machine → work | Machine types of its factories cleared (logged) | Reject while machine types exist | M2-15 | |
| OQ-57 | Values while "Runs here" is off | Kept (machine type, cost, max, capacity); capacity not editable | Clear the row | M2-16 | |
| OQ-58 | Format of the TG Supplier / "Pricing Tool" process import (P-04) | Own two-sheet workbook (`Processes`, optional `Factories`), see module spec | Adapt to the real export once a sample exists | M2-17 | |
| OQ-59 | Catalog admin importing per-factory setup (owned by factory managers) | Allowed for the initial load (`ProcessesEdit`, history source `Import`) | Process sheet only; own-factory check per row | M2-18 | |
| OQ-60 | Imports with errors / empty cells | Whole import blocked while any row has an error (all imports); on re-import an empty status keeps the value, other empty cells clear it | Import valid rows, skip the rest; empty = keep everywhere | M2-19, M2-20, FE-13 | |
| OQ-61 | Inactive processes and factories in capacity views | Hidden (inactive processes stay in the Processes list) | Show greyed | M2-22 | |
| OQ-62 | Do factory / shift managers see the labor cost they edit? | Yes (`ProcessesViewCost`) | Admin and finance only | FB-7 | |

### SKUs

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-63 | Manufactured SKU with no process mapping or no components | Valid: only mapped items are checked; the S-10 migration fills processes later | Invalid until at least one process is mapped | M3-10 | |
| OQ-64 | A bad SKU in a sync page; "failures alert the admin" | Skipped, logged, counted in `failed`, run completes; transport errors fail the run; alert = failed status on the badge, no e-mail | Fail the whole run; e-mail / Slack alert | M3-6 | |
| OQ-65 | Alias already owned by another SKU | Incoming SKU fails (counted) | Move the alias; keep both | M3-7 | |
| OQ-66 | SKU becomes a dropshipper product | Processes, producible flags and overrides removed (history written) | Keep them hidden | M3-8 | |
| OQ-67 | Dropshipper record fields (S-19) | `dropshipper_id`, `dropshipper_name`, total cost amount + currency on `sku` | Separate dropshipper table | FB-25 | |
| OQ-68 | When is SKU validation (S-11) recomputed? | After every master catalog run (all SKUs) and every process import (imported SKUs); a component deactivated by SAP shows at the next master catalog run | Also after the SAP sync; on read | M3-9 | |
| OQ-69 | "Only approved files can be imported" (S-10) | Workbook needs an `Approval` sheet with `Approved by` / `Approved on`, else nothing is saved; approver logged | Approval queue in the UI; signed file; second-admin approval | M3-11 | |
| OQ-70 | Process import: replace or add? | Replace: each SKU in the file gets exactly the file's processes; others untouched | Upsert only; explicit delete column | M3-12 | |
| OQ-71 | Producible flags | No `sku_factory` row = not producible, first tick creates it; import accepts Yes/No, Y/N, true/false, 1/0; rows of factories the caller cannot edit are row errors | Producible by default; ignore other factories' rows | M3-19, M3-20 | |
| OQ-72 | Production conflict (S-20) | Only the latest produced quote per SKU and factory counts (tag clears when a later log matches); unknown machine type on either side is not a mismatch | Every log of the last N days; unknown = mismatch | M3-21, M3-22 | |
| OQ-73 | Where the global-share Settings page lives | `/factory-share/settings`, opened from the "Default balance" card (admin only), not in the menu | A Settings menu entry; inline edit of the default row | M3-18, FE-25 | |
| OQ-74 | Who sees SKU and Factory share pages | Everyone with `Skus.View`; edit controls greyed for others | Hide factory share from production floor users | M3-25 | |

### Quotes / legacy balancer (catalog side)

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-75 | Inactive factories in quotes | Only active factories returned | Return inactive ones with a reason | Q-12 (4A) | |
| OQ-76 | Quote validity (`validUntil`) | 15 min (configurable), informational only: `select` accepts an expired quote | Reject `select` after `validUntil` (409) | Q-14 (4A) | |
| OQ-77 | `select` conflict rules | 409 for a non-producible quote, a `Produced` / `Transferred` quote, or when another quote of the same set is selected / produced; repeat = no-op; other sets of the item not checked; shadow sets selectable | Allow manual override of non-producible; one selected quote per order item across sets; forbid selecting shadow | Q-15 (4A) | |
| OQ-78 | Legacy eligibility with unknown data | Unknown SKU → only the CBD checked; unknown components never skip a factory (returned in `unknownComponents`); unknown factory ids → 422 | 404 for unknown SKU; unknown component = OOS; ignore unknown factories | Q-19 (4A) | |
| OQ-79 | Legacy skip log volume | Same cause logged once per UTC day | Every call; once per order forever | Q-20 (4A) | |
| OQ-80 | SKU identifier accepted in quotes | UUID, then alias, then Site SKU when unambiguous (else 422), case-insensitive | UUID and alias only | Q-23 (4A) | |

### Balancer

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-81 | Quotes in shadow mode | One `shadow` quote request per item, stored with mode `Shadow`, never selected or published | Use `estimate` (nothing stored, no audit link) | Q-3 (4B) | |
| OQ-82 | Switching shadow ↔ live | `PUT /v1/settings` (`BalancerEditReservations`), initial `Balancer:Mode = Shadow`; each switch is a new settings version kept on decisions | Deployment setting only; catalog feature toggle | Q-4 (4B) | |
| OQ-83 | Transfer re-quote (B-23) | Preview via catalog `estimate`; confirm requests a `live` quote (new set `…-R{n}`), marks the old quote transferred, selects the new one; non-producible target reported per item and not moved | Reuse the original set's quote for the target | Q-18 (4B) | |
| OQ-84 | Daily plan input (B-22) | Local live allocations `Allocated` for the factory by due date (overdue on today), grouped by the selected quote's process lines; capacity = `maxPerDay × capacityPct`, `today.used` for today | Query catalog quotes (`Selected`) on each page load | Q-21 (4B) | |
| OQ-85 | Routing dashboard during shadow mode | KPIs count live and fallback allocations only; shadow only in the shadow card | Count shadow as "would-be" routing | Q-23 (4B) | |
| OQ-86 | Estimated savings in the shadow report | Σ (legacy total − new total) over disagreeing items | Include agreeing items; ETA-weighted | Q-24 (4B) | |
| OQ-87 | UI for DIM cost and capacity reservations (not in mockup) | DIM cost = export from the routing dashboard (`BalancerViewDimCost`); reservations via API only | Dedicated pages | Q-25 (4B) | |
| OQ-88 | Permissions of Balancer pages | Routing dashboard → `BalancerViewDashboards`; decision log / transfers → `BalancerView`; daily plan → new `BalancerViewDailyPlan` (factory / shift manager, own factory); transfer → new `BalancerTransfer` (managers own factory, operations any) | Everything behind `BalancerView` | FB-6, FE-21 | |

### UI

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-89 | "X of Y" footer on server lists | Shows `from–to of filtered total` + "Clear N filters"; unfiltered total not shown | Add the unfiltered count | FE-12 | |
| OQ-90 | When header filters apply | Text / number 400 ms after typing; checkboxes at once | Apply button | FE-27 | |
| OQ-91 | Column picker | Always-visible columns + up to 8 optional, saved on change, definition order | Drag-to-reorder | FE-28 | |
| OQ-92 | Row selection across pages | Kept while paging, cleared on search / filter change | Page-only selection | FE-29 | |
| OQ-93 | Visual style | BO theme (Open Sans, BO top bar) with mockup table density, chips, tiles; Roboto Mono for ids; title in content area | Roboto / plain MUI theme | FE-32 | |
| OQ-94 | Name shown for "Set by" / "Last changed by" | Inconsistent today: OOS flags store the display name (e-mail fallback); capacity shows the e-mail | Display name everywhere (needs a user directory); e-mail everywhere | M1-16, M2-24 | |
| OQ-95 | Global critical level input | Saved on Enter or blur | Explicit Save button (prototype) | M1-22 | |
| OQ-96 | Component stock levels layout | ERP ID and Name in separate columns; no status tiles | One combined cell; tiles with counts | M1-23 | |

### Architecture and permissions

| # | Question | Implemented (recommended) | Alternatives | Source | Your answer |
|---|---|---|---|---|---|
| OQ-97 | Where the catalog lives (discovery Q; AD roles decided Nov 22) | Standalone service + back-office style UI, shipment service stack; TG Supplier production floor users need an AD account | Inside TG Supplier (users not in AD); inside the BO monolith | A-1 | **Checked (2026-11-25):** confirmed that TG Supplier has its own users, not AD |
| OQ-98 | Balancer as a separate service | `Tenengroup.Balancer.Api` in the same repo, calls the catalog over HTTP | One service with a balancer module; separate repo | A-2 | |
| OQ-99 | One database or two | One `factory_catalog` database, schemas `catalog`, `balancer`, `hangfire`, `hangfire_balancer`; no cross-schema reads; schemas created by migrations / Hangfire | Database per service | A-5, Q-1 (4B), D-8 | |
| OQ-100 | Jira project key | None; plain commit messages | `FC-`, `FAST-` | A-4 | |
| OQ-101 | Database login of the running APIs | `migrations_user` for migrations and runtime (as the shipment service) | Least-privilege `app_user` with DML grants only | D-7 | |
| OQ-102 | Public ingress | None: all hosts internal (cluster + VPN) | `remote-*` ingress for external callers | D-15 | |
| OQ-103 | RDS size | `db.t3.small`, 20 GiB (prod 80 GiB, Multi-AZ, 35-day backups), no replica | Share an existing instance; other class | D-28 | |
| OQ-104 | Production floor user and the Balancer | `BalancerView` granted (overview table gives every persona "View") | Hidden | FB-5 | |
| OQ-105 | Who reads sync status / history | Any authenticated user; `SyncMonitor` for run details, `SyncTrigger` for "Sync now" | Catalog admin only | FB-14 | |
| OQ-106 | Who may call quote writes (`POST /v1/quotes`, lifecycle, eligibility) | Service accounts (Basic) only; JWT users get 403 even with enforcement off | A permission for operations; allow admins | Q-18 (4A) | |
| OQ-107 | Who may switch All factories / By factory in capacity | UI role check (operations manager, BO administrator); API returns both to anyone with `ProcessesView` | A `ProcessesViewCapacityByFactory` permission | M2-21 | |
| OQ-108 | User with several roles; admins and own factory | Union of all roles' grants; `BO_Administrator` and disabled enforcement may edit any factory | Highest-priority role only; restrict admins to their `factoryIds` | FE-16, FE-18, FB-8 | |

## 4. Technical decisions (FYI, no answer needed)

### Already decided by the business (implemented, not re-asked)

- SKU key = master catalog UUID, legacy factory SKUs are read-only aliases (decision Nov 23) — I-5.
- Factory managers edit runs here / machine type / labor cost for their own factory via `ProcessesEditFactorySetup`; catalog admin keeps specifications (decision Nov 23) — FB-3, FE-20. Only the admin's initial import is open (OQ-59).
- No step column on SKU processes (decision Nov 23) — M3-13.
- "Sync now" only on the SKU list, so not on Components (decision Nov 23) — M1-14.
- Production feedback: `production_event` dropped; `quote` stores status, `produced_at`, `production_log`, `reproduction_of_quote_id` (decision Nov 24) — FB-9.
- Reproduction = new produced quote `{quoteId}-RP{n}` via `POST /v1/quotes/{id}/produced` with `reproductionOfQuoteId` (decision Nov 24) — Q-16 (4A), Q-15 (4B).

### Backend

- Repo `C:\shakuro\factory-catalog`, namespaces `Tenengroup.FactoryCatalog.*` / `Tenengroup.Balancer.*` — A-3.
- Field history via own `history_entry` table, not Audit.NET — A-10; alternatives / replacements audit there too — FB-17.
- `quote_key` unique; re-quotes `…-R2`, estimates `EST-{requestId}`, order / item ids ≤ 90 chars — FB-10, Q-13 (4A).
- `GET /v1/feature-toggle` uses the shipment service `{ data: [...] }` envelope — FB-11, FE-6.
- Enums serialised as C# member names; requests case-insensitive; reason codes `UPPER_SNAKE` — FB-13, Q-21 (4A).
- Stale `Running` sync older than 6 h is failed by the next run — FB-15.
- No FK from `sku_component` / `sku_process` to components / processes (unknown → `Invalid`) — FB-16.
- "Contains" filters use `lower() LIKE` with b-tree indexes; `pg_trgm` later — FB-20.
- Hangfire dashboard behind Basic auth — FB-21; API needs PostgreSQL to start — FB-22.
- One shared `Tenengroup.Infrastructure.Common` project — FB-24.
- Sync trigger `POST /v1/sync/{syncType}/run` (`sap-components` in the request, `master-catalog-skus` in the background), 409 while running, status `null` before the first run — FE-3, FE-4, FE-5, M1-12, M1-13, M3-24.
- Components list filtered / paged in memory; filter options endpoint; ERP id as one path segment — M1-20, M1-21, M1-25.
- "SKUs using this" returned whole in `GET /v1/processes/{id}` — M2-23; batch SKU identifier resolver for imports — M3-23.
- Idempotent quote replay by `requestId` returns the stored set; different order item → 409 — Q-24 (4A).
- Balancer gets own factory from catalog `/v1/user/me` (forwarded token) and the permission toggle from catalog `/v1/feature-toggle` (cached 10 min, fail closed) — Q-19 (4B), Q-20 (4B).
- `permissions.enable_all` off = every check passes (incl. own factory); UI fails closed when the toggle cannot be read — FE-7, FB-8.

### Frontend

- List filter shape (`values` or `operator` + `value` / `valueTo`), 1-based paging, page sizes 25 / 50 / 100 — FE-1, FE-2.
- Export file name from `Content-Disposition`; `columns` = visible keys in order — FE-8, FE-9.
- No dev proxy, APIs allow the UI origin; factory ids are strings — FE-10, FE-11.
- Balancer lists read list settings / toggles from the catalog API — FE-14.
- Roles from `GET /v1/user/me`; CASL `can("access", "<Permission>")`; "No access" page — FE-15, FE-17, FE-19.
- Own `AppLayout` / grouped nav, routes `/components`, `/processes`, `/skus`, `/component-stock-levels`, `/process-capacity`, `/factory-share`, `/balancer/*` — FE-23, FE-24.
- Own `DataTable` instead of `bo-frontend` `List`; tabs in `?tab=`; Redux + MUI Snackbar — FE-26, FE-30, FE-31.
- Dependencies left out vs the shipment UI; `tsconfig` includes `src/**` only — FE-33, FE-34.

### DevOps

- Balancer setting names, `migrate` command, `/ping`, test project assumed — D-1, D-2.
- One Helm release, three deployments, per-env values as Terraform lookups, secrets rendered from KMS into env vars — D-4, D-5, D-6.
- IRSA instead of static AWS keys — D-9.
- `/ping` for liveness and readiness; UI container port 80 — D-16, D-17.
- Datadog tracer latest, `DD_SERVICE` per API; Jenkins agent image reused; build + tests inside Docker builds — D-18, D-19, D-20.
- Balancer image tag / Dockerfile layout / `.dockerignore` per Dockerfile — D-21, D-22, D-23.
- LocalStack locally; master catalog / shipment service not in compose — D-24, D-25.
- Feature-branch environments kept as in the reference; chart selector quirk copied unchanged — D-26, D-27.
