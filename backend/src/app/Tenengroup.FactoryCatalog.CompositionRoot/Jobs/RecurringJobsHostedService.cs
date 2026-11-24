using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Jobs;

public class RecurringJobsHostedService(IRecurringJobManager recurringJobs, IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobs.RegisterComponentsJobs(configuration);
        recurringJobs.RegisterProcessesJobs(configuration);
        recurringJobs.RegisterSkusJobs(configuration);
        recurringJobs.RegisterQuotesJobs(configuration);
        recurringJobs.RegisterLegacyBalancerJobs(configuration);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
