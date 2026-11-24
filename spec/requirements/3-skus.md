<!-- Source: https://tenengroup.atlassian.net/wiki/pages/viewpage.action?pageId=5342953498 | title: Module 3: SKUs | version 4 (2026-11-24T13:01:51.965Z) | saved verbatim from Confluence (markdown export; images are not included) -->

# Module 3: SKUs

**Goal:** one place that shows how every SKU is made and where — components, processes and factory split — replacing the "producible SKUs" list in TG V2. The master catalog stays the source of truth; the factory catalog reads, never updates.

**Key features:**

- SKU sync from the master catalog (events or daily job, plus manual trigger), including dropshipper products
- SKU list and single view: components, processes, factories, history
- Global factory share with per-SKU overrides by factory managers
- Capacity (0%) check added to the legacy balancer

**Dependencies:** Module 1 (components), Module 2 (processes); master catalog team for the sync. Feeds Balancer (quotes, share).

**Business value:** the first holistic SKU view. The legacy balancer skips a factory only when a needed component is out of stock or a needed process is at 0%; the new balancer gets the data to quote. It does not yet replace the "Product info" sheet (Phase 2).

## User stories

| ID | Epic | User story | Priority |
| --- | --- | --- | --- |
| S-01 | Master catalog sync | As a factory catalog admin, I want SKUs imported read-only from the master catalog and kept in sync, by events if feasible or otherwise a daily job, so that the factory catalog never drifts from it. - New, changed and retired SKUs in the master catalog show up in the factory catalog - Synced fields are read only in the factory catalog - Last sync time shows in the UI; failures alert the admin | MUST |
| S-02 | Master catalog sync | As a factory catalog admin, I want each SKU keyed by the UUID from the master catalog, with its Site SKU as a live identifier and legacy factory-version SKUs (TGN/TGO/TGH) as read-only aliases, so that current consumers keep working. - Every SKU has one UUID, from the master catalog - The Site SKU is a real identifier used on the sites; it leads the SKU list and is searchable - Legacy Factory SKUs are read-only, deprecated aliases; no new factory-version clones are created | MUST |
| S-03 | Master catalog sync | As a factory catalog admin, I want to trigger a master catalog sync manually from the UI, so that I can pull urgent changes without waiting for the next run. - "Sync now" button for the factory catalog admin - Shows progress and result (updated, failed) - Can't start while a sync is already running | SHOULD |
| S-04 | SKU UI | As a factory manager, I want a SKU list searchable by any of its IDs, with column-header filters and export, so that I find a SKU however it's referenced. - Starts with Site SKU; then Factory SKU, SKU name, Type (Manufactured / Dropshipper), Status, Producible at, Validation - Tiles: Total SKUs, Dropshipper, Invalid - Search across UUID, Site SKU, Factory SKU and SKU name; Excel export | MUST |
| S-05 | SKU UI | As a factory manager, I want a Components tab on each SKU, so that I see what it's built from and what's in stock. - Lists mapped components with ERP ID, quantity and usage (Static or Dynamic) - Shows stock status per factory for each - Each component links to its component page | MUST |
| S-06 | SKU UI | As a factory manager, I want a Processes tab on each SKU, so that I see which machine and work processes it needs (a step like engraving can need both). - Lists mapped processes with type and quantity required in the process unit (no step order) - Shows current capacity per factory for each - Each process links to its process page | MUST |
| S-07 | SKU UI | As a factory manager, I want a Factories tab showing who can produce the SKU and each factory's share, so that I see the split at a glance. - Lists factories that can produce the SKU, with share % - Shows whether each share is default or overridden - Shares add up to 100% | MUST |
| S-08 | SKU UI | As a factory catalog admin, I want an Audit tab on each SKU, so that every mapping and share change is traceable. - Logs every sync, mapping and share change: who, when, old and new value - Newest first, read only | SHOULD |
| S-09 | Mapping | As a factory catalog admin, I want components mapped to each SKU, taken only from the master catalog as the single source of truth, so that components show their "SKUs using this" list. - Mappings come only from the master catalog - Each mapping carries quantity and usage: Static (always used) or Dynamic (depends on the customer's selection) - Each component page lists the SKUs that use it - Mapping changes arrive with the regular sync | MUST |
| S-10 | Mapping | As a factory catalog admin, I want SKU processes migrated from the Product info file and TG Supplier through export, manual fix and approval, then import, so that mappings start clean. - Export, manual fix and approval, then import - Each row carries the quantity required in the process unit (P-12) - Import validates rows and reports errors before saving - Only approved files can be imported | MUST |
| S-11 | Mapping | As a factory catalog admin, I want mappings validated (components exist in SAP, processes exist in the catalog), so that bad data never reaches the balancer. - Each mapped component must exist and be active in SAP - Each mapped process must exist in the catalog - Invalid SKUs are flagged and excluded from quotes until fixed | MUST |
| S-12 | Factory split | As a factory catalog admin, I want to set a global share between all factories on the Settings page, so that every SKU gets a default split. - Settings page, editable by the factory catalog admin only; total must be 100% - Every SKU without an override uses it - Changing it updates all default shares at once | MUST |
| S-13 | Factory split | As a factory manager, I want to mark which SKUs my factory can produce, so that the producible-SKU list in TG V2 can be retired. - Factory share screen: a "Producible" checkbox per SKU and factory; the factory manager edits only their own (highlighted) column - Bulk select and Excel import supported - Non-producible factories get 0% share for that SKU | MUST |
| S-14 | Factory split | As a factory manager, I want to override the default share for a specific SKU myself (share is set per SKU only, not per model), so that I can steer volume for reasons outside capacity or stock. - Factory managers set overrides themselves, no approval step - Defaults show grey, overrides blue; per-row "Reset to default" - Shares must total 100%; changes auto-save | MUST |
| S-15 | Factory split | As the balancer, I want a factory's share to flow to the others when it can't produce a SKU, so that shares always add up to 100%. - When a factory drops out, its share is split among the others in proportion to their shares - Stored shares don't change; only the share used for the order does | MUST |
| S-16 | Legacy support | As the country balancer, I want the existing SKU lists fed from the new data, so that current routing keeps working during the transition. - Current SKU lists keep their format and are generated from the new data - Legacy factory IDs resolve through the aliases - Lists are retired with Module 4 | MUST |
| S-17 | Legacy balancer | As the legacy balancer, I want to skip a factory only when an OOS component is required for that order, so that optional parts don't block orders. - Static components always count; Dynamic ones count only if they're in the order's CBD - A counted OOS component skips that factory, unless an alternative flagged For balancer is in stock there - Each skip is logged with the component | MUST |
| S-18 | Legacy balancer | As the legacy balancer, I want to skip a factory when a process the SKU needs is at 0%, so that capacity outages stop orders. This is the capacity check Module 3 adds to the legacy balancer. - A mapped process at 0% skips that factory - Partial capacity doesn't affect the legacy balancer - Each skip is logged with the process | MUST |
| S-19 | Master catalog sync | As a factory catalog admin, I want dropshipper products imported from the master catalog as a limited record that includes total cost, so that the balancer and finance see their cost without components or processes. - Dropshipper products sync with ID, name, dropshipper and total cost - No components, processes or factory share - Marked as dropshipper in the list and excluded from factory routing | MUST |
| S-20 | Production feedback | As a factory catalog admin, I want SKUs tagged "Production conflict" when the production floor used different processes or machines than the ones assigned, so that the catalog's mapping matches how items are really made. - On "sent", TG Supplier fires the full production log to the catalog: each process and machine type actually used - The catalog compares it with the SKU's assigned processes and that factory's machine type for each - Any difference tags the SKU "Production conflict" for that factory, listing what was expected and what was used - The tag shows in the SKU list and the SKU header; the Processes tab lists each process used but not listed, with machine, order ID, factory and day. It clears once the mapping is fixed or later logs match - Delivered with Module 4, once TG Supplier fires the events | SHOULD |

## Data schema: SKU

First draft for discussion. Master catalog fields are read only; factory fields are owned by the factory catalog.

| Field | Type | Source | Notes |
| --- | --- | --- | --- |
| skuUuid | uuid | Master | Canonical key (S-02) |
| siteSku | string | Master | Live identifier used on the sites (S-02) |
| aliases\[\] | {id, type: factory, deprecated} | Master | Legacy factory-version SKUs, read only |
| name | string | Master | SKU name; there is no separate product name |
| productType | manufactured \| dropshipper | Master |  |
| status | active \| retired | Master |  |
| components\[\] | {erpId, quantity, usage: static \| dynamic} | Master | Only source of mappings (S-09) |
| processes\[\] | {processId, quantity} | Migration | Quantity in the process unit (S-10, P-12) |
| factories\[\].factoryId | string | Catalog |  |
| factories\[\].producible | boolean | Factory manager | Own factory only (S-13) |
| factories\[\].defaultSharePct | number | Derived | From global share |
| factories\[\].overrideSharePct | number \| null | Factory manager | Per SKU; totals 100% (S-14) |
| factories\[\].overrideBy / At | string / datetime | System | Audit |
| dropshipper | {dropshipperId, totalCost {amount, currency}} | Master | Dropshipper SKUs only (S-19) |
| validation | {status: valid \| invalid, issues\[\]} | Derived | Invalid SKUs get no quotes (S-11) |
| lastMasterSync | datetime | System |  |
| productionConflict | {status: none \| conflict, issues\[{factoryId, expected, actual, orderItemId, seenAt}\]} | Derived | From the TG Supplier production log (S-20) |

Global share is one setting on the Settings page, edited by the factory catalog admin (S-12): `{ "factoryShares": [{ "factoryId": "110", "pct": 60 }, { "factoryId": "125", "pct": 40 }] }`.

```json
{
  "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90",
  "siteSku": "TGI-N-4609-SD-VMGP_1.1",
  "aliases": [
    { "id": "TGN-N-4609-SD-VMGP_1.1", "type": "factory", "deprecated": true }
  ],
  "name": "Name necklace with diamond, gold vermeil",
  "productType": "manufactured",
  "status": "active",
  "components": [
    { "erpId": "R.VM.BRD.08MM", "quantity": 1, "usage": "static" },
    { "erpId": "STN.DIA.RND.002", "quantity": 1, "usage": "static" },
    { "erpId": "CHN.VM.CABLE", "quantity": 1, "usage": "dynamic" }
  ],
  "processes": [
    { "processId": "CUTTING", "quantity": 4 },
    { "processId": "PLATING", "quantity": 12 },
    { "processId": "SETTING", "quantity": 1 },
    { "processId": "PACKING", "quantity": 1 }
  ],
  "factories": [
    { "factoryId": "110", "producible": true, "defaultSharePct": 60, "overrideSharePct": 70, "overrideBy": "fm.110", "overrideAt": "2026-11-18T09:00:00Z" },
    { "factoryId": "125", "producible": true, "defaultSharePct": 40, "overrideSharePct": 30 }
  ],
  "validation": { "status": "valid", "issues": [] },
  "productionConflict": {
    "status": "conflict",
    "issues": [{ "factoryId": "125", "expected": "ENGRAVING on LASER", "actual": "ENGRAVING on CNC", "orderItemId": "ITM001", "seenAt": "2026-11-24T09:12:00Z" }]
  },
  "lastMasterSync": "2026-11-23T02:10:00Z"
}
```

## UI mockup

[Open the prototype](https://claude.ai/artifact/CP3Nfn2fR7aHUDBSRmaJFx). Screens for this module: Configurations → SKU (list and detail tabs) and Overview → Factory share.
