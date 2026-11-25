# Module 4B (Balancer service and UI) — questions and assumptions

> **Requirements:** Phase overview v4; Module 4: Balancer v4

Each row was implemented with the recommended variant. Design: `spec/modules/4b-balancer.md`.

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| Q-1 | Where does the Balancer keep its data? | Same PostgreSQL database as the catalog, own schema `balancer` and Hangfire schema `hangfire_balancer`; no cross-schema reads — everything from the catalog goes through its API | Separate database; read catalog tables directly |
| Q-2 | Shape of the OCS "paid order item" SQS message | **Checked against OCS (2026-11-25):** event `paid_order_created_v2` (attribute `event_type`), the OCS v2 order mapped by `OcsPaidOrderMapper` (see module spec "Events"); CBD from the master catalog `component-breakdown/v2/{site}` | OCS adds CBD / SLA / event flag; OCS calls `POST /v1/allocations` |
| Q-3 | One quote request per order item, also in shadow mode? | Yes, with `mode: shadow`; shadow decisions are stored with mode `Shadow`, never selected or published | Use `estimate` in shadow (nothing stored in the catalog, no audit link) |
| Q-4 | What makes the balancer switch between shadow and live? | `balancer_settings.mode`, changed with `PUT /v1/settings` (permission `BalancerEditReservations`), initial value `Balancer:Mode` = `Shadow`. A switch creates a new settings version; decisions keep the version they used | A deployment setting only; a feature toggle in the catalog |
| Q-5 | Shipping route source (section C is owned by the shipment service) | **Checked against the shipment service (2026-11-25):** daily pull of `getByCriteria` (`statuses: [0]`), routes of `Balancer:ShippingServiceLevel`, priority then cheapest per (facility, country), null facilities = all, transit in working days on `shippingWorkingDays` | Route events topic; `getOptimal` per order |
| Q-6 | Catalog factory id vs shipment service packing facility id | **Checked (2026-11-25):** not equal — `Balancer:PackingFacilities` maps them (`110` → `1`, `125` → `7`); `315` unknown | Read `supplier_facility` from the shipment service |
| Q-7 | How "meets the delivery promise" is evaluated | `etaTo = today + production days + max transit ≤ promisedDeliveryDate`; production days = average order-to-sent days of the factory over 90 days, else `DefaultProductionDays` (3) | Working-day calendars per factory and carrier |
| Q-8 | Scoring formula and weights (versioned config) | `total × (1 − ShareWeight × shareDeficit/100) + LatePenaltyPerDay × daysLate (+ success-rate penalty)`, defaults 0.1 / $5 per day / split penalty $3; SLA misses rank before score | Pure lexicographic rules; a weighted multi-criteria score with normalised terms |
| Q-9 | Partial capacity (B-08) — which number? | The catalog's `capacitySignalPct` (min of stock and today's remaining process %) scales the quoted share; 0 excludes the factory; event reservations subtract from it for regular orders | Re-read process capacity per SKU from the catalog |
| Q-10 | Fallback share source (B-11) and alerting | Producible quotes' shares, else `Balancer:FallbackShares`, else equal split of active factories; the factory with the largest deficit wins; Error log `BALANCER FALLBACK` is the alert hook (Datadog monitor) | SNS alert topic; email |
| Q-11 | Packing facility | The producing factory's facility from `Balancer:PackingFacilities` (the shipment service may still pack split orders elsewhere) | Shipment service `FacilityResolver` rules |
| Q-12 | B-12 "actual cost once known (source to confirm)" | The TG Supplier sent event may report `quantity` per process in `productionLog`; actual line cost = reported quantity × quoted unit cost, actual labor summed on the allocation; material and packing stay quoted | Actual cost from SAP / finance; actual = quoted until a source exists |
| Q-13 | B-15 "on time vs ETA" without delivery data | On time = produced (sent) on or before the due (ship-by) date computed at allocation | Consume carrier delivery events from the shipment service |
| Q-14 | TG Supplier sent / reproduced event shape | **Checked against TG Supplier (2026-11-25):** `TenenGroup.Supplier.Events.ItemSent` (sent time = SQS `SentTimestamp`) and `…ItemProductionStatusChanged` (`Reproduction` marks the allocation, the next `ItemSent` is the reproduction); no production log; SQS message id is the idempotency key | TG Supplier adds `sentAt`, quote id, production log |
| Q-15 | Reproduction in the catalog | `POST /v1/quotes/{originalQuoteId}/produced` with `reproductionOfQuoteId` = the original; the catalog creates `{quoteId}-RP{n}`, which becomes the produced record's quote id and is linked to the original record | Re-quote and select before producing |
| Q-16 | How the item moves to the new supplier page in TG Supplier (B-23) | The Balancer publishes `balancer.allocation.transferred` (on the allocation topic, own `EventType`) and keeps the page on the allocation; TG Supplier is expected to consume it | Call a TG Supplier API synchronously |
| Q-17 | Production pages list per supplier | `Balancer:ProductionPages:{factoryId}` plus the pages already used by allocations | Read pages from TG Supplier |
| Q-18 | Transfer re-quote | Preview uses catalog `estimate` (nothing stored); confirm requests a `live` quote (new set `…-R{n}`), marks the old quote `transferred`, selects the new one. A non-producible target is reported per item and not moved | Reuse the original quote set's target quote |
| Q-19 | Own-factory source | Catalog `GET /v1/user/me` called with the caller's token (forwarded `Authorization` header), logged at Information | Read the `user_factory` table directly; JWT claim |
| Q-20 | Permission toggle source | Catalog `GET /v1/feature-toggle` (`permissions.enable_all`) with Basic auth, cached 10 minutes, fail closed | Own toggle table |
| Q-21 | Daily plan input (B-22) | Local live allocations with status `Allocated` for the factory and due date (overdue ones shown on today), grouped by the stored process lines of the selected quote; capacity from `GET /v1/process-capacity/factories/{id}` (`maxPerDay × capacityPct`, `today.used` for today only) | `POST /v1/quotes/search` (status `Selected`) at every page load |
| Q-22 | Target share for B-14 | Average quoted (`sharePct`) share of the routed items' candidates in the period | Global factory share from the catalog |
| Q-23 | Routing dashboard during shadow mode | KPIs count live and fallback allocations only; shadow decisions appear only in the shadow card | Count shadow decisions as "would-be" routing |
| Q-24 | Estimated savings in the shadow report | Σ (legacy total − new total) over disagreeing items; legacy total from the legacy decision or the new balancer's candidate for the legacy factory | Include agreeing items; ETA-weighted savings |
| Q-25 | UI pages for DIM cost and capacity reservations | Not in the mockup: DIM cost export from the routing dashboard (`Balancer.ViewDimCost`), reservations via API only | Dedicated pages |
| Q-26 | Shipping cost currency vs quote currency | Summed as they are (both USD today) | FX conversion |

## Requests to orchestrator

None blocking. Notes:

- `spec/backend/overview.md` / `api.md` / `database.md` / `infrastructure.md` could link `spec/modules/4b-balancer.md`
  for the Balancer endpoints, the `balancer` schema and its configuration keys (shared files, not edited here).
- Datadog monitor on the log message `BALANCER FALLBACK` (B-11 alert) — DevOps.
- TG Supplier: fire `supplier.item.sent` / `supplier.item.reproduced`, send legacy decisions to
  `POST /v1/legacy-decisions` during shadow mode, consume `balancer.allocation.transferred`.

No npm packages were added; no shared frontend or backend file was edited.
