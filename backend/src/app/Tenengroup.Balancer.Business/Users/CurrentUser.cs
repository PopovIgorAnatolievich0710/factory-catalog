using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.Business.Users;

public class CurrentUser(
    IUserIdentity identity,
    IRoleResolver roleResolver,
    IPermissionEnforcementToggle enforcementToggle,
    ICatalogClient catalogClient,
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private IReadOnlyCollection<string> factoryIds;

    public string Actor => string.IsNullOrEmpty(identity.Email) ? identity.Name : identity.Email;

    public async Task<bool> CanAccessAnyFactory(CancellationToken cancellationToken)
    {
        if (identity.IsServiceAccount || !await enforcementToggle.IsEnforced(cancellationToken))
        {
            return true;
        }

        var resolution = await roleResolver.Resolve(identity.Groups, cancellationToken);
        if (!resolution.IsAuthorizationReady || resolution.IsRestricted)
        {
            return false;
        }

        return PermissionsHelper.IsAdministrator(resolution.Roles)
               || resolution.Roles.Contains(RoleNames.OperationsManager, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyCollection<string>> GetFactoryIds(CancellationToken cancellationToken)
    {
        if (factoryIds != null)
        {
            return factoryIds;
        }

        var authorization = httpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].ToString();
        factoryIds = string.IsNullOrEmpty(authorization) || identity.IsServiceAccount
            ? []
            : (await catalogClient.GetUser(authorization, cancellationToken)).FactoryIds;

        return factoryIds;
    }

    public async Task<bool> CanAccessFactory(string factoryId, CancellationToken cancellationToken)
    {
        return await CanAccessAnyFactory(cancellationToken)
               || (await GetFactoryIds(cancellationToken)).Contains(factoryId);
    }

    public async Task EnsureCanAccessFactory(string factoryId, CancellationToken cancellationToken)
    {
        if (!await CanAccessFactory(factoryId, cancellationToken))
        {
            throw new ForbiddenException($"User '{Actor}' has no access to factory '{factoryId}'");
        }
    }
}
