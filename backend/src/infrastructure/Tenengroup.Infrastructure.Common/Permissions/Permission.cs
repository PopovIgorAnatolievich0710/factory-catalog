namespace Tenengroup.Infrastructure.Common.Permissions;

public enum Permission
{
    ComponentsView,
    ComponentsEditManualOos,
    ComponentsEditCriticalLevels,
    ComponentsEditAlternatives,
    ComponentsViewCost,
    ComponentsEditCost,
    SyncMonitor,
    SyncTrigger,
    ProcessesView,
    ProcessesEdit,
    ProcessesEditFactorySetup,
    ProcessesEditCapacity,
    ProcessesViewCost,
    SkusView,
    SkusEditProducible,
    SkusEditShareOverride,
    SkusEditGlobalShare,
    SkusEditMappings,
    BalancerView,
    BalancerViewDashboards,
    BalancerEditReservations,
    BalancerViewDimCost,
    BalancerViewDailyPlan,
    BalancerTransfer
}
