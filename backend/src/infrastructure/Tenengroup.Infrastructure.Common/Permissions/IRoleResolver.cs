namespace Tenengroup.Infrastructure.Common.Permissions;

public interface IRoleResolver
{
    Task<RoleResolution> Resolve(IEnumerable<string> tokenGroups, CancellationToken cancellationToken = default);
}

public record RoleResolution(bool IsAuthorizationReady, bool IsRestricted, string[] Roles)
{
    public static RoleResolution NotReady { get; } = new(false, false, []);
}
