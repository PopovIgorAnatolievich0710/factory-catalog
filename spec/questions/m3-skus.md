# Module 3 (SKUs) — questions and assumptions

> **Requirements:** Phase overview v4; Module 3: SKUs v4

Each row was implemented with the recommended variant.

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| M3-1 | Events or a daily job for the master catalog sync (S-01)? | Daily Hangfire job (`0 3 * * *` UTC) plus "Sync now"; the consumer for master catalog events is left for when the team publishes them | SQS / SNS event consumer; hourly job |
| M3-2 | Master catalog API contract | `GET v1/skus?page&pageSize&changedSince` returning `{ items, hasMore }`, item = the SKU data schema of the requirements (contract in `spec/modules/3-skus.md`), Basic auth | Full export file on S3; cursor paging; OAuth client credentials |
| M3-3 | Is each item the full SKU or a field delta? | Full SKU: aliases and components missing from the item are removed from the SKU | Field-level patch; explicit "removed" lists |
| M3-4 | "Changed since" reference point | Start time of the last successful run (no value on the first run) so changes made during a run are pulled again next time | Finish time; a server-provided cursor |
| M3-5 | How are retired SKUs delivered? | As items with `status: "retired"`; they stay in the catalog (never deleted) and keep their mappings and factory rows | Deleted items list; tombstones |
| M3-6 | A single bad SKU in a page | Skipped, logged with the reason and counted in `failed`; the run completes. Transport / HTTP errors fail the whole run (shown on the badge; "failures alert the admin" = failed run status, no e-mail) | Fail the whole run; e-mail / Slack alert to the admin |
| M3-7 | An alias that already belongs to another SKU | The incoming SKU fails (counted in `failed`) | Move the alias; keep both |
| M3-8 | What happens to processes / producible / overrides when a SKU becomes a dropshipper product? | They are removed (history written); dropshipper SKUs have no factory data (S-19) | Keep them hidden |
| M3-9 | When is validation (S-11) recomputed? | After every master catalog run (all SKUs) and every process-mapping import (imported SKUs). A component deactivated by the SAP sync shows up at the next master catalog run | Also at the end of the SAP sync (needs a Components module change); on read |
| M3-10 | Is a manufactured SKU with no process mapping (or no components) invalid? | Valid: only mapped items are checked; the migration (S-10) fills processes later | Invalid until at least one process is mapped |
| M3-11 | "Only approved files can be imported" (S-10) | The workbook needs an `Approval` sheet with `Approved by` and `Approved on` filled; otherwise a file-level error and nothing is saved. The approver is logged | Upload to an approval queue approved in the UI; signed file; approval by a second admin in the app |
| M3-12 | Does the process import replace a SKU's processes or add to them? | Replace: every SKU in the file gets exactly the processes of the file; SKUs not in the file are untouched | Upsert only; explicit delete column |
| M3-13 | Step column on the SKU Processes tab (UI spec open item 7) | Not shown: the requirements say "no step order" and mappings carry no step | Keep a Step column |
| M3-14 | How does one manager's override rebalance the others (S-14)? | The rest (`100 − value`) is split among the other producible factories in proportion to their current effective shares and stored; rebalanced rows show as `Rebalanced`, another manager's override is rescaled but keeps its author. Reset clears the factory's override; with no manager override left the whole SKU returns to the defaults | Overrides only of the editing factory, total normalised at read time; reject changes that do not total 100 and let managers coordinate |
| M3-15 | Where do the effective shares come from when a factory is not producible (S-15)? | Base shares of producible factories redistributed with `ShareRedistribution` (same function as the quotes module); stored values unchanged | Store redistributed values |
| M3-16 | Precision of shares | 0–100 with at most 2 decimals; rounding remainder goes to the largest share | Whole percents only |
| M3-17 | Global share: must every active factory be listed? | Yes, each active factory exactly once, total 100 | Missing factories mean 0 |
| M3-18 | Where does the global share Settings page live (Settings is out of the Phase 1 menu)? | `/factory-share/settings`, reached from the "Default balance" card on the Factory share page (admin only) | A Settings menu entry; inline editing of the default row |
| M3-19 | Does a producible flag without a `sku_factory` row exist? | No row = not producible; the first tick creates the row | Producible by default everywhere |
| M3-20 | Producible import values | `Yes`/`No`, `Y`/`N`, `true`/`false`, `1`/`0`; rows of factories the caller cannot edit are row errors | Ignore other factories' rows silently |
| M3-21 | Production conflict (S-20): which logs count? | Only the latest produced quote per SKU and factory, so the tag clears when a later log matches; logs are read from `quote.production_log_json` written by the quotes module | Every log of the last N days; a stored conflict table |
| M3-22 | Machine mismatch when either machine type is unknown | Not a conflict (only both known and different) | Treat unknown as a mismatch |
| M3-23 | SKU lookup in imports | Same order as `Business/Quotes/SkuLookup`: UUID, alias, then an unambiguous Site SKU, case-insensitive; implemented as a batch resolver (`SkuIdentifierResolver`) because imports resolve thousands of rows | Call `SkuLookup.Find` per row |
| M3-24 | Manual sync trigger: in the request or in the background? | Background (Hangfire), because a full master catalog pull can take minutes; the endpoint returns the `Running` status and 409 when a run is in progress, the UI polls the status | In the request, like the SAP sync |
| M3-25 | Who can see Factory share and SKU pages? | Everyone with `Skus.View`; edit controls are greyed for others | Hide factory share from production floor users |
