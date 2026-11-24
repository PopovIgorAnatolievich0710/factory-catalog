using System.Reflection;
using Hangfire;
using Tenengroup.FactoryCatalog.CompositionRoot;
using Tenengroup.Infrastructure.Common;
using Tenengroup.Infrastructure.Common.Auth;
using Tenengroup.Infrastructure.Common.Hangfire;
using Tenengroup.Infrastructure.Common.Web;

namespace Tenengroup.FactoryCatalog.Api;

public class Startup(IConfiguration configuration)
{
    private const string ApiTitle = "Factory Catalog API";
    private static readonly Assembly ApiAssembly = typeof(Startup).Assembly;
    private static readonly string Version = ApiAssembly.GetName().Version?.ToString() ?? "1.0.0";

    public void ConfigureServices(IServiceCollection services)
    {
        services.RegisterRoot(configuration, DbConnectionString.Build(configuration));
        services.AddTenengroupAuth(configuration);
        services.AddTenengroupCors(configuration);
        services.AddTenengroupControllers();
        services.AddTenengroupSwagger(ApiTitle, Version, Path.Combine(AppContext.BaseDirectory, $"{ApiAssembly.GetName().Name}.xml"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseTenengroupSwagger(ApiTitle, Version);
        app.UsePing();
        app.UseCorrelationId();
        app.UseRouting();
        app.UseCors(WebExtensions.CorsPolicyName);
        app.UseGlobalExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();
            endpoints.MapHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization =
                [
                    new HangfireDashboardAuthorizationFilter(
                        configuration[Constants.ParameterNames.ApiServiceUsername],
                        configuration[Constants.ParameterNames.ApiServiceUserPassword])
                ]
            });
        });
    }
}
