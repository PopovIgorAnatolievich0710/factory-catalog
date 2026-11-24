using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.CompositionRoot.Jobs;
using Tenengroup.FactoryCatalog.CompositionRoot.Modules;
using Tenengroup.FactoryCatalog.DataAccess;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Aws;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.CompositionRoot;

public static class Root
{
    public static IServiceCollection RegisterRoot(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();
        services.AddScoped<IRequestRunner, HandlerBasedRequestRunner>();

        services.AddDbContext<FactoryCatalogDbContext>(options =>
            FactoryCatalogDbContextOptions.Configure(options, connectionString));
        services.AddScoped<IFactoryCatalogDbContext>(sp => sp.GetRequiredService<FactoryCatalogDbContext>());

        services.AddAwsInfrastructure(configuration);
        services.AddHangfireModule(connectionString);
        services.AddHostedService<RecurringJobsHostedService>();

        services.AddSharedModule();
        services.AddComponentsModule(configuration);
        services.AddProcessesModule(configuration);
        services.AddSkusModule(configuration);
        services.AddQuotesModule(configuration);
        services.AddLegacyBalancerModule(configuration);

        return services;
    }
}
