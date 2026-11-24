using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Tenengroup.Infrastructure.Common.Auth;

public class HttpUserIdentity(IHttpContextAccessor httpContextAccessor) : IUserIdentity
{
    public const string SystemUserName = "system";

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsServiceAccount => User?.Identity?.AuthenticationType == BasicAuthenticationHandler.SchemeName;

    public string Email =>
        (User?.FindFirstValue("preferred_username")
         ?? User?.FindFirstValue("email")
         ?? User?.FindFirstValue("upn")
         ?? User?.FindFirstValue(ClaimTypes.Email)
         ?? User?.FindFirstValue(ClaimTypes.Upn)
         ?? string.Empty).ToLowerInvariant();

    public string Name =>
        User?.FindFirstValue("name")
        ?? User?.FindFirstValue(ClaimTypes.Name)
        ?? SystemUserName;

    public IReadOnlyCollection<string> Groups =>
        User?.FindAll(AzureAuthorizationHandler.GroupsClaimType).Select(c => c.Value).ToArray() ?? [];
}
