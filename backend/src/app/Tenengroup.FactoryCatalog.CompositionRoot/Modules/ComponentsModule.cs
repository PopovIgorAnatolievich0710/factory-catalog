using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class ComponentsModule
{
    public static IServiceCollection AddComponentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
