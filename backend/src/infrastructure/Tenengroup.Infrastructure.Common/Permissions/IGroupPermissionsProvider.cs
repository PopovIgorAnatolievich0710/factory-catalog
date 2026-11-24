namespace Tenengroup.Infrastructure.Common.Permissions;

public interface IGroupPermissionsProvider
{
    Task<GroupPermissions?> Get(CancellationToken cancellationToken = default);
}
