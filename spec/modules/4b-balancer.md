# Module 4B — Balancer service and its UI

The second deployable of the repository: the new "smart balancer" that routes every paid order item to the factory
with the best total cost (production + shipping) and delivery time. Requirements: `spec/requirements/4-balancer.md`
(B-04 … B-24, "Decision logic", schemas C–E). The catalog side (B-01 … B-03, quote lifecycle) is Module 4A —
contract `spec/modules/4a-quotes.md`. Assumptions: `spec/questions/m4b-balancer.md`.

Base URL `http://localhost:5011`. JSON is camelCase, enums are PascalCase strings in responses and parsed
case-insensitively, dates are ISO-8601 (`DateOnly` as `yyyy-MM-dd`), errors follow `spec/backend/api.md`.

---

## Architecture

Same layering, conventions and shared infrastructure (`Tenengroup.Infrastructure.Common`) as the catalog API.

| Project | Content |
|---|---|
| `Tenengroup.Balancer.Api` | `Program` (`migrate` argument applies the EF migrations and exits), `Startup` (Basic + Azure AD JWT auth, CORS, Swagger, `/ping`, Hangfire dashboard `/hangfire` behind Basic), controllers, request models, `ServiceAccountOnly` filter |
| `Tenengroup.Balancer.Business.Abstractions` | CQRS contracts, options, catalog / shipping client contracts, event contracts |
| `Tenengroup.Balancer.Business` | Decision engine, handlers, catalog client, shipping route client, queue handlers, jobs, current user |
| `Tenengroup.Balancer.Domains` | Entities and enums of the `balancer` schema |
| `Tenengroup.Balancer.DataAccess(.Abstractions)` | `BalancerDbContext` (default schema `balancer`, snake_case), configurations, `Initial` migration, design-time factory |
| `Tenengroup.Balancer.CompositionRoot` | `Root.RegisterRoot` (explicit scoped DI), `ExternalClientsModule` (typed `HttpClient`s with Polly retry), `HangfireModule` (schema `hangfire_balancer`), hosted services (recurring jobs, two SQS consumers) |
| `tests/unit-tests/Tenengroup.Balancer.Business.UnitTests` | NUnit + Moq: decision engine, intake, output, production, dashboard |

Flow:

```
OCS paid order items ──SQS (OrderItems:QueueUrl)──┐
                   POST /v1/allocations (Basic) ──┤
                                                  ▼
                         AllocateOrderCommandHandler
       ├─ POST /v1/quotes per item (catalog, live|shadow)      ─ quotes (B-01…B-03)
       ├─ shipping_route_cache (daily refresh from the shipment service, B-04)
       ├─ RoutingDecisionEngine.Decide (pure, B-05…B-09, B-11, B-17)
       ├─ allocation + allocation_cost_line rows (decision log, DIM cost)
       └─ live: Hangfire job AllocationOutputJob
              ├─ POST /v1/quotes/{quoteId}/select (catalog, B-10)
              └─ SNS balancer.allocation.decided (BalancerEvents:AllocationDecidedTopicArn)

TG Supplier sent / reproduced ──SQS (SupplierEvents:QueueUrl)──► RecordProductionCommandHandler
       ├─ POST /v1/quotes/{quoteId}/produced (catalog; reproduction → {quoteId}-RP{n})
       ├─ production_record row (reproduction = new row linked to the original)
       └─ SNS balancer.quote.produced (BalancerEvents:QuoteProducedTopicArn)
```

### Authentication and permissions

`AddTenengroupAuth` as in the catalog: Basic (machine-to-machine, `API_SERVICE_USERNAME` / `API_SERVICE_USER_PASSWORD`)
and Azure AD JWT with the shared permission matrix (`spec/backend/permissions.md`). Writes from other systems
(`POST /v1/allocations`, `POST /v1/legacy-decisions`) are Basic only (`[ServiceAccountOnly]` → 403 for a JWT).

- `IPermissionEnforcementToggle` → `CatalogPermissionEnforcementToggle`: reads the catalog's
  `GET /v1/feature-toggle` (toggle `permissions.enable_all`) with the Balancer's Basic credentials, caches the value
  for 10 minutes and logs `Permission enforcement read from the factory catalog feature toggle …` at Information.
  When the catalog cannot be reached permissions are enforced (fail closed, Warning log).
- Own factory (`ICurrentUser`): the caller's factories come from the catalog's `GET /v1/user/me`, called with the
  caller's own `Authorization` header (forwarded token), once per request; logged at Information
  (`Own factories of {Actor} resolved from the factory catalog GET v1/user/me …`). Operations managers and BO
  administrators (or permissions switched off) may act on any factory.

---

## Database — schema `balancer`

One migration `Initial` (`DataAccess/Migrations/20261124145246_Initial.cs`). Money is `numeric(18,4)`.

### `allocation` — one routing decision per order item and mode (decision log, DIM cost, open items)

| Column | Type | Notes |
|---|---|---|
| `id` | uuid PK | |
| `order_id`, `order_item_id` | varchar(100) | API limits them to 90 (catalog quote key) |
| `sku_uuid` | varchar(100) | as received (UUID, alias or Site SKU) |
| `quantity` | int | |
| `fulfillment_type` | varchar(20) | `Factory`, `Virtual`, `Dropshipper` |
| `destination_country` | varchar(2) | upper case |
| `promised_delivery_date`, `sla_delivery_date` | date null | |
| `is_event_order` | bool | uses reserved capacity (B-09) |
| `event_name`, `site` | varchar(200) / varchar(100) null | transfer search (B-23) |
| `paid_at` | timestamptz null | production days = order (paid) → sent |
| `cbd` | jsonb | the CBD sent to the catalog |
| `mode` | varchar(20) | `Live`, `Shadow`, `Fallback` |
| `status` | varchar(20) | `Allocated`, `NotRouted` (virtual / dropshipper), `Produced` |
| `fallback_reason` | varchar(500) null | `QuotesUnavailable`, `RoutesUnavailable`, `NoFeasibleFactory` |
| `quote_key`, `selected_quote_id` | varchar(200) / varchar(220) null | catalog quote set / quote |
| `factory_id`, `original_factory_id`, `packing_facility_id` | varchar(20) null | packing facility = factory (Q-11) |
| `production_page` | varchar(100) null | TG Supplier page (transfers) |
| `material_cost`, `labor_cost`, `packing_cost`, `production_cost` | numeric null | quoted cost of the selected quote (B-12); dropshipper: `total_cost` = dropshipper cost |
| `shipping_cost`, `total_cost`, `currency` | numeric / varchar(3) null | |
| `cheapest_feasible_total_cost` | numeric null | B-15 cost vs cheapest |
| `actual_labor_cost` | numeric null | actual vs quoted (B-12) |
| `eta_from`, `eta_to`, `due_date` | date null | ETA at allocation (B-15); due = ship-by date |
| `meets_promise`, `is_split` | bool | |
| `decision_reason` | varchar(1000) | why the winner won |
| `candidates` | jsonb | every candidate with costs, ETA, share, capacity, score, exclusion reason |
| `settings_version` | int | `balancer_settings.version` used |
| `transfer_count` | int | B-13 transfer rate |
| `decided_at`, `output_published_at`, `produced_at` | timestamptz | |

Indexes: `(order_id, order_item_id)`, `decided_at`, `(factory_id, status, due_date)`.

### `allocation_cost_line` — DIM cost by component and process (B-12)

`id` bigint PK, `allocation_id` uuid FK (cascade), `kind` (`Component`, `Process`, `Packing`), `code` (ERP ID or process
id), `quantity`, `quoted_unit_cost` null, `quoted_cost`, `actual_quantity` null, `actual_cost` null.

### `allocation_transfer` — transfer log (B-23, B-24)

`id` bigint PK, `allocation_id` FK, `order_id`, `order_item_id`, `sku_uuid`, `from_factory_id`, `from_page` null,
`to_factory_id`, `to_page`, `reason` (`ProcessOffline`, `ComponentOos`, `OverCapacity`, `Quality`,
`CustomerRequest`, `Other`), `note` null, `old_quote_id` / `new_quote_id` null, `cost_delta` null, `currency` null,
`old_eta` / `new_eta` null, `transferred_by` varchar(320), `initiator` (`User`, `Balancer`), `transferred_at`.
Indexes: `allocation_id`, `(order_id, order_item_id)`, `transferred_at`.

### `production_record` — produced quotes (B-20, B-21)

`id` bigint PK, `allocation_id` FK, `source_event_id` (idempotency, unique), `order_id`, `order_item_id`, `sku_uuid`,
`quote_key`, `quote_id` (the produced quote; a reproduction gets the catalog's `{quoteId}-RP{n}`), `factory_id`,
`packing_facility_id`, `sent_at`, `production_days`, `is_reproduction`, `reproduction_of_quote_id` null,
`reproduction_of_record_id` null (self FK), `production_log` jsonb, `created_at`. Index `(factory_id, sent_at)`.

### Other tables

| Table | Columns | Purpose |
|---|---|---|
| `shipping_route_cache` | `id`, `origin_facility_id`, `destination_country`, `cost_amount`, `cost_currency`, `transit_days_min`, `transit_days_max`, `refreshed_at`; unique `(origin_facility_id, destination_country)` | daily route cache (B-04), cheapest active route per origin and country |
| `legacy_decision` | `id`, `order_id`, `order_item_id`, `factory_id`, `total_cost` null, `eta` null, `received_at`; unique `(order_id, order_item_id)` (latest wins) | legacy balancer decisions for the shadow comparison (B-19) |
| `capacity_reservation` | `id`, `factory_id`, `date_from`, `date_to`, `reserved_pct` (1–100), `event_name`, `updated_by`, `updated_at` | event reservations (B-09) |
| `balancer_settings` | `version` PK, `mode` (`Live` / `Shadow`), `late_penalty_per_day`, `split_penalty`, `share_weight`, `success_rate_enabled`, `success_rate_weight`, `share_window_days`, `share_gap_threshold_pct`, `default_production_days`, `updated_by`, `updated_at` | versioned scoring config editable without a deploy; no row = defaults from `Balancer` options (version 0) |

---

## Decision algorithm (`Business/Decisions/RoutingDecisionEngine.cs`, pure and deterministic)

Input (`DecisionContextFactory`): the quote set per item, cached routes to the destination country, today's
capacity reservations per factory, the actual share of every factory over the last `ShareWindowDays` (same mode),
average production days per factory (last 90 days, non-reproductions), success rate per factory
(`1 − reproductions / produced`), the factories already holding other items of the order (anchors), the settings.

1. **Filters** per item and factory (exclusion reason in the candidate list): not producible (the catalog's reason
   codes, e.g. `COMPONENT_OOS`, `PROCESS_OFFLINE`, `PROCESS_NOT_AVAILABLE`); capacity signal 0 → `NO_CAPACITY`;
   capacity left after reservations ≤ 0 for a non-event order → `CAPACITY_RESERVED`; no cached route →
   `NO_ROUTE`. Virtual and dropshipper items never reach the engine.
2. **Score** each feasible candidate:
   - `total = quote.cost.total + route.cost`; `etaFrom/To = today + productionDays + transitMin/Max`
     (`productionDays` = factory history or `DefaultProductionDays`).
   - effective share = `sharePct × (capacity − reserved)/100`, renormalised over the feasible candidates — partial
     capacity lowers a factory's share and the freed share goes to the others (B-08).
   - `score = total × (1 − ShareWeight × (effectiveShare − actualShare)/100) + LatePenaltyPerDay × daysLate +
     (SuccessRateEnabled ? SuccessRateWeight × (1 − successRate) × total : 0)`.
3. **Order-level pass** (B-06): if one factory can build every item, the best such factory wins for all items
   (ranked by SLA misses, then summed score). Otherwise every combination is ranked (sum of scores +
   `SplitPenalty × (factories − 1)`, anchors count as used factories), greedy above 50 000 combinations. A split
   is flagged on every item and logged (Warning). An SLA order is split only when keeping it together misses the SLA
   for more items.
4. **Business rules** (B-09): SLA misses rank first, so an SLA order is routed to meet it even at a higher cost;
   reserved capacity is only usable by event orders. Ties: score, then the larger share deficit, then factory id.
5. **Fallback** (B-11): no quote set (catalog unavailable) or no feasible candidate → pick by default share: the
   producible quotes' shares, else `Balancer:FallbackShares`, else an equal split of the active catalog factories;
   the factory with the largest deficit to its share wins. Mode `Fallback`, `fallback_reason` set, Error log
   `BALANCER FALLBACK …` (alert hook). Nothing is left unrouted; with no share source at all the request fails.

Every decision stores all candidates, the cheapest feasible total, the reason text and the settings version.

---

## Endpoints

| Method and path | Auth | Purpose (story) |
|---|---|---|
| `POST /v1/allocations` | Basic only | Allocate a paid order synchronously (same as the SQS message) → `AllocationDto[]` |
| `POST /v1/allocations/search` | `BalancerView` | Decision log list (filters `mode`, `status`, `factoryId`, `destinationCountry`, `fulfillmentType`, `orderId`, `decidedAt`; search = order id; sort `decidedAt`, `orderId`, `factoryId`, `totalCost`, `mode`) |
| `GET /v1/allocations/{id}` | `BalancerView` | One decision with candidates and the legacy pick |
| `POST /v1/estimates` | any authenticated | Likely factory and ETA without allocating (B-16), catalog `mode: estimate` |
| `POST /v1/legacy-decisions` | Basic only | Legacy balancer decision intake for shadow mode (B-19) |
| `GET /v1/dashboard/routing?from&to&factoryId&country` | `BalancerViewDashboards` | Routed per day / factory, transfer rate, on-time %, cost vs cheapest, fallbacks, target vs actual share (B-13 … B-15) |
| `GET /v1/dashboard/shadow-comparison?from&to&factoryId&country` | `BalancerViewDashboards` | Agreement rate, estimated savings, disagreement reasons (B-19) |
| `POST /v1/dim-cost/search`, `POST /v1/dim-cost/export` | `BalancerViewDimCost` | Quoted vs actual DIM cost per order item, Excel export `dim_cost_*.xlsx` (B-12) |
| `GET /v1/dim-cost/{allocationId}` | `BalancerViewDimCost` | Cost lines by component and process |
| `GET /v1/daily-plan?factoryId&date` | `BalancerViewDailyPlan` + own factory | Daily plan per process (B-22) |
| `POST /v1/transfers/open-items/search` | `BalancerView` | Open (allocated, not produced, live) items; filters `orderId`, `factoryId`, `productionPage`, `eventName`, `isEventOrder`, `site`, `dueDate`; `canTransfer` per row |
| `POST /v1/transfers/open-items/ids` | `BalancerTransfer` | All ids matching the search ("select all") |
| `GET /v1/transfers/pages?factoryId` | `BalancerView` | Production pages of a supplier (`Balancer:ProductionPages` + pages in use) |
| `POST /v1/transfers/preview` | `BalancerTransfer` | Re-quote at the target (catalog `estimate`), cost change, target capacity, ETA |
| `POST /v1/transfers` | `BalancerTransfer` + own factory (ops: any) | Re-quote (`live`, new `…-R{n}` set), mark old quote transferred, select the new one, move, log, publish |
| `POST /v1/transfers/search` | `BalancerView` | Transfer log (search order / SKU / user / page / note; filters `supplier` (from or to), `orderId`, `fromFactoryId`, `toFactoryId`, `fromPage`, `toPage`, `reason`, `transferredBy`, `transferredAt`) |
| `GET /v1/settings`, `PUT /v1/settings` | `BalancerView` / `BalancerEditReservations` | Scoring config and live/shadow switch (new version per change) |
| `GET/POST/PUT/DELETE /v1/capacity-reservations[/{id}]` | `BalancerView` / `BalancerEditReservations` | Event capacity reservations (B-09) |
| `GET /v1/quotes/{quoteKey}`, `POST /v1/quotes/search` | `BalancerView` | Pass-through to the catalog for audit links |
| `GET /ping` | anonymous | health |

`POST /v1/allocations` body (also the SQS message `data` of event type `ocs.order.paid`):

```json
{
  "orderId": "ORD456789",
  "destinationCountry": "DE",
  "promisedDeliveryDate": "2026-12-02",
  "slaDeliveryDate": null,
  "isEventOrder": false,
  "eventName": null,
  "site": "tenengroup.com",
  "paidAt": "2026-11-24T09:55:00Z",
  "items": [
    {
      "orderItemId": "ITM001",
      "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
      "quantity": 1,
      "cbd": [{ "erpId": "R.VM.BRD.08MM", "quantity": 1 }],
      "fulfillmentType": "Factory"
    }
  ]
}
```

Idempotent per `(orderId, orderItemId, mode)`: items already allocated are returned as stored; new items of the same
order are routed with the already used factories as anchors. `fulfillmentType` `Virtual` / `Dropshipper` (or a quote
set where every factory says `DROPSHIPPER`) → `status: NotRouted`, the dropshipper total cost is read from
`GET /v1/quotes/dropshipper-cost` and stored in `total_cost`.

`POST /v1/legacy-decisions`: `{ orderId, orderItemId, factoryId, totalCost?, eta? }`.
`POST /v1/estimates`: `{ skuUuid, quantity, cbd[], destinationCountry, promisedDeliveryDate? }` →
`{ factoryId, packingFacilityId, etaFrom, etaTo, totalCost, currency, isFallback, candidates[] }`.
`POST /v1/transfers` / `preview`: `{ allocationIds[], toFactoryId, toPage, reason, note }` (note required for
`Other`) → items `{ allocationId, orderId, orderItemId, fromFactoryId, fromPage, toFactoryId, toPage, oldQuoteId,
newQuoteId, costDelta, currency, targetCapacityPct, oldEta, newEta, skipped, problem }`.

---

## Events

Published with the shared `IEventPublisher` (SNS, message attribute `EventType`).

| Event type | Topic setting | When | Shape |
|---|---|---|---|
| `balancer.allocation.decided` | `BalancerEvents:AllocationDecidedTopicArn` | live / fallback allocation, after the catalog `select` | section D: `orderId`, `orderItemId`, `quoteKey`, `selectedQuoteId`, `factoryId`, `packingFacilityId`, `productionCost` / `shippingCost` / `totalCost` `{amount, currency}`, `eta {from, to}`, `mode` (`live` / `fallback`), `candidates[{factoryId, totalCost, eta, excludedReason}]` |
| `balancer.allocation.transferred` | `BalancerEvents:AllocationDecidedTopicArn` | after a transfer | `orderId`, `orderItemId`, `fromFactoryId`, `fromPage`, `toFactoryId`, `toPage`, `oldQuoteId`, `newQuoteId`, `reason`, `note`, `transferredBy`, `transferredAt` |
| `balancer.quote.produced` | `BalancerEvents:QuoteProducedTopicArn` | TG Supplier "sent" or "reproduced" | section E: `orderId`, `orderItemId`, `quoteKey`, `quoteId`, `status: "produced"`, `factoryId`, `packingFacilityId`, `quotedCost {material, labor, packing, total, currency}`, `sentAt`, `productionDays`, `reproductionOf` |

Shadow allocations publish nothing and select nothing. The output runs as a Hangfire job (retried) so a catalog or
SNS outage never loses the decision; `output_published_at` makes it idempotent.

Consumed (SQS, `IQueueReader`, attribute `EventType`, 10 messages per poll every `Balancer:QueuePollSeconds`):

| Queue setting | Event types | Message `data` (assumed, Q-2 / Q-14) |
|---|---|---|
| `OrderItems:QueueUrl` | `ocs.order.paid` | the `POST /v1/allocations` body |
| `SupplierEvents:QueueUrl` | `supplier.item.sent`, `supplier.item.reproduced` | `{ orderId, orderItemId, sentAt, productionLog: [{ processId, machineType, quantity }] }` |

Production: the latest live allocation of the order item is found; `sent` → catalog `produced` on the selected quote
(log without `quantity`), allocation `Produced`, actual labor from the reported quantities; `reproduced` → catalog
`produced` with `reproductionOfQuoteId` = the original → the catalog creates `{quoteId}-RP{n}`; a new
`production_record` linked to the original. Duplicate events (same message id) are ignored; a reproduction before
its sent event is retried.

---

## Assumed external contracts

- **Factory catalog** (`FactoryCatalog:BaseUrl`, Basic): exactly `spec/modules/4a-quotes.md` — `POST /v1/quotes`,
  `GET /v1/quotes/{quoteKey}`, `POST /v1/quotes/search`, `POST /v1/quotes/{quoteId}/select|produced|transferred`
  (responses `{ quoteKey, requestId, orderId, orderItemId, skuUuid, quantity, mode, validUntil, createdAt, quote }`),
  `GET /v1/quotes/dropshipper-cost?sku=`, plus `GET /v1/factories`, `GET /v1/feature-toggle`, `GET /v1/user/me`
  (forwarded user token) and `GET /v1/process-capacity/factories/{factoryId}` (daily plan capacity: `capacityPct`,
  `maxPerDay`, `today.used` of the factory's cell). Polly: 3 retries with exponential back-off on transient errors.
- **Shipment service** (`ShipmentService:BaseUrl`, Basic): `POST v1/shippingRoute/getByCriteria` with
  `{ statuses: [0], page: { offset, limit: 500 } }`, paged until `totalRowCount`; response
  `{ data: ShippingRoute[], totalRowCount }` where a route has `packingFacilityIds[]`, `toCountryCodes[]`,
  `shippingCost`, `currencyCode`, `packingTimeFrame`, `shippingTimeFrame`, `status` (numeric enum, 0 = Active).
  One cache row per (packing facility, destination country), the cheapest route kept; transit min =
  `shippingTimeFrame`, max = `shippingTimeFrame + packingTimeFrame`. Refresh daily (Hangfire recurring job
  `balancer-shipping-route-refresh`, `Balancer:ShippingRoutesCronExpression`, default `0 1 * * *`); an empty answer
  keeps the old cache. The catalog factory id must equal the shipment service packing facility id (Q-6).

---

## Configuration

| Key (env var form) | Default | Notes |
|---|---|---|
| `Kestrel__Endpoints__Http__Url` | `http://0.0.0.0:5011` | |
| `DATABASE_HOST/PORT/NAME/USERNAME/PASSWORD` | — | same database as the catalog, schema `balancer` |
| `API_SERVICE_USERNAME`, `API_SERVICE_USER_PASSWORD` | — | Basic auth of callers and the Hangfire dashboard |
| `AzureAd__Instance/TenantId/ClientId`, `GROUP_PERMISSIONS_URL`, `CORS_ALLOWED_HOSTS` | — | as the catalog |
| `Aws__DefaultRegion/AccessKey/SecretKey/ServiceUrl/SqsServiceUrl/SnsServiceUrl` | — | shared AWS infrastructure |
| `FactoryCatalog__BaseUrl/Username/Password` | — | catalog client |
| `ShipmentService__BaseUrl/Username/Password` | — | route client |
| `OrderItems__QueueUrl` | empty = consumer disabled (Warning) | OCS paid order items |
| `SupplierEvents__QueueUrl` | empty = consumer disabled | TG Supplier sent / reproduced |
| `BalancerEvents__AllocationDecidedTopicArn` | empty = not published (Warning) | |
| `BalancerEvents__QuoteProducedTopicArn` | empty = not published (Warning) | |
| `Balancer__Mode` | `Shadow` | initial mode until a `balancer_settings` row exists |
| `Balancer__LatePenaltyPerDay` / `SplitPenalty` / `ShareWeight` | 5 / 3 / 0.1 | initial scoring weights |
| `Balancer__SuccessRateEnabled` / `SuccessRateWeight` | false / 0.5 | B-17 |
| `Balancer__ShareWindowDays` / `ShareGapThresholdPct` / `DefaultProductionDays` | 7 / 2 / 3 | |
| `Balancer__FallbackShares__{factoryId}` | empty = equal split of active factories | B-11 |
| `Balancer__ProductionPages__{factoryId}__{n}` | empty | TG Supplier pages offered for transfers |
| `Balancer__QueuePollSeconds` | 5 | |
| `Balancer__ShippingRoutesCronExpression` | `0 1 * * *` | |

All names match `docker-compose.yml`, `devops/helm/chart/values.yaml` and `devops/docker/balancer-api.Dockerfile`.

---

## UI (`frontend/FactoryCatalogUI/src/app/balancer`)

| Page | Route | Gate | Content |
|---|---|---|---|
| Routing dashboard | `/balancer/routing` | `Balancer.ViewDashboards` | factory / country / 7-30-90 days filters; tiles (orders routed, transfer rate, on time vs ETA, avg cost vs cheapest, fallbacks); target vs actual share bars with highlighted gaps; shadow card (agreement, savings, why they disagree); items routed per day (stacked bars); "DIM cost (quoted vs actual)" Excel export with `Balancer.ViewDimCost` |
| Balancer decision log | `/balancer/decision-log` | `Balancer.View` | order-id search, mode and factory filters; one card per order with every item: winner, packing, total, ETA, mode, split / transferred / legacy pick, candidate table with costs, ETA, share · capacity and exclusion reason; server paging |
| Daily plan | `/balancer/daily-plan` | `Balancer.ViewDailyPlan` (own factory; ops and admins pick any) | today + 4 days; tiles; processes sorted by over-capacity and load with done / available / load bar; drill-down to order items with quote and due date |
| Transfers | `/balancer/transfers` | `Balancer.View` (transfer needs `Balancer.Transfer`) | "Find & transfer": search by order, supplier, page, event, site, due day; selection one by one, per page or all matches, kept across pages; target supplier, page, reason, note (required for Other); preview re-quote (quote old → new, cost change, ETA, target capacity) and confirm. "Transfer log": search, supplier and reason filters, quotes old → new, cost and ETA change |

State lives in the module reducer (`store/`), API calls in `services/balancer-service.ts` through `balancerApi`.

---

## Story coverage

| Story | Status | Notes |
|---|---|---|
| B-04 Route cache | done | daily Hangfire job, shipment service contract assumed from its code (Q-5, Q-6) |
| B-05 Lowest total cost meeting the promise | done | candidates and reason stored; UI decision log |
| B-06 Avoid splits | done | order-level pass, anchors for later items, split flagged and logged |
| B-07 Virtual / dropshipper | done | never routed; dropshipper cost read from the catalog and stored |
| B-08 Partial capacity lowers share | done | via the catalog capacity signal (stock + remaining process capacity) |
| B-09 SLA and event reservations | done | reservations API (`BalancerEditReservations`); no UI page for reservations (not in the mockup) |
| B-10 Select + allocation event | done | Hangfire job, catalog select, SNS section D |
| B-11 Fallback | done | default share, flagged `Fallback`, Error log as the alert hook (no alerting channel configured) |
| B-12 DIM cost quoted vs actual + export | partial | quoted cost by component / process stored; actual = labor from the reported process quantities × quoted unit cost (actual source to confirm, Q-12); export on the dashboard, no dedicated DIM cost page |
| B-13 Routing outcomes, transfer rate | done | |
| B-14 Target vs actual share | done | target = average quoted share of the routed items |
| B-15 On time vs ETA, cost vs cheapest | partial | on time = produced by the due (ship-by) date; delivery dates are not available to the Balancer (Q-13) |
| B-16 Estimate | done | `POST /v1/estimates`, same engine, catalog `estimate` mode |
| B-17 Success rate | done | from reproductions, switchable with a weight |
| B-18 Legacy retirement | partial | live / shadow switch (`PUT /v1/settings`); turning off the legacy balancer, country SKU lists and aliases happens in TG Supplier |
| B-19 Shadow mode | done | shadow quotes and decisions, legacy decision intake, comparison report on the dashboard |
| B-20 Supplier events | partial | consumer done; TG Supplier firing the events is outside this repository |
| B-21 Mark produced + event | done | reproductions linked, counted separately |
| B-22 Daily plan | done | from the selected (allocated, not produced) items and their stored quote lines; capacity from the catalog |
| B-23 Transfers | done | re-quote, mark transferred, select, move; moving the item in TG Supplier relies on the `balancer.allocation.transferred` event (Q-16) |
| B-24 Transfer log | done | |

## Unverified

- No PostgreSQL, AWS or live catalog / shipment service run: handlers are unit-tested with mocked `DbSet`s and
  clients; the migration was generated but not applied; SQL of the report queries was not executed.
- The HTTP layer (model binding, auth attributes) and the UI were not exercised against running backends (the UI
  type-checks and builds).
