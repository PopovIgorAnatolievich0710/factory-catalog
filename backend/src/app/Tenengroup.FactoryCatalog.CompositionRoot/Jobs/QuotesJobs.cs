using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public static class QuotesJobs
{
    public static void RegisterQuotesJobs(this IRecurringJobManager recurringJobs, IConfiguration configuration)
    {
    }
}
