namespace Tenengroup.Infrastructure.Common.Permissions;

public static class PermissionsHelper
{
    private static readonly Permission[] FactoryManagerPermissions =
    [
        Permission.ComponentsView,
        Permission.ComponentsEditManualOos,
        Permission.ProcessesView,
        Permission.ProcessesEditFactorySetup,
        Permission.ProcessesEditCapacity,
        Permission.ProcessesViewCost,
        Permission.SkusView,
        Permission.SkusEditProducible,
        Permission.SkusEditShareOverride,
        Permission.BalancerView,
        Permission.BalancerViewDailyPlan,
        Permission.BalancerTransfer
    ];

    private static readonly Dictionary<Role, Permission[]> RolePermissions = new()
    {
        [Role.BoAdministrator] = Enum.GetValues<Permission>(),

        [Role.FactoryManager] = FactoryManagerPermissions,

        [Role.ShiftManager] = FactoryManagerPermissions,

        [Role.SupplyChainManager] =
        [
            Permission.ComponentsView,
            Permission.ComponentsEditCriticalLevels,
            Permission.ComponentsEditAlternatives,
            Permission.ComponentsViewCost,
            Permission.ComponentsEditCost,
            Permission.ProcessesView,
            Permission.SkusView,
            Permission.BalancerView
        ],

        [Role.CatalogAdmin] =
        [
            Permission.ComponentsView,
            Permission.ComponentsViewCost,
            Permission.SyncMonitor,
            Permission.SyncTrigger,
            Permission.ProcessesView,
            Permission.ProcessesEdit,
            Permission.ProcessesViewCost,
            Permission.SkusView,
            Permission.SkusEditGlobalShare,
            Permission.SkusEditMappings,
            Permission.BalancerView
        ],

        [Role.ProductionFloor] =
        [
            Permission.ComponentsView,
            Permission.ProcessesView,
            Permission.SkusView,
            Permission.BalancerView
        ],

        [Role.OperationsManager] =
        [
            Permission.ComponentsView,
            Permission.ProcessesView,
            Permission.SkusView,
            Permission.BalancerView,
            Permission.BalancerViewDashboards,
            Permission.BalancerEditReservations,
            Permission.BalancerTransfer
        ],

        [Role.Finance] =
        [
            Permission.ComponentsView,
            Permission.ComponentsViewCost,
            Permission.ProcessesView,
            Permission.ProcessesViewCost,
            Permission.SkusView,
            Permission.BalancerView,
            Permission.BalancerViewDimCost
        ]
    };

    public static readonly IReadOnlySet<Permission> OwnFactoryPermissions = new HashSet<Permission>
    {
        Permission.ComponentsEditManualOos,
        Permission.ProcessesEditFactorySetup,
        Permission.ProcessesEditCapacity,
        Permission.SkusEditProducible,
        Permission.SkusEditShareOverride,
        Permission.BalancerViewDailyPlan
    };

    public static bool HasPermission(IEnumerable<string> roleNames, Permission required)
    {
        return roleNames.Any(name =>
            RoleNames.TryParse(name, out var role) && RolePermissions[role].Contains(required));
    }

    public static bool IsAdministrator(IEnumerable<string> roleNames)
    {
        return roleNames.Any(name => RoleNames.TryParse(name, out var role) && role == Role.BoAdministrator);
    }
}
