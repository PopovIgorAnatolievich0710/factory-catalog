using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Infrastructure.Common.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddTenengroupAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHttpClient(GroupPermissionsProvider.HttpClientName);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, null)
            .AddMicrosoftIdentityWebApi(configuration);

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, BasicAuthenticationHandler.SchemeName)
                .Build();
        });

        services.AddScoped<IAuthorizationHandler, AzureAuthorizationHandler>();
        services.AddScoped<IGroupPermissionsProvider, GroupPermissionsProvider>();
        services.AddScoped<IRoleResolver, RoleResolver>();
        services.AddScoped<IUserIdentity, HttpUserIdentity>();

        return services;
    }
}
