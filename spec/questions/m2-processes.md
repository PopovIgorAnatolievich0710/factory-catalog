# Module 2 Process capacity — assumptions and questions

Each row was implemented with the recommended variant.

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| M2-1 | P-11: how does packing cost vary by item size or complexity? | Flat cost per factory: `PACKING` uses the ordinary labor-cost fields of `process_factory` (amount per unit + currency) | size / complexity tiers per factory; a cost per packing type on the SKU |
| M2-2 | P-14: in which unit is "used today" counted? Max per day is "order items per day" | Order items: sum of `quote_set.quantity` of quotes produced today that need the process | process units (item quantity × `sku_process.quantity`), with max per day in process units |
| M2-3 | P-14: which quotes count as used? | Only `Produced` with `produced_at` today; `Selected` (balanced, not done) is left to the daily plan (B-22), as the story says | also subtract `Selected` quotes so that remaining capacity reflects work already routed |
| M2-4 | P-14: what is "today"? | The current UTC day | the factory's local day (needs a time zone per factory) |
| M2-5 | P-14: where do the processes of a produced item come from? | `quote.processes[].processId` (shape of the Balancer requirement page); when a quote stores none, the SKU's current `sku_process` mapping | only the stored quote; the reported `production_log` |
| M2-6 | P-14: remaining % when used exceeds available | Clamped to 0; `used` is reported as is | negative remaining; cap `used` at `available` (mockup) |
| M2-7 | P-14: no max per day set | `today` is not shown ("Maximum daily output is not set") | treat as unlimited; require max per day before "Runs here" |
| M2-8 | P-09: history has no reason column | The capacity entry's new value carries it (`80% (reason: …)`); reason and recovery changes are also logged as their own fields | add a `note` column to `history_entry` |
| M2-9 | P-07: reason / recovery when capacity returns to 100% | Cleared | kept as the last note |
| M2-10 | P-03: "Avg labor cost ($)" needs currency conversion; there is no FX source | Average in the factories' own currency, shown with the code; empty when factories use different currencies | FX table and conversion to USD; labor cost always in USD |
| M2-11 | P-05: `laborCost.effectiveFrom` | Set by the system to the day the cost changes; no cost history besides the audit log | editable date; dated cost rows |
| M2-12 | P-05: currency input | Free 3-letter code, default USD in the UI | fixed list; one currency per factory |
| M2-13 | P-01: who chooses the process ID? | Optional on create; derived from the name when empty; upper-case letters, digits, `_ - .`; not editable afterwards | always generated (`PRC-001`); always typed |
| M2-14 | P-01: can `PACKING` be deactivated or turned into a machine process? | No (422), because every quote needs it | no restriction |
| M2-15 | P-01: type changed from machine to work | Machine types of its factories are cleared (logged) | reject while machine types exist |
| M2-16 | P-02: values while "Runs here" is off | Stored values (machine type, cost, max, capacity) are kept; capacity cannot be edited | clear the row |
| M2-17 | P-04: format of the TG Supplier / "Pricing Tool" export | Own two-sheet format (`Processes`, optional `Factories`), see the module spec | adapt to the real export once a sample exists |
| M2-18 | P-04: the admin imports per-factory setup that only factory managers may edit | Allowed for the initial load (permission `ProcessesEdit`, history source `Import`) | process sheet only; own-factory check per row |
| M2-19 | P-04: partial import | Blocked while any row has an error | import valid rows, skip the rest |
| M2-20 | P-04: empty optional cells on re-import | Status empty = keep; other empty cells clear the value | empty = keep everywhere |
| M2-21 | P-08: which permission lets a user switch All factories / By factory? | Role check in the UI (operations manager, BO administrator); the API returns both views to anyone with `ProcessesView` | a `Processes.ViewCapacityByFactory` permission |
| M2-22 | P-08: inactive processes and factories | Hidden from the capacity views; inactive processes stay in the Processes list | show greyed |
| M2-23 | P-03: "SKUs using this" for a process used by every SKU (packing) | Returned whole inside `GET /v1/processes/{id}` and filtered in the browser | a paged endpoint for the tab |
| M2-24 | "Last changed by <name>" | The user's email (what `capacity_updated_by` stores) | display name (needs a user directory) |
