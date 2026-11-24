namespace Tenengroup.Infrastructure.Common.Permissions;

public class RoleResolver(IGroupPermissionsProvider provider) : IRoleResolver
{
    public async Task<RoleResolution> Resolve(IEnumerable<string> tokenGroups, CancellationToken cancellationToken = default)
    {
        var permissions = await provider.Get(cancellationToken);
        if (permissions == null)
        {
            return RoleResolution.NotReady;
        }

        var groups = tokenGroups.ToArray();

        if (groups.Any(g => permissions.RestrictedGroups.Contains(g, StringComparer.OrdinalIgnoreCase)))
        {
            return new RoleResolution(true, true, []);
        }

        var groupToRole = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (role, aliases) in permissions.RoleGroupMapping)
        {
            foreach (var alias in aliases ?? [])
            {
                groupToRole.TryAdd(alias, role);
            }
        }

        var roles = groups
            .Where(groupToRole.ContainsKey)
            .Select(g => groupToRole[g])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RoleResolution(true, false, roles);
    }
}
