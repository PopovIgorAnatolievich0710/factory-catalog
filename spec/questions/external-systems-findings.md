# Open questions checked against OCS, TG Supplier and the shipment service

Checked on 2026-11-25 against `master` of `C:\shakuro\OCS` (d9957c6), `C:\shakuro\supplier_api` (ebc0825) and
`C:\shakuro\ShipmentService` (ad927cf). Paths are relative to each repo. Verdict: ✅ matches the implementation,
⚠️ differs (needs a change or a decision), ❓ not found in any of the three repos.

**Status (2026-11-25):** OQ-7, OQ-8, OQ-9, OQ-16 and OQ-43 are re-implemented to these contracts (balancer OCS
mapper and component breakdown client, route sync by service level / priority / working days, supplier event
types, factory → packing facility map, migration `AddShipmentAndSupplierContracts`). The other rows are recorded in
`spec/open-questions.md` with what is still to decide.

## Summary

| OQ | Topic | Verdict | Finding |
|---|---|---|---|
| OQ-7 | OCS paid-order message | ⚠️ | Topic right; event name, envelope, filter attribute, item shape all differ; no CBD, no quantity, no event flag |
| OQ-8 | Shipment routes | ⚠️ | Endpoint and auth right; enums are ints, time frames are working days, cheapest is per service level with priority overrides |
| OQ-9 | Supplier sent / reproduced events | ⚠️ | Topic right; real types are `TenenGroup.Supplier.Events.*`; no `sentAt`, no production log |
| OQ-10 / OQ-83 | Transfers | ⚠️ | TG Supplier already has a transfer API and emits `ItemSupplierChanged`; it consumes no balancer event |
| OQ-11 | Replacement screen (C-16) | ❓ | No alternative / replacement code in TG Supplier: the screen is new work there |
| OQ-12 | Legacy balancer | ⚠️ | SKU lists live inside TG Supplier (`saveSkus`), nothing is pulled; hook point for shadow decisions identified |
| OQ-14 / OQ-15 / OQ-18 | AD, groups, platform names | ✅ | Local dev values equal the shipment service ones; naming convention confirmed |
| OQ-16 | Factory ids | ⚠️ | 110 / 125 are TG Supplier ids, mapped to shipment facilities 1 / 7; **315 exists nowhere** |
| OQ-17 / OQ-97 | Users bound to factories | ⚠️ | TG Supplier has its own users (`TABLE_SUP_USERS.SupplierId`), not AD |
| OQ-21 | Currencies | ⚠️ | An FX service exists (`exchange-rate-service`); OCS is multi-currency; shipping costs are USD |
| OQ-38 | Working-day calendars | ⚠️ | Shipment service has country holidays, weekends, facility working days, route working days |
| OQ-42 | Delivery data | ⚠️ | Shipment service publishes `ShipmentShipped` / `ShipmentDelivered` with delivery status |
| OQ-43 | Packing facility | ⚠️ | Not always the producing factory: shipment service resolves one facility per fulfillment, transfers items |
| OQ-26 / OQ-58 | Labor / packing cost source | ❓ | TG Supplier stores no cost per process; only machine daily goals |

## OQ-7 — OCS paid-order event

- Topic `order-core-service-<env>-domain-events` (`devops/terraform/domain-events.tf:2`) — confirmed. OCS creates no
  subscriptions; the balancer creates its own SQS queue and subscription.
- Event type: there is no `ocs.order.paid`. Use `paid_order_created_v2`
  (`backend/src/OrderCoreService.DomainContracts/Events/V2/OrderCreatedMessage.cs:13`). All types:
  `paid_order_created(_v2)`, `published_order_changes(_v2)`, `refund_created`, `store_credit_candidate`,
  `TenenGroup.Voucher.CreationRequest`, `TenenGroup.Voucher.CancelRequest`.
- Envelope `{ GroupId, EventType, DataJson, Attributes, Metadata { MessageId, MessageDate } }`; the payload is a JSON
  **string** in `DataJson`. The only SNS message attribute is `event_type` — the filter policy must use it.
- Payload is per order with `fulfillments[] → product_items[]`; enums are numbers.
  - destination country: `shipping_address.country_code`
  - promised delivery date: `fulfillments[].estimated_delivery_date`
  - SLA / ship-by: nearest are `fulfillment_due_dates[].production_due_date` / `pickup_due_date` + `is_guaranteed`
    (nullable, date only)
  - site: `site_id` (int); paid-at: none, only `created_date`
  - SKU: site SKU string only (no catalog UUID, no alias)
  - quantity: none — one product item = one unit
  - fulfillment type: `fulfillment_type` string per fulfillment (`virtual`, `dropshipper`, others such as
    `tg_shipment`; "Factory" is not a value; null when the lookup fails)
  - event order flag / name: not present (only an integer `event_level` on shipping items)
  - **CBD: not in any event.** OCS calls the external CBD service only when sending to supplier and keeps the
    breakdown ids; the component detail comes from Catalog `component-breakdown/v2/{site}`.
- Timing: the event fires at order creation from the paid bill, before verification release (order may still be on
  hold / cancelled). Imported orders are sent too (`DomainNotificationService.cs:83-87` logs "Skipped" but continues).
- OCS has no balancer and no factory ids. Items go to production through OM (`sendToSupplier`); supplier per item =
  frozen `ProductionSupplierId`, else `products.default_supplier_id`. Promised dates come from the bill / OM
  (`calculateFulfillmentsEta`), not from the shipment service.

**Impact:** the balancer consumer needs a mapper from `paid_order_created_v2`, a CBD source (CBD service / Catalog
`component-breakdown/v2` per item, or ask OCS to add it), a rule for quantity (count of product items with the same
SKU), and a decision on the missing event-order flag and on orders that are later held or cancelled.

## OQ-8 — Shipment service routes

- `POST v1/shippingRoute/getByCriteria` (`Api/Controllers/ShippingRouteController.cs:42-51`) — confirmed. Response
  `{ data, totalRowCount }`; paging `page { offset, limit }` (default limit 10, no max).
- No "active" flag: filter `statuses: [0]` (Active = 0, Inactive = 1). No `JsonStringEnumConverter` — enums are ints.
  Without the filter inactive routes are returned.
- Route: `PackingFacilityIds int[]` (null = any facility), `FromCountryCode`, `ToCountryCodes[]`, `ShippingCost` +
  `CurrencyCode`, `MaxShippingValue`, `PackingTimeFrame` / `ShippingTimeFrame` (days), `ShippingWorkingDays`,
  `Priority`, `CarrierServiceType.Method` (Basic / Expedited / Express / Virtual).
- Time frames are **working days**: `Business/Services/RouteEddCalculator.cs:76-94` counts packing over facility working
  days + origin holidays and shipping over route working days + destination holidays. The service computes one date
  (packing + shipping); "min = shipping, max = shipping + packing" is our own reading.
- Cost is flat per route — confirmed. Overwritten daily (02:00 Asia/Jerusalem) from S3 agreements CSV, USD only
  (`Business/Jobs/ImportShippingCostFromS3Job.cs`).
- Live route choice (`Business/Services/ShippingRouteService.cs:104-118`): active, same **service level**, facility
  country, destination, facility; routes with `Priority` win, then cheapest. `getOptimal` / `getOptimalList` exist
  (deadline-aware, ignore status and priority).
- Auth: Basic, user `admin`; env names `API_SERVICE_USERNAME` / `API_SERVICE_USER_PASSWORD`, KMS secret
  `ENCRYPTED-SHIPMENT-SERVICE-USER-PASSWORD`. Basic callers bypass permission checks.
- Alternative to the daily pull: topic `shipment-service-<env>-shipping-routes-events`
  (`shipping_route_created/updated/deleted`, full route), but gated by `SHIPPING_ROUTES_EVENTS_ENABLED` (default off)
  and the daily cost import does not publish them.

**Impact:** send `statuses: [0]`, read enums as ints, cache cheapest per (facility, country, service level) and honour
`Priority`, treat time frames as working days.

## OQ-9 — TG Supplier events

- Topic `supplier-<env>-events` (`configs_parameters.json`) — confirmed. Envelope `{ EventType, DataJson }` (string),
  `EventType` also an SNS attribute; sent through an outbox job.
- Real types: `TenenGroup.Supplier.Events.ItemSent` `{ OrderId, ItemId, SupplierId, SupplierType }`
  (`Tenengroup.Supplier.Api/Handlers/SentItemEventHandler.cs:46`) — **no `sentAt`**;
  `…ItemProductionStatusChanged` `{ OrderId, ItemId, SupplierItemId, ProductionStatus, ChangedAt }` — reproduction is
  `ProductionStatus = "Reproduction"`; `…ItemSupplierChanged`; also `ItemCreation`, `ItemCancelation`,
  `OutOfStockItemMarked`, `OutOfStockItemsTransferred`, `BalancerRunCompleted`.
- `ItemWasSentForReproduction` is internal only (event log), not on SNS.
- **No production log / process / machine type in any event.** Internally there are station-consumption rows per
  component / department and `TableMachineItems` (item → machine).

**Impact:** sent-at must come from the SNS / outbox timestamp; reproduction from `ItemProductionStatusChanged`; the
production log (DIM actual cost, OQ-41; production conflict, OQ-72) needs a new TG Supplier payload.

## OQ-10 / OQ-83 — Transfers

TG Supplier already transfers: `Transfer/transferitem(s)`, `transferFullOrders`, `autotransfer`
(`Controllers/TransferController.cs:114-168` → `TransferService.TransferItemsAsync(items, newSupId, newPage, …)`), and
publishes `ItemSupplierChanged`. A "production page" is a `TABLE_SUP_PAGE` row (global `Id` + `SupId`, due date,
production days). Nothing consumes a `balancer.allocation.transferred` event. Options: TG Supplier consumes our event,
or the balancer calls the transfer API.

## OQ-11 — Replacement screen

No alternative / replacement component screen or code in TG Supplier (nor in git history). Related only:
`ComponentBreakdownSanitizer` (ERP id blacklist) and `BomingFallBackService`. C-16 is new work on the TG Supplier side.

## OQ-12 — Legacy balancer (TG Supplier)

- `POST WorkloadBalancerProcess/run` → `Services/WorkloadBalancer/WorkloadBalancer.cs`. Items on the "new" pages of
  queue supplier 140; `CountryBalance { Priority, Share, CountryId }` caps each country at `total × Share / 100`;
  an order goes to a country only if **all** its items' SKUs are in that country's list; shipping-country
  restrictions reroute to a fallback.
- SKU lists `CountryBalanceSku { CountryBalanceId, Sku, Producible, Removed, PackingCenterId }` are stored in TG
  Supplier and edited via `CountryBalancer/saveSkus` — nothing is pulled from outside today.
- Country → factory: Israel splits TGI 120 / TGN 130; other countries use the master supplier of
  `SupplierIdsForBalancing` [702, 126, 218].
- Second stage: BI optimization (`BalancerRunCompleted` → SQS `BalancerOptimizationFeedReady` CSV), with an existing
  dry-run switch `BiBalancerOptimizationDryRun`.
- Shadow hook: `WorkloadBalancer.cs` after the country result (~line 60) or before `TransferItemsAsync` in
  `IsraelWorkloadBalancerProcessService.cs:119-150`.

## OQ-16 / OQ-43 — Factories and packing facilities

- TG Supplier suppliers: Hibert 101, TGI 120, TGN 130, TGO 126, TGH 702, queue 140; balanceable 110, 113, 120, 130,
  122, 702; countries Israel = 1, Hungary = 4, Thailand = 6; packing centers KG, NZ.
- Shipment service facilities: 0 Default, 1 Kiryat Gat, 2 Nazareth, 3 TLV, 4 VPN (IL), 5 Hungary, 6 Salamander,
  7 Thailand, plus Merukazim-US / UK. `supplier_facility` maps supplier → facility: 110, 120, … → 1; 113, 130 → 2;
  122, 702 → 5; 125, 126 → 7.
- So **factory ids ≠ packing facility ids** (our assumption is wrong) — the balancer needs the supplier → facility
  mapping. **315 appears in none of the repos** (only as a ZIP prefix in OCS).
- Packing is not always at the producing factory: `Business/Services/FacilityResolver.cs:34-95` resolves one facility
  per fulfillment (mixed-country → Kiryat Gat, `IsTransferRequired`).

## OQ-14 / OQ-15 / OQ-18 — AD and platform

- Shipment service: tenant `26c34b0b-…`, API client `17fd6764-…`, UI client `c4e70d41-…`, scope
  `api://17fd6764-…/RnD.Access` — what the local catalog setup already reuses. OCS has its own registrations.
- Roles from `GroupPermissions.json` `{ RoleGroupMapping, RestrictedGroups }` on `cdn-mnn` (`Dev` / `Stg` prefixes).
  Existing groups: `SHP-*`, `CSR_*`, `Payment_Operation_Manager`, `BO_Administrator`, `BO_Viewer`; no `FC-*` yet.
- Conventions: KMS alias = service name, namespaces `<service>-{prod|stg|dev}`, Slack `<service>-builds`, Docker Hub
  `tenen/<service>-main|-ui`, master → prod, release → stg, develop → dev.

## OQ-17 / OQ-97 — Users

TG Supplier has its own users (`TABLE_SUP_USERS { UserName, LoginPassword, SupplierId, LoginRole, … }`), one supplier
per user, no AD. Production floor users would need AD accounts (as OQ-97 assumes), and `SupplierId` could seed
`user_factory`.

## OQ-21 — Currencies

An FX service exists: `exchange-rate-service` (`EXCHANGE_RATE_BASE_URL`,
`/api/ExchangeRate/GetExchangeRate?from=&to=`), used by the shipment service and OCS. OCS orders are multi-currency;
shipping costs are USD. "No FX source exists" is not true.

## OQ-38 / OQ-42 — Calendars and delivery data

- Shipment service has `CountryHoliday`, `CountryWeekEnd`, `facility_config.working_days`, route
  `ShippingWorkingDays` and carrier pickup times. Possible bug there: `RouteEddCalculator.cs:130-148` parses working
  days 0-based while the seed looks 1-based.
- Events on `shipment-service-<env>-domain-events`: `ShippingService.Events.ShipmentReadyToPack`, `…OnHold`,
  `…Packed`, `…Shipped`, `…Delivered`, `…Returned`, with carrier, tracking, `DeliveryStatus`,
  `EstimatedDeliveryDate`. "On time" (B-15) can use real delivery data.

## OQ-26 / OQ-58 — Costs

TG Supplier stores no labor cost per process and no packing cost. It has machines with daily goals
(`TableMachine`), productivity logs and `Group.ProductionDays`. The "Pricing Tool" sample is still needed.

## Side findings (security)

- `ShipmentService` `Api/appsettings.json:103-111`: plaintext OcsApi / CatalogApi passwords committed.
- `supplier_api` `Tenengroup.Supplier.Api/appsettings.json:171`: an AWS access key committed; the API accepts any
  `Auth-Token` that deserializes to `UserData` (no signature check, `ActionFilters/AuthenticationFilterAttribute.cs`).
