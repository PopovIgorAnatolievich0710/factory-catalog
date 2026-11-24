using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Tenengroup.Infrastructure.Common.Auth;

public class BasicAuthenticationHandler(
    IOptionsMonitor<BasicAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<BasicAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Basic";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? authorizationHeader = Request.Headers[HeaderNames.Authorization];
        if (!BasicCredentials.TryRead(authorizationHeader, out var username, out _))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var matches = BasicCredentials.Matches(
            authorizationHeader,
            configuration[Constants.ParameterNames.ApiServiceUsername],
            configuration[Constants.ParameterNames.ApiServiceUserPassword]);

        if (!matches)
        {
            Logger.LogWarning("Basic authentication rejected for user '{Username}'", username);
            return Task.FromResult(AuthenticateResult.Fail("Invalid login or password."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
