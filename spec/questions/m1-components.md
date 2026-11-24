# Module 1 (Components) — questions and assumptions

Each row was implemented with the recommended variant.

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| M1-1 | SAP export format (open question I-1) | Two UTF-8 CSV files with a header under `{prefix}/{yyyy-MM-dd}/`: `components.csv` and `stock.csv`, columns in `spec/modules/1-components.md`; the parser sits behind `ISapExportReader` | JSON / fixed-width / one file; SAP column names |
| M1-2 | Which dated folder does a run read? | The folder of the run's UTC date; a missing file fails the run (Hangfire retries the scheduled job) | The newest folder not later than today; a date parameter on the manual trigger |
| M1-3 | Is `components.csv` a full master or a delta? | Full: an ERP id missing from the file is marked `Inactive`. A file with no valid row fails the run instead of deactivating everything | Delta file with an explicit status only |
| M1-4 | Is `stock.csv` a full snapshot? What about a component / factory that disappears from it? | Full: the row stays (it carries the manual OOS flag and the override) and its stock becomes 0 | Delete the row; keep the last known stock |
| M1-5 | Several stock rows for the same component, factory and location | Summed (several storage bins) | Last row wins; reject as a row error |
| M1-6 | Does the sync write history for stock quantities? | No — only master-data changes, new components and deactivation. Daily quantity changes would add tens of thousands of rows a day | Log every stock change; log only status transitions |
| M1-7 | Unit cost source (C-10, business question) | View only. The sync stores the amount when the SAP file carries one (source `SAP`) and leaves it untouched otherwise. No edit endpoint although `ComponentsEditCost` exists | Edit endpoint for the supply chain manager; import from the Pricing Tool sheet |
| M1-8 | Who sees unit cost? | Callers with `ComponentsViewCost`; others get `null` and the UI hides the column | Everyone |
| M1-9 | Derivation when no critical level is set (no override, no global) | Never `Low`, never OOS conflict (the comparison with a missing level is false) | Treat a missing level as 0 |
| M1-10 | Status of a component in a factory that has no `component_factory` row | No status there: only existing rows count for the worst status and for the stock levels view; the Stock tab shows "—" and the manager can still flag OOS, which creates the row | Treat a missing row as zero stock (OOS) in every active factory |
| M1-11 | Inactive components | Listed with their status and an "Inactive in SAP" tag, never "action required", left out of the stock levels view; they cannot be added as an alternative, an existing link stays and is tagged | Hide inactive components; remove their alternative links |
| M1-12 | Manual trigger route: the task says `POST /v1/sync/SapComponents/run`, the status endpoint and the UI kit use the sync type `sap-components` | `POST /v1/sync/sap-components/run` (the `SyncTypes.SapComponents` value) | Route segment `SapComponents` |
| M1-13 | Does the manual trigger run in the request or in the background? | In the request: 409 comes straight from the run tracker and the response is the final status | Enqueue a Hangfire job and let the UI poll |
| M1-14 | "Sync now" for the SAP sync in the UI | Not shown: the UI spec gives the components list only "Last SAP sync" and keeps "Sync now" on the SKU list. The endpoint exists | Show it to the catalog admin |
| M1-15 | Default use flags of a newly added alternative | Both on (for balancer, for S.C. planner); the supply chain manager unticks what does not apply | Both off; ask in the add form |
| M1-16 | Who is stored as "Set by" on a manual OOS flag | The user's display name (e-mail when the token has no name), as the UI shows "Set by <name>" | E-mail and resolve the name in the UI |
| M1-17 | Editing the note of an active flag | Changes the note only; "set by" and the date stay those of the flag | Update author and date on every note edit |
| M1-18 | TG Supplier replacement contract (C-16) | `GET /v1/components/{erpId}/alternatives?factoryId=` returns active planner alternatives with stock in that factory (zero stock included); `POST /v1/components/replacements` validates the pair against the approved list and stores it. Basic auth caller | Return only alternatives in stock; accept any replacement and flag unapproved ones |
| M1-19 | Does recording a replacement change stock? | No — SAP consumption (MCM) reads the record; stock comes from the next SAP sync | Decrease the replacement's stock immediately |
| M1-20 | List performance: status, total stock and per-factory stock are derived | Components and their factory rows are loaded and filtered / sorted / paged in memory with the shared filter helpers | SQL view or computed columns when the catalog grows |
| M1-21 | Filter value lists for Family and Material | `GET /v1/components/filter-options` returns the distinct values | Free-text filter |
| M1-22 | Global critical level input | Saved on Enter or when the field loses focus (the prototype has a Save button) | Explicit Save button |
| M1-23 | Component stock levels columns | ERP ID first, then Name (the prototype shows one "Component" cell); no status tiles above the table (not in the written UI spec) | One combined cell; tiles with counts per status |
| M1-24 | Factory code shown in tags and column titles | `factory.countryCode`, the factory id when it is empty (seeded factories have no country) | A dedicated short code column on `factory` |
| M1-25 | ERP ids in routes | Taken as one path segment (dots are fine); an id containing `/` is not addressable | Pass the id as a query parameter |
