namespace Tenengroup.Infrastructure.Common.Permissions;

public interface IPermissionEnforcementToggle
{
    Task<bool> IsEnforced(CancellationToken cancellationToken = default);
}
