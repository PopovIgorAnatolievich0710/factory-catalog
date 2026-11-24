using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class SkusJobs
{
    public static void RegisterSkusJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
    }
}
