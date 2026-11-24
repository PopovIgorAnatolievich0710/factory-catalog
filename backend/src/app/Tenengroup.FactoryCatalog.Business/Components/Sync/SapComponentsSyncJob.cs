using Microsoft.Extensions.Logging;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Components.Sync;

public class SapComponentsSyncJob(ISapComponentsSyncService syncService, ILogger<SapComponentsSyncJob> logger)
    : ISapComponentsSyncJob
{
    public const string TriggeredBy = "schedule";

    public async Task Run(CancellationToken cancellationToken)
    {
        try
        {
            await syncService.Run(TriggeredBy, cancellationToken);
        }
        catch (ConflictException e)
        {
            logger.LogWarning("Scheduled SAP components sync skipped: {Reason}", e.Message);
        }
    }
}
