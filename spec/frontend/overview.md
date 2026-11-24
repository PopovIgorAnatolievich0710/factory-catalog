# Frontend — Overview

## Location

```
frontend/FactoryCatalogUI/
```

Admin SPA for the Factory Catalog (components, processes, SKUs) and the Balancer. It talks to two backends:
the Factory Catalog API and the Balancer API. The UI source of truth is the Phase 1 mockup in `spec/mockup/`
(`Phase 1 UI Changes.md` first); the shell and the page kit below reproduce it with MUI and `bo-frontend`.

---

## Technology Stack

Same stack and versions as the shipment service UI (`frontend/ShipmentUI`).

| Technology | Version (range in `package.json`) | Purpose |
|---|---|---|
| React | ^18.3.1 | UI framework |
| TypeScript | ^4.9.5 | Type safety |
| Vite | ^6.0.0 | Build tool and dev server |
| Redux + redux-thunk | ^4.0.5 / ^2.3.0 | Global state |
| Material-UI (MUI) | ^6.4.7 | Component library |
| Formik + formik-mui + yup | ^2.2.6 | Forms (available to modules) |
| Azure MSAL | ^3.28.1 | Azure AD authentication |
| CASL | ^6.8.0 | Permission checks |
| Axios | ^0.21.4 | HTTP client |
| Day.js | ^1.9.8 | Dates |
| notistack | ^1.0.4 | Available to modules; the shell uses the Redux notification + Snackbar pattern of the reference app |
| file-saver | ^2.0.5 | Saving the Excel export |

No private-registry packages: there is no `.npmrc` and the Docker build needs no token.

---

## Source Structure

```
frontend/FactoryCatalogUI/
├── bo-frontend/            # git submodule (shared BO components), pinned to the commit the shipment service pins
├── index.html              # window.env placeholders
├── vite.config.mts         # dev server :8080, dev replacements for the placeholders
├── Dockerfile, nginx.conf, docker-entrypoint.sh
└── src/
    ├── index.tsx, app.tsx  # store, MSAL bootstrap, shell
    ├── router.tsx          # central router (module agents do not edit)
    ├── navigation.ts       # central nav aggregation (module agents do not edit)
    ├── theme.ts
    ├── app/
    │   ├── auth, loading, notifications, current-user, factories   # shared store slices
    │   ├── components/     # private-route, no-access, not-found, health-check, home, kit-demo
    │   ├── components-catalog/   # Module 1: routes.tsx, navigation.ts, store/index.ts
    │   ├── processes/            # Module 2
    │   ├── skus/                 # Module 3
    │   └── balancer/             # Module 4
    ├── components/         # the page kit (see below)
    ├── core/auth/          # roles, permissions, CASL ability, PermissionProvider, PermissionGate, current user
    ├── core/CONSTANTS.ts
    ├── constants/urls.ts   # UI routes and shared API urls
    ├── entities/           # TypeScript types of every shared API shape
    ├── enums/
    ├── helpers/            # http-client, api-helper, api-error, response-helper
    ├── services/           # user, factories, history, list settings, sync, feature toggles, list search/export
    ├── store/              # rootReducer, selectors (useRootStateSelector, useFactories)
    └── utils/              # format-date, list-query (URL <-> list request), table-filter (client filter/sort)
```

---

## Environment (`window.env`)

`index.html` holds `${..._ENV_VAR}` placeholders. `docker-entrypoint.sh` substitutes them with `envsubst` at
container start; `vite.config.mts` substitutes dev values when the dev server serves the page.

| `window.env` key | Container env var | Dev value |
|---|---|---|
| `APP_URL` | `FACTORY_CATALOG_BASE_FRONTEND_URL_ENV_VAR` | `http://localhost:8080` |
| `FACTORY_CATALOG_BASE_API_URL` | `FACTORY_CATALOG_BASE_API_URL_ENV_VAR` | `http://localhost:5010/` |
| `BALANCER_BASE_API_URL` | `BALANCER_BASE_API_URL_ENV_VAR` | `http://localhost:5011/` |
| `AZURE_CLIENT_ID` | `AZURE_CLIENT_ID_ENV_VAR` | shipment UI registration (open question A-6) |
| `AZURE_BACKEND_SCOPE` | `AZURE_BACKEND_SCOPE_ENV_VAR` | shipment UI scope (A-6) |
| `AZURE_TENANT_ID` | `AZURE_TENANT_ID_ENV_VAR` | Tenengroup tenant |
| `SERVICES_CONFIG_URL` | `SERVICES_CONFIG_URL_ENV_VAR` | BO services list used by the `bo-frontend` app switcher |
| `SERVICE_NAME` | — (constant) | `Factory Catalog` |

The dev server proxies nothing: both APIs are called cross-origin, so both backends must allow CORS for the UI
origin and expose the `Content-Disposition` header (export file name).

---

## Build & Dev

```bash
git submodule update --init          # bo-frontend
npm install --legacy-peer-deps
npm run dev          # Vite dev server, port 8080
npm run build        # production build to dist/
npm run typecheck    # tsc --noEmit
```

---

## Authentication

Azure AD via `@azure/msal-browser` / `@azure/msal-react`, through `bo-frontend`'s `useMsalService`
(redirect flow, callback `/signInCallback`). `app.tsx` waits for the redirect promise, signs in when there is no
account, then calls `init(msal)` from `helpers/http-client.ts`.

## API communication

`helpers/http-client.ts` creates one Axios instance per backend with the same interceptors:
request — `Authorization: Bearer <MSAL token>`; response — errors are rejected as `{ response, error }`.

`helpers/api-helper.ts` exposes them as two typed `Api` objects:

```ts
import { catalogApi, balancerApi } from "../../helpers/api-helper";

catalogApi.get<Component>(`v1/components/${erpId}`);
catalogApi.post<void>(`v1/components/${erpId}/oos`, body);
catalogApi.put / catalogApi.delete / catalogApi.postForm(url, formData) / catalogApi.download(url, body)
```

Every method resolves with the response body. Errors are shown with
`const notifyError = useErrorNotification()` (`helpers/response-helper.ts`): 400/422 field errors are listed as
`field: message`, 401/403/404/409/5xx get a standard message. Success messages: `dispatch(notificationSuccess("..."))`.

---

## Roles and permissions

The current user comes from `GET /v1/user/me` → `{ email, name, roles, factoryIds }` and is kept in the store
(`state.currentUser`). Roles are resolved by the backend from AD groups; the UI does not read group claims.

| Role constant (`core/auth/role.ts`) | AD group |
|---|---|
| `Roles.FactoryManager` | `FC-FactoryManagers` |
| `Roles.SupplyChainManager` | `FC-SupplyChainManagers` |
| `Roles.CatalogAdmin` | `FC-CatalogAdmins` |
| `Roles.ProductionFloor` | `FC-ProductionFloor` |
| `Roles.OperationsManager` | `FC-OperationsManagers` |
| `Roles.Finance` | `FC-Finance` |
| `Roles.BOAdministrator` | `BO_Administrator` |

Permission names are the ones in `implementation-plan.md` section 3, one-to-one, as the constant tree
`Permissions` in `core/auth/permissions.ts`:

`Components.View`, `Components.EditManualOos`, `Components.EditCriticalLevels`, `Components.EditAlternatives`,
`Components.ViewCost`, `Sync.Monitor`, `Sync.Trigger`, `Processes.View`, `Processes.Edit`,
`Processes.EditCapacity`, `Processes.ViewCost`, `Skus.View`, `Skus.EditProducible`, `Skus.EditShareOverride`,
`Skus.EditGlobalShare`, `Skus.EditMappings`, `Balancer.View`, `Balancer.ViewDashboards`,
`Balancer.EditReservations`, `Balancer.ViewDimCost`.

In CASL every permission is a rule `can("access", "<Permission name>")`; `BO_Administrator` gets
`can("manage", "all")`. A user with several roles gets the union of their grants (`core/auth/ability.ts`,
`RolePermissions`). The own-factory permissions (`Components.EditManualOos`, `Processes.EditCapacity`, `Skus.EditProducible`,
`Skus.EditShareOverride`) additionally need the factory to be in `factoryIds` — check them with `canInFactory`.

```tsx
const { can, canInFactory } = usePermissions();
const { user, canEditFactory } = useCurrentUser();

can(Permissions.Components.EditAlternatives);
canInFactory(Permissions.Components.EditManualOos, factory.id);   // permission AND own factory
canEditFactory(factory.id);                                       // own factory only (BO admin: always)

<PrivateRoute exact path={SOME_URL} requiredPermission={Permissions.Skus.EditGlobalShare}>...</PrivateRoute>
<PermissionGate allowed={can(Permissions.Processes.Edit)}><PrimaryBoButton>New process</PrimaryBoButton></PermissionGate>
<Can I="access" a={Permissions.Balancer.ViewDimCost}>...</Can>
```

Feature toggle `permissions.enable_all` (`GET /v1/feature-toggle`) works as in the shipment service: when the
toggle is enabled the role rules above apply; when it is disabled every check passes. If the toggle request fails
the UI enforces permissions (fail closed).

**No access (C-01):** when permissions are enforced and the user has none of the roles above (or `/v1/user/me`
fails), the shell renders only the "No access" page — no navigation, no routes.

---

## Shell

`components/app-layout` renders the `bo-frontend` top bar, app switcher, loader and global styles plus a grouped
side navigation (the `bo-frontend` nav bar has no groups). Navigation follows the mockup:

| Group | Entries (order) | Owner module |
|---|---|---|
| Configurations | Components, Processes, SKU | components-catalog, processes, skus |
| Overview | Component stock levels, Process capacity, Factory share | components-catalog, processes, skus |
| Balancer | Routing dashboard, Balancer decision log, Daily plan, Transfers | balancer |

Each module contributes entries from its own `navigation.ts` (`ModuleNavItem`: `group`, `order`, `title`,
`href`, `icon` (MUI icon), `permission`); `src/navigation.ts` merges, filters by permission and sorts them.

The central router lazy-loads one `routes.tsx` per module and mounts it on that module's path prefixes behind
its `*.View` permission:

| Module | Path prefixes | Gate |
|---|---|---|
| components-catalog | `/components`, `/component-stock-levels` | `Components.View` |
| processes | `/processes`, `/process-capacity` | `Processes.View` |
| skus | `/skus`, `/factory-share` | `Skus.View` |
| balancer | `/balancer` (`/routing`, `/decision-log`, `/daily-plan`, `/transfers`) | `Balancer.View` |

`/` redirects to the first navigation entry the user may open.

---

## Page kit (`src/components`)

A working example of every piece is in `src/app/components/kit-demo/` (used by the placeholder routes).

### List request / filter contract

`POST /v1/{resource}/search`, body `ListRequest` (`entities/list.ts`), response `{ items, totalCount }`:

```ts
{
  page: number,                 // 1-based
  pageSize: number,             // 25 | 50 | 100
  search: string,
  sortBy: string | null,
  sortDirection: "asc" | "desc",
  filters: {
    [field: string]: {
      values?: string[],        // restricted-value columns: any of
      operator?: "eq" | "ne" | "lt" | "lte" | "gt" | "gte" | "between" | "contains",
      value?: string,           // numbers are sent as strings
      valueTo?: string          // only with "between"
    }
  }
}
```

A filter carries either `values` or `operator` + `value`. `POST /v1/{resource}/export` gets the same body plus
`columns: string[]` (visible column keys in display order) and returns the xlsx; the file is saved under the
name from `Content-Disposition` (fallback `{listKey}_{yyyyMMdd_HHmmss}.xlsx`).

The list state lives in the URL: `?search=&sortBy=&sortDirection=&page=&pageSize=`,
`filter.<field>=<value>` (repeated, for `values`), `filter.<field>.<operator>=<value>` and
`filter.<field>.to=<valueTo>`.

### `CatalogListPage<T>` — server-driven list

```tsx
const columns: CatalogColumn<ComponentRow>[] = [
  { key: "erpId", label: "ERP ID", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
  { key: "name", label: "Name", alwaysVisible: true, sortable: true, filterType: "text" },
  { key: "status", label: "Status", alwaysVisible: true, sortable: true, filterType: "values",
    filterValues: [{ value: "ok", label: "OK" }, { value: "low", label: "Low" }],
    render: row => <StockStatusChip status={row.status} /> },
  { key: "totalStock", label: "Total stock", alwaysVisible: true, sortable: true, filterType: "number", align: "right" },
  { key: "material", label: "Material", sortable: true, filterType: "values", filterValues: materials },   // optional column
];

<CatalogListPage<ComponentRow>
  listKey="components"                       // list-settings key + export fallback name
  title="Components"
  resource="components"                      // POST v1/components/search | export
  columns={columns}
  getRowId={row => row.erpId}
  getRowUrl={row => `${COMPONENTS_URL}/${row.erpId}`}        // row click -> single view (ctrl/cmd: new tab)
  defaultSort={{ sortBy: "erpId", sortDirection: "asc" }}
  searchPlaceholder="Search by ERP ID or name"
  headerInfo={<SyncStatusBadge syncType="sap-components" label="Last SAP sync" />}
  isActionRequired={row => row.actionRequired}               // red left border + "Action required"
/>
```

| Prop | Purpose |
|---|---|
| `listKey`, `title`, `subtitle?`, `resource`, `api?` (default `catalogApi`; pass `balancerApi` for balancer lists) | identity and endpoint |
| `columns: CatalogColumn<T>[]` | `key` (field for sort/filter/export), `label`, `alwaysVisible?` (otherwise offered in the column picker), `sortable?`, `filterType?: "values" \| "number" \| "text"`, `filterValues?`, `render?`, `getValue?`, `align?`, `width?`, `monospace?` |
| `getRowId`, `getRowUrl?`, `defaultSort?`, `searchPlaceholder?` | rows and defaults |
| `headerInfo?`, `headerActions?` | right side of the header: sync line, "New ..." button |
| `summary?` | tiles above the table (`SummaryTile`) |
| `toolbarActions?(context)` | extra buttons next to Columns / Export; `context = { request, visibleColumnKeys, totalCount, reload }` |
| `isActionRequired?`, `renderRowTags?` | row marker and tags after the first cell |
| `selectable?`, `isRowSelectable?`, `bulkActions?(selectedIds, { clearSelection, reload, ... })` | bulk selection (S-13) |

Built in: debounced search box, column-header filter/sort popover on every column that sets `sortable` or
`filterType` (title turns blue with × when filtered), "Clear N filters", paging, a column picker limited to 8
optional columns stored through `GET`/`PUT /v1/user/list-settings/{listKey}`, "Export to Excel".

### `DataTable<T>` and `ClientDataTable<T>` — the header-filter table

`DataTable` is the controlled table `CatalogListPage` uses. `ClientDataTable` wraps it for detail-page tables
with data already in memory: it filters and sorts locally, derives the value list of `filterType: "values"`
columns from the rows when `filterValues` is omitted, and shows "Clear N filters" and "X of Y {noun}".

```tsx
<SectionCard title="SKUs using this component" headerRight={<LockTag text="Read only · Source: Master catalog" />} noPadding>
  <ClientDataTable
    columns={[
      { key: "siteSku", label: "Site SKU", sortable: true, filterType: "text", monospace: true },
      { key: "quantity", label: "Qty", sortable: true, filterType: "number", align: "right" },
      { key: "usage", label: "Usage", sortable: true, filterType: "values" },
    ]}
    rows={skus}
    getRowId={row => row.skuUuid}
    noun="SKUs"
    onRowClick={row => history.push(`${SKUS_URL}/${row.skuUuid}`)}
    fixedLayout                                  // fixed column widths for tables with editable cells
  />
</SectionCard>
```

`tableStyles` (same module) exports the header/body cell styles and row height for hand-built tables.

### `EntityViewPage` — single view

```tsx
<EntityViewPage
  title={component.name}
  entityId={component.erpId}                   // breadcrumb "← Components › ERP-10001"
  backUrl={COMPONENTS_URL} backLabel="Components"
  statusChips={<StockStatusChip status={component.status} />}
  actions={...}
  isLoading={isLoading}
  tabs={[
    { key: "specifications", label: "Specifications", content: <.../> },
    { key: "stock", label: "Stock by factory", content: <.../> },
    { key: "skus", label: "SKUs using this", count: 12, content: <.../> },
    { key: "audit", label: "Audit", content: <HistoryTab entityType="component" entityId={component.erpId} /> },
  ]}
/>
```

The active tab is in the URL (`?tab=audit`); only the active tab's content is mounted.

### `HistoryTab` — the "Audit" tab

`<HistoryTab entityType="component" entityId={erpId} api={catalogApi} />` — reads
`GET /v1/history/{entityType}/{entityId}?page&pageSize`, newest first, read only, paged. Columns: When, Who
(user or sync/import/system source), Factory, Field, Old value, New value.

### `SyncStatusBadge` — "Last ... sync" header line and "Sync now"

```tsx
<SyncStatusBadge
  syncType="master-catalog-skus"
  label="Last master catalog sync"
  canTrigger={can(Permissions.Sync.Trigger)}
  onSyncFinished={() => reload()}
/>
```

Reads `GET /v1/sync/{syncType}/status`, shows the last successful sync time, a "Last run failed" chip with the
error in its tooltip, and while a run is in progress a "Sync running" chip (polled every 3 s). With `canTrigger`
it shows "Sync now" (`POST /v1/sync/{syncType}/run`; 409 → "A sync is already running"), disabled while running;
after the run it shows "Added N · Updated N · Failed N".

### Shared pieces

| Component | Use |
|---|---|
| `LockTag` | `<LockTag text="Read only · Source: SAP" />` — grey pill with a lock icon for the card header; edit-rights messaging only |
| `SectionCard` | card with `title`, `subtitle?`, `headerRight?` (lock tag), `noPadding?` for tables |
| `SpecificationsTable` | `<SpecificationsTable rows={[{ label: "ERP ID", value: c.erpId }, ...]} />` — two-column label/value table |
| `FactoryGrid` | per-factory table, rows = factories (active factories from the store by default): `columns: [{ key, label, width?, align?, render(factory) }]`, `isRowHighlighted?` (own factory), `isRowDisabled?` (greyed). Fixed layout and row height |
| `GatedCheckbox` | `<GatedCheckbox label="OOS" allowed={canInFactory(Permissions.Components.EditManualOos, f.id)} checked={...} onChange={...} />` — same checkbox for everyone, disabled with a tooltip when not allowed |
| `PermissionGate` (`core/auth`) | disables any single control with a tooltip when `allowed` is false |
| `ActionRequiredLabel` | the red "Action required" label (the table row marker uses it) |
| `SummaryTile` | `<SummaryTile icon={<Inventory2OutlinedIcon />} label="Total SKUs" value={1280} hint="..." color="#1976d2" />` |
| `StatusChip`, `StockStatusChip`, `CapacityChip`, `FactoryTag` (`status-chips`) | `StockStatusChip status="ok" \| "low" \| "outOfStock" \| "oosConflict"`; `CapacityChip percent={40}` → Full / Reduced / Offline (0% is solid red); `FactoryTag code="HU" dimmed?`; `StatusChip label colors={statusColors.success}` for anything else |
| `ExcelImportDialog` | upload → validation → confirm, see below |
| `useFactories()` (`store/selectors/factoriesSelector.ts`) | `{ factories, isLoading }`, loaded once from `GET /v1/factories` |

```tsx
<ExcelImportDialog
  open={open}
  title="Import producible SKUs"
  description="Upload the approved .xlsx file."
  onClose={() => setOpen(false)}
  onValidate={file => skuImportService.validate(file)}   // -> ImportValidationResult { totalRows, validRows, errors: [{ row, field, message }] }
  onConfirm={file => skuImportService.import(file)}
/>
```

The dialog lists the per-row errors returned by `onValidate` and enables "Import" only when there are none.

---

## Module ownership rules

A module agent owns exactly `src/app/<module>/**`:

| File | Purpose |
|---|---|
| `routes.tsx` | default export: the module's own `<Switch>` with every page under the module's path prefixes; use `PrivateRoute requiredPermission` for pages stricter than `*.View` |
| `navigation.ts` | default export: the module's `ModuleNavItem[]` |
| `store/index.ts` | default export: the module reducer, already registered in the root reducer under `componentsCatalog` / `processes` / `skus` / `balancer` |
| anything else in the folder | pages, module components, module services, module entity types |

A module agent may: use everything in `src/components`, `src/core`, `src/helpers`, `src/services`, `src/store/selectors`,
`src/utils`, `src/entities`, `src/constants/urls.ts` and `bo-frontend` read-only; replace the kit-demo placeholders in its
own `routes.tsx`; add module-specific API urls, services and types inside its own folder.

A module agent may not edit: `src/router.tsx`, `src/navigation.ts`, `src/app.tsx`, `src/index.tsx`,
`src/store/rootReducer.ts`, `src/core/**`, `src/constants/urls.ts`, `src/components/**`, `src/helpers/**`,
`src/services/**`, `src/entities/**`, other modules' folders, `bo-frontend/**`, `index.html`, `vite.config.mts`,
`tsconfig.json`. A new npm dependency is the only allowed change to `package.json` / `package-lock.json` and must be
listed in the agent's `spec/questions/<agent>.md`. A missing kit feature is also logged there instead of patching the kit.

`src/app/components/kit-demo/` is removed once all four modules have replaced their placeholders.

---

## Deployment

`Dockerfile` builds with `node:20-alpine` (`npm ci --legacy-peer-deps`, `npm run build`) and serves `dist/` with
nginx; `docker-entrypoint.sh` fills the `window.env` placeholders from the container environment. The build context
must contain the checked-out `bo-frontend` submodule.
