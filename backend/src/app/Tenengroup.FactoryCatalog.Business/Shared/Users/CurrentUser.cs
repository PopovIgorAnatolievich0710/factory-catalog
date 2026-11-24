using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Shared.Users;

public class CurrentUser(
    IUserIdentity identity,
    IRoleResolver roleResolver,
    IPermissionEnforcementToggle enforcementToggle,
    IFactoryCatalogDbContext db) : ICurrentUser
{
    private IReadOnlyCollection<string> roles;
    private IReadOnlyCollection<string> factoryIds;

    public string Email => identity.Email;

    public string Name => identity.Name;

    public bool IsServiceAccount => identity.IsServiceAccount;

    public async Task<IReadOnlyCollection<string>> GetRoles(CancellationToken cancellationToken = default)
    {
        if (roles != null)
        {
            return roles;
        }

        var resolution = await roleResolver.Resolve(identity.Groups, cancellationToken);
        roles = resolution.IsAuthorizationReady && !resolution.IsRestricted ? resolution.Roles : [];

        return roles;
    }

    public async Task<IReadOnlyCollection<string>> GetFactoryIds(CancellationToken cancellationToken = default)
    {
        if (factoryIds != null)
        {
            return factoryIds;
        }

        var email = identity.Email;
        factoryIds = string.IsNullOrEmpty(email)
            ? []
            : await db.UserFactories
                .Where(u => u.UserEmail == email)
                .Select(u => u.FactoryId)
                .ToListAsync(cancellationToken);

        return factoryIds;
    }

    public async Task<bool> HasPermission(Permission permission, CancellationToken cancellationToken = default)
    {
        if (identity.IsServiceAccount || !await enforcementToggle.IsEnforced(cancellationToken))
        {
            return true;
        }

        return PermissionsHelper.HasPermission(await GetRoles(cancellationToken), permission);
    }

    public async Task<bool> CanEditFactory(string factoryId, CancellationToken cancellationToken = default)
    {
        if (identity.IsServiceAccount || !await enforcementToggle.IsEnforced(cancellationToken))
        {
            return true;
        }

        if (PermissionsHelper.IsAdministrator(await GetRoles(cancellationToken)))
        {
            return true;
        }

        return (await GetFactoryIds(cancellationToken)).Contains(factoryId);
    }

    public async Task EnsureCanEditFactory(string factoryId, CancellationToken cancellationToken = default)
    {
        if (!await CanEditFactory(factoryId, cancellationToken))
        {
            throw new ForbiddenException($"User '{identity.Email}' may not edit factory '{factoryId}'");
        }
    }
}
