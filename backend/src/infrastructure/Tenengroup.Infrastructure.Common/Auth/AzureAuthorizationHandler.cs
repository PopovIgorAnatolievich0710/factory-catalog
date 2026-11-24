using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Infrastructure.Common.Auth;

public class AzureAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IRoleResolver roleResolver,
    IPermissionEnforcementToggle enforcementToggle) : AuthorizationHandler<AzureAuthenticatedRequirement>
{
    public const string GroupsClaimType = "groups";

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AzureAuthenticatedRequirement requirement)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (context.User.Identity?.AuthenticationType == BasicAuthenticationHandler.SchemeName)
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        if (!await enforcementToggle.IsEnforced(cancellationToken))
        {
            context.Succeed(requirement);
            return;
        }

        var tokenGroups = context.User.FindAll(GroupsClaimType).Select(c => c.Value);
        var resolution = await roleResolver.Resolve(tokenGroups, cancellationToken);

        if (!resolution.IsAuthorizationReady || resolution.IsRestricted)
        {
            context.Fail();
            return;
        }

        if (requirement.Permission == null ||
            PermissionsHelper.HasPermission(resolution.Roles, requirement.Permission.Value))
        {
            context.Succeed(requirement);
        }
    }
}
