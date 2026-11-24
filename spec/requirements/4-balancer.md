<!-- Source: https://tenengroup.atlassian.net/wiki/pages/viewpage.action?pageId=5342363700 | title: Module 4: Balancer | version 4 (2026-11-24T13:03:07.192Z) | saved verbatim from Confluence (markdown export; images are not included) -->

# Module 4: Balancer

**Goal:** route every order to the factory with the best total cost (production + shipping) and delivery time, using live production quotes from the factory catalog and routes from the shipment service.

**Key features:**

- Production quotes per order item, per factory
- Routing on total cost and ETA, with business rules
- Allocation event with the winning quote; exact DIM cost
- Shadow mode, then legacy balancer retirement
- Routing dashboards and production tracking

**Dependencies:** Modules 1–3; shipment service (routes, costs, timeframes); TG Supplier events; BI consumes the allocation and produced events. Replaces the legacy balancer, retired at go-live.

**Business value:** "smart balancer" savings of $140K–$1.2M a year from reassigning 2–5% of orders, fewer post-production transfers and late orders, and correct DIM cost per component and process for every order and factory.

## User stories

| ID | Epic | User story | Priority |
| --- | --- | --- | --- |
| B-01 | Production quotes | As the balancer, I want to request a production quote per order item (POST /quotes), so that I know which factories can build it. - One request per order item with its CBD and quantity - Sent once the order is paid and ready for production - Each quote set has a unique key, stored for later lookup | MUST |
| B-02 | Production quotes | As the factory catalog, I want each quote request to carry the order's CBD (components required by the customer's selection), so that I check stock and price only what this order needs. - The request includes the CBD: every Static component plus the Dynamic ones the customer selected, each with ERP ID and quantity - Only CBD components are checked for stock and priced - Processes come from the SKU's process mapping - A CBD component unknown to the catalog fails the quote with a clear reason | MUST |
| B-03 | Production quotes | As the balancer, I want each factory's quote to include producible + reason, material cost, labor cost, capacity signal and share, so that I can compare them. - Every factory is returned, producible or not, with a reason if not; an OOS component doesn't rule a factory out if an alternative flagged For balancer is in stock there - Material cost from components; labor cost = process quantity × that factory's labor cost per unit (incl. packing) - Capacity signal from stock and today's remaining process capacity (P-14), 0–100% - Share after redistributing for factories that can't produce | MUST |
| B-04 | Routing decision | As the balancer, I want each candidate factory combined with its cached shipping route, cost and timeframe (cache refreshed once a day), so that I compute total cost and ETA. - Route cost and timeframe come from a cache refreshed once a day - Factories with no route to the destination country are dropped - Total cost = production + shipping; ETA = production time + transit | MUST |
| B-05 | Routing decision | As the balancer, I want to pick the factory and packing facility with the lowest total cost and best on-time odds, within capacity, so that each order lands right the first time. - Picks the lowest total cost that meets the delivery promise - Only factories with capacity and a route are eligible - The catalog's share guides ties and spreads volume - Each decision is logged with all candidates and why the winner won | MUST |
| B-06 | Business rules | As an operations manager, I want split shipments avoided where possible, so that customers get one package. - Items of one order go to the same factory when it can build them all - A split happens only when no single factory can, and is logged | MUST |
| B-07 | Business rules | As the balancer, I want virtual and dropshipper fulfillments kept out of factory routing, while reading the dropshipper's total cost from the factory catalog, so that only TG-factory items are routed here and every item still has a cost. - Virtual and dropshipper items are never routed to a TG factory - Dropshipper total cost is read from the factory catalog and attached to the order | MUST |
| B-08 | Business rules | As the balancer, I want partial capacity (not just 0%) to lower a factory's share, so that strained factories get fewer orders. - A process below 100%, or with little capacity left today (P-14), lowers that factory's share for SKUs that need it - 0% removes the factory for those SKUs - Freed share goes to the other factories | MUST |
| B-09 | Business rules | As an operations manager, I want customer SLAs and event capacity reservations applied, so that priority orders and peak days are protected. - Orders with an SLA are routed to meet it, even at higher cost - Ops can reserve a % of a factory's capacity for event days - Reserved capacity is used only by event orders | SHOULD |
| B-10 | Allocation output | As the balancer, I want to mark the winning quote (quote.selected) and emit the allocation as a domain event, so that production and shipping invoicing consume it. - Winning quote is marked selected in the factory catalog - Allocation event carries order, item, factory, packing facility and quote - Production, the shipping invoice generator and BI consume it | MUST |
| B-11 | Allocation output | As an operations manager, I want a fallback rule (e.g. route by default factory share) when quotes or routes are unavailable, so that no order is left unrouted once the legacy balancer is gone. - If quotes or routes don't come back, the item is routed by default factory share - Fallback cases are flagged and alert ops - No order is left unrouted | MUST |
| B-12 | DIM cost | As a finance user, I want the exact DIM cost per order and factory, quoted vs actual, so that pricing and margin decisions use real numbers. - DIM cost per order item: material, labor and packing, by component and process - Quoted cost for the selected factory is stored with the order - Actual cost compared to quoted once known (source to confirm) - Export to Excel | MUST |
| B-13 | Dashboard | As an operations manager, I want to see routing outcomes and the transfer rate, so that I know how often orders move after the fact. - Orders routed per factory per day/week - Transfer rate: share of orders moved after allocation - Filter by factory, country and date | SHOULD |
| B-14 | Dashboard | As an operations manager, I want target vs actual factory share, so that I see whether allocation tracks the plan. - Target share vs actual share per factory for a chosen period - Gaps above a set threshold are highlighted | SHOULD |
| B-15 | Dashboard | As an operations manager, I want on-time delivery vs ETA and cost vs the cheapest feasible option, so that I measure routing quality. - On-time % against the ETA given at allocation - Cost per order vs the cheapest feasible option - Filter by factory, country and date | SHOULD |
| B-16 | Estimate endpoint | As any consuming system (cart, TG Supplier or others), I want a balancer endpoint that returns the likely factory and ETA for an item on request, so that allocation estimates come from one place. Cart pre-balancing itself is out of scope. - Endpoint takes an item, options and destination country - Returns the likely factory and ETA, without allocating - Same logic as the real routing decision | COULD |
| B-17 | Future | As the balancer, I want production success rate per factory as an input, so that quality also drives routing. - Success rate per factory and SKU, from reproduction events (B-20) - Can be switched on as a routing factor with a set weight | COULD |
| B-18 | Legacy retirement | As an operations manager, I want the legacy balancer retired at go-live, along with its country SKU lists and legacy factory-version aliases, so that there is one routing path. - Switch happens after shadow mode results are signed off - Legacy balancer, country SKU lists and factory-version aliases are turned off - A rollback plan exists for the first weeks | MUST |
| B-19 | Legacy retirement | As an operations manager, I want the new balancer to run in shadow mode alongside the legacy balancer, logging its decisions without acting on them, so that we compare outcomes before switching over. - New balancer decides every order in parallel but doesn't act - Each order logs both decisions and their cost and ETA - Ops gets a comparison report: agreement rate and estimated savings | MUST |
| B-20 | Production tracking | As the balancer, I want to consume TG Supplier domain events when an item is sent or reproduced (and TG Supplier to fire them), so that I know which quote was actually produced and can learn from it. - TG Supplier fires events when an item is sent and when it is reproduced - The balancer matches each event to the selected quote for that order item - Production time (order to sent) and success rate per factory feed back into routing decisions (B-17) | SHOULD |
| B-21 | Production tracking | As the balancer, I want to mark the selected quote as produced when its item is sent, and fire a domain event, so that BI and other consumers know the final factory and cost of every item. - Quote status runs quoted → selected → produced - On "sent", the selected quote becomes produced and `balancer.quote.produced` is fired; BI consumes it - A reproduction creates a new produced record linked to the original, counted separately - The event carries order item, factory, packing facility, quoted cost and sent date | MUST |
| B-22 | Daily plan | As a shift manager (factory manager permission), I want a daily plan of my factory's processes, built from the selected quotes, so that I know what's expected of my shift on a given day. - Shows items allocated to my factory for the chosen day that aren't produced yet - Grouped by process: items and quantity in the process unit, against that day's available capacity (P-13, P-14) - Processes over capacity are highlighted - Drill down to the order items behind each process: order ID, SKU, quote, due date - Limited to the user's own factory | MUST |
| B-23 | Transfers | As a factory manager, I want to find open items the way I do in TG Supplier today and move them to another supplier and production page, so that work moves when my factory can't produce it. - Search by order ID, supplier, page, event, site or due day; filters combine - Select results one by one, a whole page, or all that match; the selection is kept while paging - Pick the target supplier, a production page at that supplier, and a required reason (process offline, component OOS, over capacity, quality, customer request, other); a note is required for other - The balancer re-quotes each item at the target and shows the cost change and target capacity; on confirm the old quote is marked transferred, the new one selected and the item moves to that page - Factory managers transfer items from their own factory; operations from any supplier | MUST |
| B-24 | Transfers | As an operations manager, I want a transfer log with the reason and links to the production quotes, so that every move can be audited and debugged. - Each transfer logs when, who (user or balancer), order item, SKU, from and to supplier and page, reason and note - Links to the old and new production quote, with the cost and ETA change - Search and filter by supplier, page, order, reason, user and date - Transfers feed the transfer-rate KPI (B-13) | MUST |

## Decision logic

The balancer is a deterministic rules-and-scoring engine: the same input always gives the same decision, every step is written to the decision log (B-05), and scoring weights live in versioned config that ops can tune without a code change. It runs four steps per order:

1. **Filters** — drop factories that can't take the item: not producible, a CBD component out of stock with no "For balancer" alternative in stock there, a needed process at 0%, no shipping route to the destination, or a dropshipper / virtual item.
2. **Score** — rank what's left on total cost (production + shipping), plus a penalty for missing the promised ETA and for splitting the order, adjusted by the catalog's share and partial process capacity.
3. **Order-level pass** — try to keep all items of one order in one factory (B-06); with at most 4 factories and a few items per order, every combination can be checked.
4. **Business rules** — apply customer SLAs and event capacity reservations (B-09) on top, then pick the winner and emit the allocation event (B-10).

## Data schema: quotes and allocation

First draft for discussion. The new balancer service drives the flow: it asks the factory catalog for production quotes, then looks up shipping, then decides.

1. OCS sends the paid order item to the balancer, with its CBD (the components required by the customer's selection).
2. Balancer → factory catalog: quote request (A), synchronous.
3. Factory catalog → balancer: quote set (B), one quote per factory.
4. Balancer reads shipping from its daily cache of the shipment service (C), per producible factory.
5. Balancer picks the winner and emits the allocation event (D); the factory catalog marks that quote selected. When TG Supplier reports the item as sent, the balancer marks the quote produced and fires the produced event (E).

### A. Quote request — `POST /quotes`

| Field | Type | Notes |
| --- | --- | --- |
| requestId | uuid | Idempotency key |
| orderId, orderItemId | string |  |
| skuUuid | uuid | Or a legacy alias, resolved by the catalog |
| quantity | integer |  |
| cbd\[\] | {erpId, quantity} | Components required by the customer's selection, already resolved; the catalog checks stock and prices only these (B-02) |
| mode | live \| shadow \| estimate | Shadow for B-19, estimate for B-16 (nothing stored) |

```json
{
  "requestId": "b7e1c0d4-1f2a-4c55-9a10-3e2f9d8c7a61",
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
  "quantity": 1,
  "cbd": [
    { "erpId": "R.VM.BRD.08MM", "quantity": 1 },
    { "erpId": "STN.DIA.RND.002", "quantity": 1 },
    { "erpId": "CHN.VM.CABLE.45", "quantity": 1 }
  ],
  "mode": "live"
}
```

### B. Quote set — response

| Field | Type | Notes |
| --- | --- | --- |
| quoteKey | string | Identifies the whole set |
| validUntil | datetime |  |
| quotes\[\].quoteId | string | One per factory |
| quotes\[\].factoryId | string | Every factory is returned |
| quotes\[\].producible | boolean |  |
| quotes\[\].reasons\[\] | code | e.g. NOT\_PRODUCIBLE, COMPONENT\_OOS, PROCESS\_OFFLINE |
| quotes\[\].cost | {material, labor, packing, total, currency} | DIM cost basis (B-12) |
| quotes\[\].capacitySignalPct | 0–100 | From stock and process capacity |
| quotes\[\].sharePct | number | After redistribution (S-15) |
| quotes\[\].components\[\] | {erpId, quantity, unitCost, stockStatus, alternativeUsed} | The CBD components, priced and with stock status; alternativeUsed when a For balancer alternative stands in for an OOS component |
| quotes\[\].processes\[\] | {processId, quantity, capacityPct, laborCost} |  |

```json
{
  "quoteKey": "PQ-ORD456789-ITM001",
  "validUntil": "2026-11-23T12:15:00Z",
  "quotes": [
    {
      "quoteId": "PQ-ORD456789-ITM001-110",
      "factoryId": "110",
      "producible": true,
      "reasons": [],
      "cost": { "material": 38.2, "labor": 6.1, "packing": 1.4, "total": 45.7, "currency": "USD" },
      "capacitySignalPct": 80,
      "sharePct": 100,
      "components": [{ "erpId": "R.VM.BRD.08MM", "quantity": 1, "unitCost": 21.0, "stockStatus": "ok" }],
      "processes": [{ "processId": "ENGRAVING", "quantity": 2, "capacityPct": 80, "laborCost": 1.1 }]
    },
    {
      "quoteId": "PQ-ORD456789-ITM001-125",
      "factoryId": "125",
      "producible": false,
      "reasons": ["COMPONENT_OOS"],
      "sharePct": 0
    }
  ]
}
```

### C. Shipping lookup — balancer cache of the shipment service

Shape is owned by the shipment service; to confirm with that team.

```json
{
  "originFacilityId": "110",
  "destinationCountry": "DE",
  "cost": { "amount": 9.8, "currency": "USD" },
  "transitDays": { "min": 3, "max": 5 },
  "refreshedAt": "2026-11-23T01:00:00Z"
}
```

### D. Allocation event — `balancer.allocation.decided`

| Field | Type | Notes |
| --- | --- | --- |
| orderId, orderItemId | string |  |
| quoteKey, selectedQuoteId | string | Factory catalog marks it selected (B-10) |
| factoryId, packingFacilityId | string |  |
| productionCost, shippingCost, totalCost | {amount, currency} |  |
| eta | {from, to} date | Shown in on-time reports (B-15) |
| mode | live \| shadow \| fallback | Fallback per B-11 |
| candidates\[\] | {factoryId, totalCost, eta, excludedReason} | Why the winner won (B-05) |

```json
{
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "quoteKey": "PQ-ORD456789-ITM001",
  "selectedQuoteId": "PQ-ORD456789-ITM001-110",
  "factoryId": "110",
  "packingFacilityId": "110",
  "productionCost": { "amount": 45.7, "currency": "USD" },
  "shippingCost": { "amount": 9.8, "currency": "USD" },
  "totalCost": { "amount": 55.5, "currency": "USD" },
  "eta": { "from": "2026-11-30", "to": "2026-12-02" },
  "mode": "live",
  "candidates": [
    { "factoryId": "110", "totalCost": 55.5, "eta": "2026-12-02" },
    { "factoryId": "125", "excludedReason": "COMPONENT_OOS" }
  ]
}
```

### E. Produced event — `balancer.quote.produced`

| Field | Type | Notes |
| --- | --- | --- |
| orderId, orderItemId | string |  |
| quoteKey, quoteId | string | The quote that was actually produced (B-21) |
| status | produced | Quote status runs quoted → selected → produced |
| factoryId, packingFacilityId | string |  |
| quotedCost | {material, labor, packing, total, currency} | From the selected quote |
| sentAt | datetime | From the TG Supplier "sent" event |
| productionDays | number | Order to sent; feeds routing (B-17) |
| reproductionOf | quoteId \| null | Set when this is a reproduction, linked to the original |

```json
{
  "orderId": "ORD456789",
  "orderItemId": "ITM001",
  "quoteKey": "PQ-ORD456789-ITM001",
  "quoteId": "PQ-ORD456789-ITM001-110",
  "status": "produced",
  "factoryId": "110",
  "packingFacilityId": "110",
  "quotedCost": { "material": 38.2, "labor": 6.1, "packing": 1.4, "total": 45.7, "currency": "USD" },
  "sentAt": "2026-11-27T15:20:00Z",
  "productionDays": 4,
  "reproductionOf": null
}
```

## UI mockup

[Open the prototype](https://claude.ai/artifact/CP3Nfn2fR7aHUDBSRmaJFx). Screens for this module: Balancer → Routing dashboard, Balancer decision log, Daily plan and Transfers.
