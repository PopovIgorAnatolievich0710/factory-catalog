using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Tenengroup.Infrastructure.Common.Auth;

namespace Tenengroup.Balancer.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class ServiceAccountOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.AuthenticationType != BasicAuthenticationHandler.SchemeName)
        {
            context.Result = new ForbidResult();
        }
    }
}
