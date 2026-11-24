using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class ProcessesJobs
{
    public static void RegisterProcessesJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
    }
}
