# Factory Catalog — Phase 1 UI: change log and current spec

Prototype: `Factory Catalog.html`. Phase 1 mode is the default; the "Scope" switch in Tweaks toggles Phase 1 / Full vision. Full vision is unchanged. This document records what the Phase 1 UI now does. The original Phase 1 brief is the baseline; product-owner decisions made during review override it where they differ.

---

## 1. Global

### Navigation (Phase 1)
Sections and order follow the module flow:

- **Configurations:** Components → Processes → SKU
- **Overview:** Component stock levels → Process capacity → Factory share
- **Balancer:** Routing dashboard → Balancer decision log

Changes from the original brief:
- "Catalog" renamed **Configurations**, "Factories" renamed **Overview**, "Operations" renamed **Balancer**.
- "Products" renamed **SKU** and moved below Processes.
- Overview order: Component stock levels, Process capacity, Factory share.
- **Audit log** and **Setup / Settings** removed from the menu for now. Global factory share and the SAP / master catalog sync cards (which lived in Settings) are therefore not reachable; "Sync now" remains on the SKU list for the admin.
- Phase 1 opens on Components.
- Hidden in Phase 1: Design groups, Global prices, My queue, Dashboard, Performance view, Reports.

### Personas (Phase 1 switcher)
Factory manager (with a KG / NZ / HU / TH factory picker), Supply chain manager, Factory catalog admin, Production floor user (view only), Operations manager, Finance. Edit controls appear only for the persona allowed to edit; everyone else sees the same control in a disabled state.

### Permissions
| Action | Who |
|---|---|
| Mark / clear component OOS flag, edit OOS note | Factory manager of that factory |
| Add / remove alternatives, set "For balancer" / "For S.C. planner" | Supply chain manager |
| Global critical level and per-factory overrides | Supply chain manager |
| Process specifications (name, type, measured in, status, description) | Factory catalog admin |
| New process | Factory catalog admin |
| Process per-factory: runs here, machine type, labor cost | Factory manager of that factory |
| Process capacity slider, reason, expected recovery | Factory manager of that factory |
| SKU factory share / producible (own column) | Factory manager of that factory |
| Default balance (global share) | Factory catalog admin |
| Sync now | Factory catalog admin |
| Process capacity factory switcher + All factories view | Operations manager (other non-factory-manager roles see the All factories grid, read only) |

### UI conventions (apply everywhere)
- **Lock tag:** a grey pill with a lock icon in the card header, stating edit rights and source, e.g. "Read only · Source: SAP", "Source: SAP. Factory manager edits.", "Read only · Source: Master catalog", "Factory catalog admin edits". The lock icon is reserved for edit-permission messaging only.
- **No "From SAP" / "From master catalog" badges** on component screens; the source appears in the lock tag instead.
- **Unified checkboxes:** the same checkbox is shown in every row. It is active only for the persona/factory allowed to edit, and greyed for everyone else, so the view looks the same for all users.
- **Stable layouts:** tables with editable cells use fixed column widths and row heights, so toggling a flag or opening a note never shifts the layout.
- **ID first:** list views start with the record's unique ID (Components: ERP ID; Processes: Process ID; SKU: Site SKU).
- **Detail tabs:** the change log tab is called **Audit** (formerly History) on component, process and SKU pages.
- **Specifications** are shown as a simple two-column table (label | value), one row per field.

### Column-header filtering (all tables: lists and detail-page tables)
- Every column title has a filter/sort icon; clicking opens "Filter by {column}" with Ascending / Descending sort.
- **Restricted values** (family, status, type, factory, usage, validation): checkbox list, with a search box when there are more than 6 values.
- **Numbers:** operator (=, ≠, <, ≤, >, ≥, Between) plus value(s).
- **Text / IDs:** "contains".
- An active filter turns the title blue and shows × to clear it; the footer shows "Clear N filters" and "X of Y".
- The separate filter dropdowns above tables were removed; the free-text search box stays.
- Applies to: Components list, Component stock levels, Processes list, SKU list, component "SKUs using this", process "SKUs using this", SKU Components tab, SKU Processes tab.

---

## 2. Components

### List
- No "New component" button (components come only from SAP). Header: "Last SAP sync: <date, time>".
- Default columns: ERP ID, Name, Family, Status, Total stock, Critical level, SKUs using. Column picker (up to 8, remembered) also offers Material, Unit cost and per-factory stock.
- Status values: OK, Low, OOS, OOS conflict; shows the worst status across factories plus which factories.
- "Action required" marker (red left border + label) on rows that are OOS or OOS conflict.
- Family shown as text only (no icons).
- Search on ERP ID and name; column-header filters; Export to Excel (CSV download).

### Detail — tabs
Specifications · Stock by factory · Alternatives · SKUs using this · Settings · Audit

**Specifications:** simple table — ERP ID, Name, Family, Material, Measure, Measured by, Unit of measure, Unit cost. Lock tag "Read only · Source: SAP". There is no "Source to be confirmed" note on unit cost.

**Stock by factory:**
- Lock tag "Source: SAP. Factory manager edits."
- Columns (fixed widths): Factory, Warehouse, Floor, Total, Critical level (with "global" or "override"), Status, OOS flag, Note.
- No "days of cover".
- OOS flag is a checkbox labelled "OOS". It is active only on the current factory manager's own factory row, and greyed on other rows (a checked flag on another factory shows in faded red).
- Ticking OOS flags immediately and opens an inline note box with a green ✓ to save (Enter also saves). The saved note shows with "Set by <name> · <date>"; the owner can click it to edit. Unticking clears the flag.
- The flag is set and cleared only by the factory manager, never by the SAP sync.
- OOS conflict (stock above critical but flag on) shows on the status chip, with the explanation in its tooltip.
- Seed data: HU is flagged OOS on ERP-10001, 10002, 10003, 10005 by Zsófia Nagy with the note "Supply scheduled for Feb 1".

**Alternatives:**
- Columns: ERP ID, Name, stock per factory, **Use**.
- Use has two checkboxes per alternative: **For balancer** and **For S.C. planner** (supply chain manager edits; greyed for others).
- Removed: the subtitle "Used by the balancer when…" and the one-way note.

**SKUs using this:** columns Site SKU-level rows with Product, Site SKU, Qty, **Usage** (Static / Dynamic). The Type column is removed (only manufactured SKUs use components). Static uses a pin icon and Dynamic a slider icon. Lock tag "Read only · Source: Master catalog".

**Settings:** one global critical stock level plus optional per-factory overrides (override wins). Supply chain manager edits.

**Audit:** who, when, field, old value, new value; includes SAP syncs.

### Component stock levels
- Shows components below the critical level or OOS, per factory.
- Status values: Low, OOS, OOS conflict. No Daily use, Days of cover or Re-order.
- Column-header filters (including Factory) and search; Export to Excel.

---

## 3. Processes

### List
- Columns: **Process ID** (first), Process, Type (Machine / Work), Machine type, Factories running it, Lowest capacity today, Avg labor cost ($), SKUs using.
- No Stage column. "New process" is shown for the factory catalog admin only.
- Search on process ID or name; column-header filters.

### Detail — tabs
Specifications · Factories · SKUs using this · Audit

**Specifications:** simple table — Name, Type, **Measured in** (process unit of measure: minutes, seconds, hours, units, pieces, cm, mm, cm², mm²), Status, Description. Factory catalog admin edits inline; others see the lock tag "Factory catalog admin edits". Removed: workflow stage, floor colour tag, AI assembly-instruction label.

**Factories:**
- Lock tag "Factory manager edits".
- Columns: Factory, Runs here, Machine type (machine processes only; can differ per factory), Labor cost per <unit> with currency, Capacity.
- The factory manager edits only their own factory row (highlighted); other rows are greyed.
- Capacity is read only here; the note about where capacity is set was removed.

**SKUs using this:**
- Columns: Product, Site SKU, **Quantity required (<unit>)**. Steps are not shown here.
- Lock tag "Read only · Source: Info file / Supplier".

### Data model: process quantity per SKU
- The measuring unit is defined once on the process ("Measured in").
- Each SKU–process assignment carries the quantity of that process the SKU requires, in the process unit. Example: process measured in minutes; SKU 1 needs 2 min, SKU 2 needs 5 min.
- The quantity is shown both in the process "SKUs using this" tab and the SKU "Processes" tab.

### Process capacity
- **Factory manager:** sees only their own factory. Rows list only processes the factory runs: Process, Type, slider in 10% steps (0–100, 11 stops; default 100%), status (Offline 0% / Reduced 10–90% / Full 100%), optional Reason and Expected recovery when below 100% ("For information only — capacity changes only when you change it"), and "Last changed by <name> on <date>".
- **Operations manager:** toggle between All factories and By factory (with a factory switcher; read only).
- **All factories grid** (operations manager; also shown read only to admin and the other roles): Process and Type columns placed as in the factory-manager view, then one column per factory. Each cell has a capacity bar and %: green 100%, amber reduced, red 0%. Reduced and offline cells are tinted with "Reduced/Offline · until <date>"; hovering shows the reason. Processes not run at a factory show "Doesn't run here".

---

## 4. SKU

### List
- Read only from the master catalog. No New SKU, version chips, workflow statuses or design group.
- Header: "Last master catalog sync: <date, time>"; "Sync now" for the factory catalog admin.
- Tiles: Total SKUs, Dropshipper, Invalid.
- Columns in order: **Site SKU, Factory SKU, SKU name, Type** (Manufactured / Dropshipper), Status (Active / Retired), Producible at (factory chips), Validation (Valid / Invalid).
- Search across UUID, Site SKU, Factory SKU and name; column-header filters.

### Detail — tabs
Overview · Components · Processes · Factories · Audit. **Dropshipper SKUs show Overview only**, including dropshipper name and total cost.

- **Overview:** name, UUID, Site SKU, Factory SKU, type, status, legacy IDs shown as "Deprecated alias", and a validation box listing issues (e.g. "Component ERP-10999 not found in SAP").
- **Components:** ERP ID, Component, Qty, Usage (Static / Dynamic), stock status per factory; each row links to the component. Lock tag "Read only · Source: Master catalog".
- **Processes:** Step, Process, Type, **Quantity required** (in the process unit), current capacity per factory; each row links to the process. Lock tag "Read only · Source: Info file / Supplier".
- **Factories:** producible on/off and share % per factory, marked Default or Override, plus "Reset to default". The factory manager edits only their own factory.

### Factory share (Overview section)
- "Full capacity" renamed **Producible**.
- The factory manager edits only their own factory's column (highlighted). The Default balance row is edited only by the factory catalog admin.
- Cells: grey = default, blue = override. Per-row "Reset to default", the 100% total check, and auto-save.

---

## 5. Balancer
The routing dashboard and balancer decision log were built per the original Phase 1 brief. They were intentionally left unchanged in this review round.

---

## 6. Sample-data notes (prototype only)
- Dropshipper SKUs are an explicit list (110-12-4492-33, 110-09-7745-02, plus several generated SKUs).
- Invalid SKUs reference ERP-10999, which does not exist in the sample component list.
- Static / Dynamic usage is currently derived from the old mandatory flag (mandatory = Static).
- SKU name currently equals the product name in the sample data.

## 7. Open items
- The SKU page's processes tab keeps the Step column (the brief lists step order). Confirm whether it should remain.
- "Runs here" on the process Factories tab is set by the factory manager. Confirm whether it should belong to the factory catalog admin instead.
- Settings (global factory share, sync status cards) and the Audit log are out of the menu. Decide where the global share and sync controls live.
- Define how SKU name differs from product name, if it does.
