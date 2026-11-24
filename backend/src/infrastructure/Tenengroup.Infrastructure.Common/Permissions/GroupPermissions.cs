namespace Tenengroup.Infrastructure.Common.Permissions;

public class GroupPermissions
{
    public Dictionary<string, string[]> RoleGroupMapping { get; set; } = new();

    public string[] RestrictedGroups { get; set; } = [];
}
