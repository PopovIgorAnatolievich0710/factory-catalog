using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class LegacyBalancerJobs
{
    public static void RegisterLegacyBalancerJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
    }
}
