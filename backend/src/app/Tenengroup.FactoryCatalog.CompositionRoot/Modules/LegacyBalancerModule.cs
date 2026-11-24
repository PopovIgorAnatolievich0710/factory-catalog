using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class LegacyBalancerModule
{
    public static IServiceCollection AddLegacyBalancerModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
