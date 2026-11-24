<!-- Source: https://tenengroup.atlassian.net/wiki/pages/viewpage.action?pageId=5342429216 | title: Module 1: Components | version 5 (2026-11-24T13:00:15.290Z) | saved verbatim from Confluence (markdown export; images are not included) -->

# Module 1: Components

**Goal:** make components the catalog's first entity, read-only from SAP with stock per factory, so out-of-stock is tracked per component instead of per SKU.

**Key features:**

- Daily SAP sync of components and stock (warehouse and production floor)
- Component list and single view, with critical stock levels
- Manual OOS flags, with an "OOS conflict" alert
- Approved alternatives for OOS replacement in TG Supplier
- OOS check in the legacy balancer

**Dependencies:** none — this is the first module. The SAP team provides the daily export. Feeds SKUs (component mapping) and Balancer (OOS check).

**Business value:** fewer orders routed to a factory that can't produce them, fewer manual transfers, replacements recorded correctly in SAP consumption (MCM), better stock control for supply chain and factory managers.

## User stories

| ID | Epic | User story | Priority |
| --- | --- | --- | --- |
| C-01 | Setup | As a factory catalog admin, I want the project infrastructure, SSO login and role permissions in place, so that each persona sees and edits only what they should. - Users sign in with company SSO - Each persona gets the edit rights from the permissions table - A user with no role can't open the catalog | MUST |
| C-02 | Setup | As a factory catalog admin, I want one set of UI conventions on every screen, so that the service looks and behaves the same everywhere. - Lists start with the record's ID; detail pages use tabs, with Specifications as a two-column table and Audit as the change-log tab - Every table column has a header filter (value list, number operators or text contains) and sort - A lock tag in each card header states who edits and the data source, e.g. "Read only · Source: SAP" - Everyone sees the same controls: active for the persona allowed to edit, greyed for others - Tables with editable cells keep fixed column widths and row heights | MUST |
| C-03 | SAP sync | As a supply chain manager, I want the component master (ERP IDs, active/inactive) synced daily from SAP via S3, so that the catalog always matches SAP. - Job runs daily (proposed 02:00) and reads the SAP export from S3 - New ERP IDs are added, changed ones updated, inactive ones marked inactive (never deleted) - SAP fields can't be edited in the catalog - Last successful sync time shows in the UI | MUST |
| C-04 | SAP sync | As a supply chain manager, I want stock levels per factory, split between warehouse and production floor, synced daily, so that I see real availability. - Stock is stored per component, factory and location (warehouse or production floor) - Each sync replaces the previous snapshot - Stock shows when it was last synced | MUST |
| C-05 | Component UI | As a supply chain manager, I want a component list with search, column-header filters, a column picker and Excel export, so that I find any component fast. - Starts with ERP ID; default columns Name, Family, Status, Total stock, Critical level, SKUs using - Column picker adds up to 8 columns (e.g. Material, Unit cost, stock per factory); the choice is remembered - Status shows the worst status across factories and which factories - Search on ERP ID and name; Excel export of the current view, named page + timestamp | MUST |
| C-06 | Component UI | As a factory manager, I want a Stock tab showing stock per factory, warehouse and production floor, so that I know what I can produce today. - Columns: Factory, Warehouse, Floor, Total, Critical level (global or override), Status, OOS flag, Note - Highlights factories below critical level or OOS - Shows last sync time | MUST |
| C-07 | Component UI | As a supply chain manager, I want an Alternatives tab listing approved replacement components and what each may be used for, so that OOS can be resolved quickly. - Columns: ERP ID, Name, stock per factory, Use - Use has two flags: For balancer and For S.C. planner - Supply chain manager edits; others see the flags greyed | MUST |
| C-08 | Component UI | As a supply chain manager, I want a "SKUs using this" tab, so that I see which SKUs a shortage affects (filled in Module 3). - Lists every SKU using the component: SKU name, Site SKU, quantity and usage (Static or Dynamic), linked to the SKU - Only manufactured SKUs appear - Shows an empty state until Module 3 is live | SHOULD |
| C-09 | Component UI | As a supply chain manager, I want to set a critical stock level per component, with an optional override per factory, so that low stock is flagged before it runs out, even where factories stock differently. - One global critical level per component - Optional override per factory, which wins over the global value - Component is flagged "low stock" in a factory when stock drops below its level | MUST |
| C-10 | Component UI | As a factory catalog admin, I want to see the unit cost of each component, so that DIM cost can be built later. - Shows unit cost and currency in the Specifications table - View only in Phase 1 - Source still open (see Business stakeholder Qs) | COULD |
| C-11 | Component UI | As a factory catalog admin, I want an Audit tab logging every change and sync, so that I can see who changed what. - Logs every user change and sync: who, when, field, old and new value - Newest first, read only | SHOULD |
| C-12 | Manual OOS | As a factory manager, I want to mark a component out of stock in my factory and clear it myself (an SAP sync never clears it), so that orders stop arriving for items I can't build. - OOS checkbox on each factory row, active only on the factory manager's own factory - Ticking flags it at once and opens an inline note, shown as "Set by \<name\> · \<date\>"; the owner can edit it - Unticking clears the flag; the SAP sync never clears it - The flag reaches the list, single view and balancer right away | MUST |
| C-13 | Manual OOS | As a supply chain manager, I want a view of components below critical level or OOS across factories, so that I act before orders are affected. - Shows components below critical level or OOS, per factory - Filter by factory and status - Export to Excel | SHOULD |
| C-14 | Manual OOS | As a factory manager, I want the component list to flag components that need action, including "OOS conflict" when stock is above the critical level but a manual OOS flag is still set, so that stale flags get cleared. - "Action required" marker on rows that are OOS or OOS conflict - OOS conflict shows on the status chip; its tooltip explains why - Clears when the flag is removed or stock drops below critical | MUST |
| C-15 | Alternatives | As a supply chain manager, I want to maintain approved alternatives per component, global and one-way (B replacing A does not let A replace B), and flag what each may be used for, starting with the top 100 SKUs, so that replacements come from a vetted list. - Alternatives apply to all factories and are one-way - For balancer: if the original is OOS in a factory but this alternative is in stock there, the factory can still get the order - For S.C. planner: offered in the TG Supplier replacement flow (FAST-2090) - An alternative must be an active ERP ID in SAP - First release covers components of the top 100 SKUs | MUST |
| C-16 | Alternatives | As a production floor user, I want to pick a replacement from approved ERP IDs in TG Supplier (FAST-2090), so that I don't search every component and SAP consumption stays correct. - The replacement screen lists only alternatives flagged For S.C. planner - Shows each alternative's stock in the user's factory - The chosen replacement is recorded so SAP consumption (MCM) is correct | MUST |
| C-17 | Balancer OOS check | As the legacy balancer, I want to check an order's CBD components (taken from the current flow, unchanged) against OOS per factory before allocating, so that I skip factories that can't build it. This is the only check Module 1 adds to the legacy balancer. - Uses the order's CBD from the current flow - Skips a factory if a CBD component is OOS there and no alternative flagged For balancer is in stock there - Moves on to the next factory in the current order - If no factory qualifies, keeps current behavior and logs it | MUST |
| C-18 | Balancer OOS check | As an operations manager, I want to see why a factory was skipped, so that I can trust and debug routing. - Each skip is logged with order, factory and OOS component - Log is searchable by order ID | SHOULD |

## Data schema: Component

First draft for discussion. SAP fields are read only; the rest is owned by the factory catalog.

| Field | Type | Source | Notes |
| --- | --- | --- | --- |
| erpId | string | SAP | Primary key |
| name | string | SAP |  |
| status | active \| inactive | SAP | Inactive is never deleted (C-03) |
| family | string | SAP | e.g. Chain, Clasp |
| material | string | SAP | e.g. Sterling Silver 925 |
| measure | {measuredBy, value, unit} | SAP | e.g. length, 18, in |
| unitOfMeasure | {type, quantity, unit} | SAP | Stock unit, e.g. weight, 1, kg |
| countryOfOrigin, hsCode | string | SAP | For customs; confirm if needed in Phase 1 |
| unitCost | {amount, currency, source} | TBD | Source open (C-10) |
| criticalStockLevel | {global, factoryOverrides\[\]} | Catalog | Override per factory wins (C-09) |
| alternatives\[\] | {erpId, forBalancer, forPlanner} | Catalog | Global, one-way; supply chain manager edits (C-07, C-15) |
| factories\[\].factoryId | string | SAP |  |
| factories\[\].stock | {warehouse, productionFloor} | SAP | Daily snapshot (C-04) |
| factories\[\].stockStatus | ok \| low \| outOfStock | Derived | From stock vs critical level and manual flag |
| factories\[\].manualOos | {active, setBy, setAt, note} | Catalog | Factory manager sets and clears it; never the SAP sync (C-12) |
| factories\[\].oosConflict | boolean | Derived | Stock above critical while manual OOS is set (C-14) |
| usedInSkus\[\] | {skuUuid, quantity, usage} | Derived | Filled from Module 3 (C-08) |
| lastSapSync | datetime | System |  |

```json
{
  "erpId": "R.WG.10K.BRD.08MM",
  "name": "White gold board 10K 0.8",
  "status": "active",
  "family": "Board",
  "material": "10K white gold",
  "measure": { "measuredBy": "thickness", "value": 0.8, "unit": "mm" },
  "unitOfMeasure": { "type": "weight", "quantity": 1, "unit": "kg" },
  "countryOfOrigin": "US",
  "hsCode": "7113.11.00",
  "unitCost": { "amount": 245.5, "currency": "USD", "source": "TBD" },
  "criticalStockLevel": {
    "global": 2,
    "factoryOverrides": [{ "factoryId": "125", "level": 5 }]
  },
  "alternatives": [
    { "erpId": "R.WG.10K.BRD.09MM", "forBalancer": true, "forPlanner": true }
  ],
  "factories": [
    {
      "factoryId": "110",
      "stock": { "warehouse": 12.4, "productionFloor": 1.1 },
      "stockStatus": "ok",
      "manualOos": { "active": false },
      "oosConflict": false
    },
    {
      "factoryId": "125",
      "stock": { "warehouse": 6.0, "productionFloor": 0.5 },
      "stockStatus": "outOfStock",
      "manualOos": { "active": true, "setBy": "fm.125", "setAt": "2026-11-20T08:10:00Z", "note": "Supply scheduled for Feb 1" },
      "oosConflict": true
    }
  ],
  "usedInSkus": [
    { "skuUuid": "8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90", "quantity": 1, "usage": "static" }
  ],
  "lastSapSync": "2026-11-23T02:04:11Z"
}
```

## UI mockup

[Open the prototype](https://claude.ai/artifact/CP3Nfn2fR7aHUDBSRmaJFx). Screens for this module: Configurations → Components (list and detail tabs) and Overview → Component stock levels.
