using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class ProcessesModule
{
    public static IServiceCollection AddProcessesModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
