using Microsoft.Extensions.Hosting;
using Serilog;

namespace Tenengroup.Infrastructure.Common.Web;

public static class SerilogExtensions
{
    public static IHostBuilder UseConfiguredSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .Filter.ByExcluding(logEvent =>
                logEvent.Properties.TryGetValue("RequestPath", out var requestPath)
                && requestPath.ToString().Trim('"').Equals("/ping", StringComparison.OrdinalIgnoreCase))
            .Enrich.FromLogContext()
            .WriteTo.Console());
    }
}
