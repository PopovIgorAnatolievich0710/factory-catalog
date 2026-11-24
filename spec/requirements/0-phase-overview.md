<!-- Source: https://tenengroup.atlassian.net/wiki/pages/viewpage.action?pageId=5342429238 | title: Phase overview | version 4 (2026-11-24T13:03:45.747Z) | saved verbatim from Confluence (markdown export; images are not included) -->

## Purpose & scope

Phase 1 delivers a basic factory catalog plus a new balancer, shipped as four modules: Components, Process capacity, SKUs and Balancer. Each module goes live on its own and brings value on its own; each builds on the one before.

**In scope (Phase 1):**

- Components read-only from SAP, with stock per factory, alternatives and manual OOS flags
- Work processes and per-factory capacity indicators
- A SKU view that replaces the "producible SKUs" list (but not yet the "Product info" sheet), with components, processes and factory split, plus dropshipper products as a limited record with total cost
- A balancer that routes each order on total cost (production + shipping) and delivery time

**Out of scope (later phases):**

- Phase 2: models (groups of SKUs that share one design but differ in materials and option categories), SKU auto-generation, migrating the "Product info" sheet
- Phase 3: steps-based workflow, approvals and create/update streams to the master catalog.

**Business targets:** about $230K–$1.29M a year in savings plus about $120K in additional sales (Factory Catalog KPI slide). Each module page lists its own goal.

## Module map

The four modules ship in order, with 76 first-draft user stories across them.

| # | Module | Goal | Stories |
| --- | --- | --- | --- |
| 1 | Components | Component-level stock and OOS from SAP, plus approved alternatives | 18 |
| 2 | Process capacity | Factory managers declare capacity per work process, 0–100% | 14 |
| 3 | SKUs | One view of how and where each SKU is made; replaces "producible SKUs" | 20 |
| 4 | Balancer | Route every order on total cost and delivery time; exact DIM cost | 24 |

## Personas

Eight roles use or feed Phase 1; user stories in every module page refer to them by these names.

| Persona | Who | Main modules |
| --- | --- | --- |
| Factory manager | Runs one factory; owns its process setup and capacity, OOS flags and SKU share | Components, Process capacity, SKUs |
| Shift manager | Runs a shift in one factory, with factory manager permission; needs the daily plan of what's expected | Process capacity, Balancer |
| Supply chain manager | Watches stock across factories and critical levels, component alternatives | Components |
| Factory catalog admin | Maintains process specifications and the global share | Components, Process capacity, SKUs |
| Production floor user (TG Supplier) | View only | Components |
| Operations / GO manager | Monitors routing outcomes, transfers and cost | Balancer |
| Balancer (system) | Requests quotes and routes each order | Components, Process capacity, SKUs, Balancer |
| Finance | Monitors cost | Components, Process capacity, SKUs |

Product designers will be added from phase 2.

**Permissions (draft):** Phase 1 uses Active Directory roles. Proposed edit rights per persona, to confirm at discovery:

| Persona | Components | Process capacity | SKUs | Balancer |
| --- | --- | --- | --- | --- |
| Factory manager | Edit OOS flag (own factory) | Edit capacity, runs here, machine type, labor cost (own factory) | Edit producible SKUs, share override (own factory) | View |
| Supply chain manager | Edit critical levels, alternatives and their use flags, costs | View | View | View |
| Factory catalog admin | View, monitor SAP sync | Edit process specifications, new processes | Edit global share (Settings) | View |
| Production floor user | View | View | View | View |
| Operations / GO manager | View | View | View | View dashboards |
| Finance | View cost | View cost | View | View DIM cost |

## Cross-module dependencies

SAP and factory managers feed Modules 1 and 2; Module 3 joins them with SKUs read from the master catalog; Module 4 turns that into per-factory quotes and adds shipping routes to pick the winner.

## UI mockup

The interactive Phase 1 prototype: [Factory catalog — UI mockup](https://claude.ai/artifact/CP3Nfn2fR7aHUDBSRmaJFx). Phase 1 is the default view; switch personas with "Acting as" in the top bar. Navigation follows the module flow: Configurations (Components, Processes, SKU), Overview (Component stock levels, Process capacity, Factory share) and Balancer (Routing dashboard, Balancer decision log, Daily plan, Transfers).

## Dependencies on other teams

Phase 1 needs extra work from teams outside the project; each needs scheduling with that team.

| Team | What they provide | Needed for |
| --- | --- | --- |
| SAP team | Daily S3 export of the component master and stock per factory (warehouse and production floor) | Module 1 |
| Master catalog team | SKU sync to the factory catalog — events, API or a daily job, plus support for a manual trigger from the UI — including dropshipper products | Module 3 |
| OCS team | OCS to provide order with CBD based on customer selection | Module 4 |
| Shipment service team | Cached shipping routes, costs and timeframes | Module 4 |
| TG Supplier team | Domain events fired when an item is sent or reproduced, including the full production log (processes and machines actually used) | Module 4 |
| BI team | Consume the balancer's allocation and quote-produced events | Module 4 |

## Discovery Qs

The cross-module decisions below block story sizing.

- SAP export: confirm S3 file format, data scheme, schedule, owner of the export job
- Platform (tech decision): does the factory catalog live in TG Supplier or in the back office, for UI and user management? Supplier users are not managed in Active Directory, which affects the AD-based permissions plan.
- Master catalog sync: migration + event-based updates (preferred), or a daily job that pulls all updates + manual trigger from the UI for urgent updates?

## Business stakeholder Qs

Questions for business owners rather than the delivery team.

- Component unit cost: take it from SAP purchase terms or from the "Pricing Tool" sheet? (Components, C-10)

## Decisions

| Date | Topic | Decision |
| --- | --- | --- |
| Nov 22, 2026 | Legacy vs new balancer | Module 1 adds only the OOS check to the legacy balancer; Module 3 adds the capacity (0%) check; the legacy balancer is retired with Module 4. |
| Nov 22, 2026 | Users and permissions | Active Directory roles in Phase 1; edit rights per persona drafted under Personas. |
| Nov 22, 2026 | Dropshipper products | Imported from the master catalog in Module 3 as a limited record that includes total cost. |
| Nov 22, 2026 | Packing cost | Stays in the factory catalog as a process. |
| Nov 23, 2026 | Naming | We talk about SKUs only; there is no separate product name. Models (groups of SKUs that share one design but differ in materials and option categories) come in a later phase. |
| Nov 23, 2026 | SKU identity | The UUID from the master catalog is the key. The Site SKU stays a live identifier used on the sites. Legacy factory-version SKUs are deprecated aliases. |
| Nov 23, 2026 | Process setup | Factory managers edit runs here, machine type and labor cost for their own factory; the factory catalog admin edits process specifications. |
| Nov 23, 2026 | Alternatives use | For balancer: if the original component is OOS in a factory but the alternative is in stock there, the factory can still get the order. For S.C. planner: used in the TG Supplier replacement flow (FAST-2090). |
| Nov 23, 2026 | Settings page | Settings holds the global factory share only. Sync status has no UI; "Sync now" stays on the SKU list. |
| Nov 23, 2026 | UI conventions | Defined once in Module 1 setup (C-02) and applied across the service. |
| Nov 23, 2026 | SKU processes | No step order on SKU processes; each mapping carries the quantity in the process unit. |
| Nov 24, 2026 | Production feedback | The balancer owns the decision and the quotes, so it consumes TG Supplier's sent and reproduced events, marks the quote produced and fires an event that BI consumes; the data feeds back into routing. |
| Nov 24, 2026 | Reproductions | A reproduced item creates a new produced record linked to the original; BI counts it separately. |
