<!-- Source: https://tenengroup.atlassian.net/wiki/pages/viewpage.action?pageId=5342363692 | title: Module 2: Process capacity | version 5 (2026-11-24T13:01:01.789Z) | saved verbatim from Confluence (markdown export; images are not included) -->

# Module 2: Process capacity

**Goal:** let factory managers declare the capacity of each process in 10% steps, from 0% (offline) to 100% (full).

**Key features:**

- One process list with two kinds: machine processes (one per machine type) and work processes (team capacity, no machine); a step like engraving can need both
- Per-factory setup by each factory manager: runs here, machine type, labor cost
- Max daily capacity per process and factory (what 100% means), manual capacity % by factory managers, and today's used and remaining capacity
- Labor and packing cost per process unit and factory; process quantity per SKU

**Dependencies:** Module 1 setup (users, roles). Feeds SKUs (process mapping) and Balancer (capacity signal).

**Business value:** once Module 3 maps processes to SKUs, factories stop receiving orders they can't handle because a machine is down or a shift is short — fewer late orders and manual transfers.

## User stories

| ID | Epic | User story | Priority |
| --- | --- | --- | --- |
| P-01 | Work processes | As a factory catalog admin, I want to create and edit machine processes (one per machine type) and work processes (team capacity, no machine), so that the catalog holds one process list covering both. - Each process has an ID, name, type (machine or work), "Measured in" unit, status and description - Measured in: minutes, seconds, hours, units, pieces, cm, mm, cm² or mm² - Admin can create, edit and deactivate processes; nothing is hard-deleted - Process names are unique | MUST |
| P-02 | Work processes | As a factory manager, I want to set whether my factory runs each process and which machine type it uses (it can differ between factories), so that capacity is only asked where it applies. - Factory manager sets Runs here for their own factory only; other factories' rows are greyed - For machine processes, the factory manager picks the machine type - Capacity fields show only for processes the factory runs | MUST |
| P-03 | Work processes | As a factory manager, I want a process list and single view with the same layout as components, so that the UI is familiar. - List starts with Process ID; columns: Process, Type, Machine type, Factories running it, Lowest capacity today, Avg labor cost, SKUs using - Detail tabs: Specifications, Factories, SKUs using this, Audit - "SKUs using this" shows SKU name, Site SKU and quantity required in the process unit | MUST |
| P-04 | Work processes | As a factory catalog admin, I want to import the initial process list from TG Supplier and the "Pricing Tool" sheet, so that we don't start from zero. - Export, manual fix and approval, then import - Import validates each row and reports errors before saving - Import can run again without creating duplicates | SHOULD |
| P-05 | Work processes | As a factory manager, I want to set my factory's labor cost per process unit, so that the quote engine can price labor in Module 4. - Labor cost per process unit (e.g. per minute), with currency, per factory - Factory manager edits only their own factory; finance and others view - Cost changes are logged | SHOULD |
| P-06 | Capacity indicators | As a factory manager, I want to set each process's capacity in 10% steps from 0% to 100%, so that I can throttle my factory without removing SKUs. Only factory managers can change capacity, and it changes only manually: it never expires or auto-resets. - Values allowed: 0, 10, 20 … 100% of the max daily capacity (P-13); default 100% - Only the factory manager of that factory can change it - Value stays until changed by hand - New value is available to the balancer right away | MUST |
| P-07 | Capacity indicators | As a factory manager, I want to add a reason and an expected recovery date (for information only) to a reduced capacity, so that others know why and until when. - Reason and recovery date are optional fields on any value below 100% - Recovery date does not change capacity automatically - Both show wherever the capacity shows | SHOULD |
| P-08 | Capacity indicators | As an operations manager, I want a capacity overview of all processes across factories, so that I spot bottlenecks at a glance. - Grid of processes × factories; each cell has a capacity bar and %: green 100%, amber reduced, red 0% - Reduced and offline cells show "until \<date\>"; hovering shows the reason - "Doesn't run here" where a factory doesn't run the process - Operations manager switches between All factories and By factory; other roles see the grid read only | SHOULD |
| P-09 | Capacity indicators | As a factory catalog admin, I want every capacity change logged with user and time, so that routing decisions can be audited. - Every change logs user, time, old and new value, reason - Log is read only and visible in the process Audit tab | MUST |
| P-10 | Balancer | As the legacy balancer, I want a process at 0% to block the SKUs that need it in that factory, so that no order goes to a factory that can't build it. Delivered with Module 3, once SKU–process mapping exists. - A process at 0% in a factory blocks every SKU that needs it, in that factory only - Partial values (10–90%) don't affect the legacy balancer - Blocks lift as soon as capacity goes above 0% | MUST |
| P-11 | Work processes | As a factory catalog admin, I want packing defined as a process with its cost per factory, so that packing cost is part of every production quote. - Packing exists as a work process with a cost per factory - Packing cost can vary by item size or complexity (rules to confirm) - Packing cost is included in every production quote | MUST |
| P-12 | Work processes | As a factory catalog admin, I want each SKU–process mapping to carry the quantity the SKU needs in the process unit (e.g. 2 minutes of engraving), so that labor cost can be priced per SKU. - The unit is defined once on the process ("Measured in") - Each SKU–process mapping stores the quantity required in that unit - Quantity shows in the process "SKUs using this" tab and the SKU Processes tab - Labor cost in a quote = quantity × that factory's labor cost per unit | MUST |
| P-13 | Capacity indicators | As a factory manager, I want to set my factory's maximum daily output for each process (what 100% means), so that capacity percentages translate into real volume. - Set on the process Factories tab, per factory, by that factory's manager - Default unit: order items per day - 100% is this maximum, e.g. 1,000 a day in a factory with 4 machines, 200 in a smaller one - The capacity % scales it: 50% of a 20-a-day maximum leaves 10 a day available | MUST |
| P-14 | Capacity indicators | As a factory manager, I want to see today's used and remaining capacity for each process, so that I know how much more my factory can take. - Used today = order items that needed the process and were already done today; items balanced but not done yet show in the daily plan (B-22) - Shown under the capacity slider: "\<used\> of \<available\> today · \<remaining\>% left" - Remaining % = (max × capacity % − used) ÷ max. Max 100, capacity 100%, used 20 → 80% left; max 20, capacity 50%, used 5 → 25% left - The balancer routes on remaining capacity, not only the set % (B-08) | MUST |

Partial capacity (e.g. 60%) only affects routing in the new balancer — see the Balancer page.

## Data schema: Process

First draft for discussion. Specifications are edited by the factory catalog admin; per-factory fields (runs here, machine type, labor cost, capacity) only by the factory manager of that factory.

| Field | Type | Edited by | Notes |
| --- | --- | --- | --- |
| processId | string | Admin | Unique key, e.g. ENGRAVING |
| name | string | Admin | Unique (P-01) |
| type | machine \| work | Admin | Work = team capacity, no machine |
| measuredIn | minutes \| seconds \| hours \| units \| pieces \| cm \| mm \| cm² \| mm² | Admin | Process unit; SKU quantities and labor cost use it (P-01, P-12) |
| description | string | Admin |  |
| status | active \| inactive | Admin | Never hard-deleted |
| factories\[\].factoryId | string | System |  |
| factories\[\].runsHere | boolean | Factory manager | Capacity is asked only where true (P-02) |
| factories\[\].machineType | string | Factory manager | Machine processes only; can differ per factory |
| factories\[\].capacityPct | 0–100, step 10 | Factory manager | Default 100; manual only (P-06) |
| factories\[\].capacityNote | {reason, expectedRecovery} | Factory manager | Information only (P-07) |
| factories\[\].capacityUpdatedBy / At | string / datetime | System | Audit (P-09) |
| factories\[\].laborCost | {amountPerUnit, currency, effectiveFrom} | Factory manager | Per process unit (P-05) |
| lastUpdate | datetime | System |  |
| factories\[\].maxPerDay | number (order items per day) | Factory manager | What 100% means (P-13) |
| factories\[\].today | {available, used, remainingPct} | Derived | available = maxPerDay × capacityPct; remainingPct = (available − used) ÷ maxPerDay (P-14) |

Packing is a work process with processId PACKING; how its cost varies by item size is still to confirm (P-11).

Process scheme suggestion - sample process

```json
{
  "processId": "ENGRAVING",
  "name": "Engraving",
  "type": "machine",
  "measuredIn": "minutes",
  "description": "Text engraving for personalization",
  "status": "active",
  "factories": [
    {
      "factoryId": "110",
      "runsHere": true,
      "machineType": "LASER",
      "capacityPct": 80,
      "capacityNote": { "reason": "1 of 5 lasers down", "expectedRecovery": "2026-11-28" },
      "capacityUpdatedBy": "fm.110",
      "capacityUpdatedAt": "2026-11-22T07:40:00Z",
      "laborCost": { "amountPerUnit": 0.55, "currency": "USD", "effectiveFrom": "2026-01-01" },
      "maxPerDay": 1000,
      "today": { "available": 800, "used": 200, "remainingPct": 60 }
    },
    {
      "factoryId": "315",
      "runsHere": true,
      "machineType": "CNC",
      "capacityPct": 100,
      "laborCost": { "amountPerUnit": 0.7, "currency": "USD", "effectiveFrom": "2026-01-01" }
    }
  ],
  "lastUpdate": "2026-11-22T07:40:00Z"
}
```

## UI mockup

[Open the prototype](https://claude.ai/artifact/CP3Nfn2fR7aHUDBSRmaJFx). Screens for this module: Configurations → Processes (list and detail tabs) and Overview → Process capacity. Switch to Operations manager to see the all-factories grid.
