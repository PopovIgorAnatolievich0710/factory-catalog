# Permissions Matrix

> **Requirements:** Phase overview v4; Module 1: Components v5; Module 2: Process capacity v5; Module 3: SKUs v4; Module 4: Balancer v4

Source of truth for roles and permissions used by the backend
(`Tenengroup.Infrastructure.Common/Permissions/PermissionsHelper.cs`) and mirrored by the frontend CASL
abilities. When changing permissions, update the code, `PermissionsHelperTests` and this file.

Derived from the permissions table of `spec/requirements/0-phase-overview.md` (revised Nov 24) and the mockup
change log; differences from `spec/implementation-plan.md` section 3 are marked **(changed)**.

## Roles

| Role (`Role` enum) | AD group / role name (`RoleNames`) | Persona |
|---|---|---|
| `FactoryManager` | `FC-FactoryManagers` | Factory manager |
| `ShiftManager` | `FC-ShiftManagers` | Shift manager — factory-manager permissions in one factory **(changed: new)** |
| `SupplyChainManager` | `FC-SupplyChainManagers` | Supply chain manager |
| `CatalogAdmin` | `FC-CatalogAdmins` | Factory catalog admin |
| `ProductionFloor` | `FC-ProductionFloor` | Production floor user (view only) |
| `OperationsManager` | `FC-OperationsManagers` | Operations / GO manager |
| `Finance` | `FC-Finance` | Finance |
| `BoAdministrator` | `BO_Administrator` | everything |

Token groups are mapped to these names by `GroupPermissions.json` (`GROUP_PERMISSIONS_URL`):

```json
{
  "RoleGroupMapping": {
    "FC-FactoryManagers": ["FC-FactoryManagers", "<entra-group-id>"],
    "FC-ShiftManagers": ["FC-ShiftManagers", "<entra-group-id>"],
    "FC-SupplyChainManagers": ["FC-SupplyChainManagers"],
    "FC-CatalogAdmins": ["FC-CatalogAdmins"],
    "FC-ProductionFloor": ["FC-ProductionFloor"],
    "FC-OperationsManagers": ["FC-OperationsManagers"],
    "FC-Finance": ["FC-Finance"],
    "BO_Administrator": ["BO_Administrator"]
  },
  "RestrictedGroups": []
}
```

A user with several roles gets the union of their permissions. Unrecognised groups grant nothing. A user with no
role cannot open any module (C-01): every module endpoint requires at least its `View` permission.

## Permission matrix

FM = Factory manager and Shift manager (identical), SC = Supply chain, CA = Catalog admin, PF = Production floor,
OM = Operations manager, FI = Finance. `BO_Administrator` has every permission.

| Permission (`Permission` enum) | FM | SC | CA | PF | OM | FI | Notes |
|---|:-:|:-:|:-:|:-:|:-:|:-:|---|
| `ComponentsView` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | |
| `ComponentsEditManualOos` | ✓ own | | | | | | OOS flag and note (C-12) |
| `ComponentsEditCriticalLevels` | | ✓ | | | | | global level and per-factory overrides (C-09) |
| `ComponentsEditAlternatives` | | ✓ | | | | | alternatives and the two use flags (C-07, C-15) |
| `ComponentsViewCost` | | ✓ | ✓ | | | ✓ | **(changed: + SC)** |
| `ComponentsEditCost` | | ✓ | | | | | **(changed: new)** overview table: supply chain edits costs; C-10 keeps cost view-only in Phase 1, so no endpoint uses it yet |
| `SyncMonitor` | | | ✓ | | | | sync run details |
| `SyncTrigger` | | | ✓ | | | | "Sync now" (S-03) |
| `ProcessesView` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | |
| `ProcessesEdit` | | | ✓ | | | | process specifications, new process, deactivate, import (P-01, P-04) |
| `ProcessesEditFactorySetup` | ✓ own | | | | | | **(changed: new)** runs here, machine type, labor cost, max per day (P-02, P-05, P-13) — moved from the catalog admin to the factory manager |
| `ProcessesEditCapacity` | ✓ own | | | | | | capacity %, reason, expected recovery (P-06, P-07) |
| `ProcessesViewCost` | ✓ | | ✓ | | | ✓ | **(changed: + FM, who edits labor cost)** |
| `SkusView` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | |
| `SkusEditProducible` | ✓ own | | | | | | S-13 |
| `SkusEditShareOverride` | ✓ own | | | | | | S-14 |
| `SkusEditGlobalShare` | | | ✓ | | | | S-12 |
| `SkusEditMappings` | | | ✓ | | | | process mapping import (S-10) |
| `BalancerView` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | **(changed: + PF, the overview table gives every persona "View")** |
| `BalancerViewDashboards` | | | | | ✓ | | B-13 … B-15 |
| `BalancerEditReservations` | | | | | ✓ | | B-09 |
| `BalancerViewDimCost` | | | | | | ✓ | B-12 |
| `BalancerViewDailyPlan` | ✓ own | | | | | | **(changed: new)** B-22 |
| `BalancerTransfer` | ✓ own | | | | ✓ any | | **(changed: new)** B-23; operations may transfer from any factory |

"own" = the permission is necessary but not sufficient: the handler must also call
`ICurrentUser.EnsureCanEditFactory(factoryId)`. The set is exposed as `PermissionsHelper.OwnFactoryPermissions`.

## Own-factory rule

`ICurrentUser.CanEditFactory(factoryId)` is true when

1. the caller uses Basic auth (machine-to-machine), or
2. the `permissions.enable_all` feature toggle is off (enforcement disabled), or
3. the caller has the `BO_Administrator` role, or
4. `catalog.user_factory` has a row `(lower(user email), factoryId)`.

`user_factory` is maintained by the catalog admin (no UI in Phase 1 — rows are inserted by ops). A shift manager
is bound to a factory the same way.

## Enforcement toggle

Role enforcement on JWT requests is active only when `catalog.feature_toggle` has
`name = 'permissions.enable_all', is_enabled = true`. Default (no row): every authenticated user passes every
permission and own-factory check, and the frontend grants `manage all`. Basic-auth callers always pass. With the
toggle on, a missing / unreachable `GroupPermissions.json` or a token group listed in `RestrictedGroups` fails
closed (403). Toggles are cached for 10 minutes, the group document for 20.

## Where this lives in code

- Roles, permissions, matrix, group resolution: `backend/src/infrastructure/Tenengroup.Infrastructure.Common/Permissions/`
- Attribute, handler, Basic scheme, registration: `backend/src/infrastructure/Tenengroup.Infrastructure.Common/Auth/`
- Current user and own-factory rule: `backend/src/app/Tenengroup.FactoryCatalog.Business/Shared/Users/CurrentUser.cs`
- Toggle: `backend/src/app/Tenengroup.FactoryCatalog.Business/Shared/FeatureToggles/FeatureToggleService.cs`
- Tests: `backend/src/tests/unit-tests/Tenengroup.FactoryCatalog.Business.UnitTests/{Permissions,Users}/`
- Balancer API: same matrix and handler (shared project); own factory and the toggle come from the catalog
  (`GET /v1/user/me`, `GET /v1/feature-toggle`), see "Authentication and permissions" in
  [`modules/4b-balancer.md`](../modules/4b-balancer.md). Which permission each module endpoint needs is listed in
  `spec/modules/*.md`.

Open permission questions for the product owner: [`open-questions.md`](../open-questions.md) (OQ-15, OQ-17,
OQ-88, OQ-104 – OQ-108).
