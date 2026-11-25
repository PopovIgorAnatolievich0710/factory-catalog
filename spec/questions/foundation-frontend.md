# Foundation frontend — assumptions and questions

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

Each row was implemented with the recommended variant.

## Contract with the backend (needs the backend to match)

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| FE-1 | Shape of `filters` in `POST /v1/{resource}/search` and `/export` now that filtering is done from column headers (values, number operators, text contains) | `filters: { [field]: { values?: string[], operator?: "eq"\|"ne"\|"lt"\|"lte"\|"gt"\|"gte"\|"between"\|"contains", value?: string, valueTo?: string } }`. Either `values` (any of) or `operator` + `value`; `valueTo` only with `between`; numbers travel as strings. Matches `ListFilter` / `ListFilterOperators` already present in `Tenengroup.Infrastructure.Common/Paging` | `filters: { [field]: string[] }` from plan section 2 (cannot express operators) |
| FE-2 | Is `page` 1-based? | 1-based everywhere (search, export, history); UI page sizes 25 / 50 / 100, default 25 sent explicitly | 0-based |
| FE-3 | Endpoint that starts a sync ("Sync now", S-03) — not in plan section 2 | `POST /v1/sync/{syncType}/run`, no body; 2xx when started, 409 when a run is already in progress; progress read by polling `GET /v1/sync/{syncType}/status` every 3 s | `POST /v1/sync/{syncType}`; a job id + per-job status endpoint |
| FE-4 | `status` values of the sync status and its value before the first run | `"Running" \| "Succeeded" \| "Failed"` (backend `SyncRunStatus`), `null` with `lastSuccessAt: null` when never run (the endpoint must answer 200, not 404) | 404 until the first run |
| FE-5 | `syncType` keys | `sap-components` (Components list, "Last SAP sync") and `master-catalog-skus` (SKU list, "Last master catalog sync") in the placeholders; module agents align with the backend constants | other keys |
| FE-6 | Response shape of `GET /v1/feature-toggle` | `{ data: [{ name, isEnabled, ... }] }` — the shipment service envelope ("as in the shipment service"); every other shared endpoint returns the bare body from plan section 2 | bare array |
| FE-7 | Meaning of `permissions.enable_all` | Same as the shipment service: toggle enabled → role checks enforced; disabled → every check passes. If the toggle request fails the UI enforces (fail closed) | toggle enabled → everything allowed |
| FE-8 | Export file name | Taken from `Content-Disposition` (`filename*=UTF-8''` or `filename=`); backend must send `Access-Control-Expose-Headers: Content-Disposition`. Fallback `{listKey}_{yyyyMMdd_HHmmss}.xlsx` built in the UI | always build the name in the UI |
| FE-9 | `columns` in the export request | Keys of the visible columns in display order (always-visible + chosen optional) | labels |
| FE-10 | CORS | The dev server proxies nothing; both APIs must allow the UI origin (`http://localhost:8080` in dev) | Vite proxy per API |
| FE-11 | Type of factory ids | `string` in `factoryIds` and `Factory.id` (plan section 2); comparisons go through `String()` so numeric ids also work | number |
| FE-12 | "X of Y" footer on server-driven lists | The paging line shows `from–to of totalCount` (filtered total) plus "Clear N filters"; the unfiltered total is not shown because the search response has no such field. Client-side tables show "X of Y" | add `unfilteredCount` to the search response |
| FE-13 | Excel import contract (P-04, S-10, S-13) | Not fixed by the foundation: `ExcelImportDialog` takes `onValidate(file) → { totalRows, validRows, errors: [{ row, field, message }] }` and `onConfirm(file)`; the owning module defines the endpoints (suggested: `POST /v1/{resource}/import/validate` and `POST /v1/{resource}/import`, multipart `file`). Import is blocked while any row has an error | import valid rows and skip invalid ones |
| FE-14 | List-settings and feature toggles for Balancer lists | Always read from the Factory Catalog API (the balancer has no user settings store); search / export / history / sync accept `api={balancerApi}` | duplicate the endpoints in the Balancer API |

## Auth and permissions

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| FE-15 | Where do UI roles come from? | From `GET /v1/user/me` (`roles`), resolved by the backend. The UI has no group-claim role resolver and no `GROUP_PERMISSIONS_URL` | resolve from token group claims + GroupPermissions json as the shipment UI does |
| FE-16 | User with several roles | Union of the grants of all roles | highest-priority role only (shipment UI) |
| FE-17 | How a permission is expressed in CASL | `can("access", "<Permission name>")`, names identical to plan section 3; `BO_Administrator` → `can("manage", "all")` | action/subject pairs such as `can("edit", "components.alternatives")` |
| FE-18 | Own-factory rule for `BO_Administrator` and when permissions are disabled | `canEditFactory` returns true for any factory | restrict admins to `factoryIds` too |
| FE-19 | No-access condition (C-01) | Permissions enforced AND (no known role OR `/v1/user/me` failed) → only the "No access" page | redirect to a `/403` route |
| FE-20 | Mockup vs plan matrix: the mockup lets the factory manager edit "Runs here", machine type and labor cost; the plan gives these to `Processes.Edit` (catalog admin) | Permission matrix left exactly as in the plan; the processes module decides which permission each control uses | add `Processes.EditFactorySetup` (own factory) |
| FE-21 | Permissions of the Balancer nav entries | Routing dashboard → `Balancer.ViewDashboards`; Decision log, Daily plan, Transfers → `Balancer.View` (set in `app/balancer/navigation.ts`, owned by the module) | all behind `Balancer.View` |
| FE-22 | Azure ids | `window.env` placeholders; dev values = shipment UI registration (A-6) | dedicated registration |

## Shell and kit

| # | Question | Implemented (recommended) | Alternatives |
|---|---|---|---|
| FE-23 | Grouped navigation (Configurations / Overview / Balancer) — `bo-frontend`'s nav bar is a flat list | Own `AppLayout` + `GroupedNavBar` reusing `bo-frontend` TopBar, AppSwitcher, Loader, GlobalStyles, layout styles and theme; no collapse toggle | extend `bo-frontend` with nav groups; flat list in `TGLayout` |
| FE-24 | UI routes | `/components`, `/processes`, `/skus`, `/component-stock-levels`, `/process-capacity`, `/factory-share`, `/balancer/{routing,decision-log,daily-plan,transfers}`; `/` opens the first permitted entry (Components) | nest Overview pages under their module path |
| FE-25 | Where the global-share Settings page lives (mockup open item) | Not in the navigation; the skus module mounts it under its own prefixes (e.g. `/factory-share/settings`) behind `Skus.EditGlobalShare` | separate Settings nav entry |
| FE-26 | Header-filter table implementation | Own `DataTable` / `ClientDataTable` / `ColumnHeaderFilter`; `bo-frontend`'s `List` and `ColumnFilter` are not used (no number operators, no sort in the popover, column choice stored in localStorage without the 8-column limit) | patch `bo-frontend` |
| FE-27 | When text / number header filters are applied | 400 ms after the last keystroke (each change is a server request); checkbox values apply at once | Apply button |
| FE-28 | Column picker | Always-visible default columns + up to 8 optional ones; saved on every change; order = definition order | drag-to-reorder |
| FE-29 | Selection across pages | Kept while paging, cleared when search or filters change | page-only selection |
| FE-30 | Tabs in the URL | `?tab=<key>` query parameter, `history.push` on change | path segment |
| FE-31 | Notifications | Redux notification + MUI Snackbar as in the shipment UI; `notistack` is installed but not wired | notistack provider |
| FE-32 | Visual style | `bo-frontend` theme (Open Sans, BO top bar) kept; table density, header style, chips, lock tag, tiles, nav sections follow the mockup; Roboto Mono for ids. The page title stays in the content area, not in the top bar | switch the app to Roboto / plain MUI theme |
| FE-33 | Dependencies left out versus the shipment UI | `@tg/item-editor`, ckeditor, barcode/qr/pdf, dnd, styled-components, react-helmet, query-string/qs, country-codes-list, `@reduxjs/toolkit`, `vite-plugin-html`, `vite-plugin-static-copy`; no `.npmrc`, no registry token in the Dockerfile | — |
| FE-34 | `tsconfig` include | Only `src/**` (plus `typings.d.ts`); `bo-frontend` files are type-checked when imported, so unused submodule files needing missing packages (ckeditor, dnd-kit) do not break `tsc` | include `bo-frontend/src/**` as the reference does |
