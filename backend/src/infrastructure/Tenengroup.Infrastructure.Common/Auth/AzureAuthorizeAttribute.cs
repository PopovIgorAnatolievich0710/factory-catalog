using Microsoft.AspNetCore.Authorization;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Infrastructure.Common.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class AzureAuthorizeAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    private readonly Permission? permission;

    public AzureAuthorizeAttribute()
    {
    }

    public AzureAuthorizeAttribute(Permission permission)
    {
        this.permission = permission;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new AzureAuthenticatedRequirement(permission);
    }
}
