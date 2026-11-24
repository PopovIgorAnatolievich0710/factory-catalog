using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class ComponentsJobs
{
    public static void RegisterComponentsJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
    }
}
