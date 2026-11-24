namespace Tenengroup.Infrastructure.Common.Permissions;

public static class RoleNames
{
    public const string FactoryManager = "FC-FactoryManagers";
    public const string ShiftManager = "FC-ShiftManagers";
    public const string SupplyChainManager = "FC-SupplyChainManagers";
    public const string CatalogAdmin = "FC-CatalogAdmins";
    public const string ProductionFloor = "FC-ProductionFloor";
    public const string OperationsManager = "FC-OperationsManagers";
    public const string Finance = "FC-Finance";
    public const string BoAdministrator = "BO_Administrator";

    private static readonly Dictionary<string, Role> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        [FactoryManager] = Role.FactoryManager,
        [ShiftManager] = Role.ShiftManager,
        [SupplyChainManager] = Role.SupplyChainManager,
        [CatalogAdmin] = Role.CatalogAdmin,
        [ProductionFloor] = Role.ProductionFloor,
        [OperationsManager] = Role.OperationsManager,
        [Finance] = Role.Finance,
        [BoAdministrator] = Role.BoAdministrator
    };

    public static bool TryParse(string name, out Role role) => ByName.TryGetValue(name, out role);
}
