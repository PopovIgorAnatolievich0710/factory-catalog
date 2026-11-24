using NUnit.Framework;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Permissions;

[TestFixture]
public class PermissionsHelperTests
{
    private static readonly Dictionary<string, Permission[]> Expected = new()
    {
        [RoleNames.FactoryManager] =
        [
            Permission.ComponentsView, Permission.ComponentsEditManualOos,
            Permission.ProcessesView, Permission.ProcessesEditFactorySetup, Permission.ProcessesEditCapacity, Permission.ProcessesViewCost,
            Permission.SkusView, Permission.SkusEditProducible, Permission.SkusEditShareOverride,
            Permission.BalancerView, Permission.BalancerViewDailyPlan, Permission.BalancerTransfer
        ],
        [RoleNames.ShiftManager] =
        [
            Permission.ComponentsView, Permission.ComponentsEditManualOos,
            Permission.ProcessesView, Permission.ProcessesEditFactorySetup, Permission.ProcessesEditCapacity, Permission.ProcessesViewCost,
            Permission.SkusView, Permission.SkusEditProducible, Permission.SkusEditShareOverride,
            Permission.BalancerView, Permission.BalancerViewDailyPlan, Permission.BalancerTransfer
        ],
        [RoleNames.SupplyChainManager] =
        [
            Permission.ComponentsView, Permission.ComponentsEditCriticalLevels, Permission.ComponentsEditAlternatives,
            Permission.ComponentsViewCost, Permission.ComponentsEditCost,
            Permission.ProcessesView, Permission.SkusView, Permission.BalancerView
        ],
        [RoleNames.CatalogAdmin] =
        [
            Permission.ComponentsView, Permission.ComponentsViewCost, Permission.SyncMonitor, Permission.SyncTrigger,
            Permission.ProcessesView, Permission.ProcessesEdit, Permission.ProcessesViewCost,
            Permission.SkusView, Permission.SkusEditGlobalShare, Permission.SkusEditMappings, Permission.BalancerView
        ],
        [RoleNames.ProductionFloor] =
        [
            Permission.ComponentsView, Permission.ProcessesView, Permission.SkusView, Permission.BalancerView
        ],
        [RoleNames.OperationsManager] =
        [
            Permission.ComponentsView, Permission.ProcessesView, Permission.SkusView,
            Permission.BalancerView, Permission.BalancerViewDashboards, Permission.BalancerEditReservations, Permission.BalancerTransfer
        ],
        [RoleNames.Finance] =
        [
            Permission.ComponentsView, Permission.ComponentsViewCost, Permission.ProcessesView, Permission.ProcessesViewCost,
            Permission.SkusView, Permission.BalancerView, Permission.BalancerViewDimCost
        ],
        [RoleNames.BoAdministrator] = Enum.GetValues<Permission>()
    };

    private static IEnumerable<TestCaseData> Matrix()
    {
        return from role in Expected.Keys
            from permission in Enum.GetValues<Permission>()
            select new TestCaseData(role, permission, Expected[role].Contains(permission))
                .SetName($"{role}_{permission}");
    }

    [TestCaseSource(nameof(Matrix))]
    public void HasPermission_FollowsMatrix(string role, Permission permission, bool expected)
    {
        Assert.That(PermissionsHelper.HasPermission([role], permission), Is.EqualTo(expected));
    }

    [Test]
    public void HasPermission_UnknownRole_GrantsNothing()
    {
        Assert.That(
            Enum.GetValues<Permission>().Any(p => PermissionsHelper.HasPermission(["SHP-Packers"], p)),
            Is.False);
    }

    [Test]
    public void HasPermission_NoRoles_GrantsNothing()
    {
        Assert.That(PermissionsHelper.HasPermission([], Permission.ComponentsView), Is.False);
    }

    [Test]
    public void HasPermission_SeveralRoles_UnionOfPermissions()
    {
        string[] roles = [RoleNames.ProductionFloor, RoleNames.Finance];

        Assert.That(PermissionsHelper.HasPermission(roles, Permission.BalancerViewDimCost), Is.True);
        Assert.That(PermissionsHelper.HasPermission(roles, Permission.ProcessesEdit), Is.False);
    }

    [Test]
    public void HasPermission_RoleNameIsCaseInsensitive()
    {
        Assert.That(PermissionsHelper.HasPermission(["fc-catalogadmins"], Permission.SyncTrigger), Is.True);
    }

    [Test]
    public void IsAdministrator_OnlyForBoAdministrator()
    {
        Assert.That(PermissionsHelper.IsAdministrator([RoleNames.CatalogAdmin, RoleNames.BoAdministrator]), Is.True);
        Assert.That(PermissionsHelper.IsAdministrator([RoleNames.CatalogAdmin]), Is.False);
    }
}
