using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class SkusModule
{
    public static IServiceCollection AddSkusModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
