using Hangfire.Dashboard;
using Microsoft.Net.Http.Headers;
using Tenengroup.Infrastructure.Common.Auth;

namespace Tenengroup.Infrastructure.Common.Hangfire;

public class HangfireDashboardAuthorizationFilter(string username, string password) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (BasicCredentials.Matches(httpContext.Request.Headers[HeaderNames.Authorization], username, password))
        {
            return true;
        }

        httpContext.Response.Headers[HeaderNames.WWWAuthenticate] = "Basic realm=\"Hangfire\"";
        return false;
    }
}
