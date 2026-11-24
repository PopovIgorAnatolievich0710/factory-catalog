using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.Shared.Sync;

public class SyncRunTracker(IFactoryCatalogDbContext db, TimeProvider timeProvider) : ISyncRunTracker
{
    internal static readonly TimeSpan AbandonedAfter = TimeSpan.FromHours(6);
    internal const string AbandonedError = "Abandoned: the run did not finish";

    public async Task<long> Start(string syncType, string triggeredBy, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var running = await db.SyncRuns
            .Where(r => r.SyncType == syncType && r.Status == SyncRunStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var abandoned in running.Where(r => r.StartedAt < now - AbandonedAfter))
        {
            abandoned.Status = SyncRunStatus.Failed;
            abandoned.FinishedAt = now;
            abandoned.Error = AbandonedError;
        }

        if (running.Any(r => r.Status == SyncRunStatus.Running))
        {
            throw AlreadyRunning(syncType);
        }

        var run = new SyncRun
        {
            SyncType = syncType,
            Status = SyncRunStatus.Running,
            StartedAt = now,
            TriggeredBy = triggeredBy
        };
        db.SyncRuns.Add(run);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.DiscardChanges();
            throw AlreadyRunning(syncType);
        }

        return run.Id;
    }

    public async Task Complete(long runId, int added, int updated, int failed, CancellationToken cancellationToken = default)
    {
        var run = await Get(runId, cancellationToken);
        run.Status = SyncRunStatus.Succeeded;
        run.FinishedAt = timeProvider.GetUtcNow().UtcDateTime;
        run.Added = added;
        run.Updated = updated;
        run.Failed = failed;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Fail(long runId, string error, CancellationToken cancellationToken = default)
    {
        db.DiscardChanges();

        var run = await Get(runId, cancellationToken);
        run.Status = SyncRunStatus.Failed;
        run.FinishedAt = timeProvider.GetUtcNow().UtcDateTime;
        run.Error = error;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<SyncStatusDto> GetStatus(string syncType, CancellationToken cancellationToken = default)
    {
        var last = await db.SyncRuns
            .AsNoTracking()
            .Where(r => r.SyncType == syncType)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var lastSuccessAt = await db.SyncRuns
            .AsNoTracking()
            .Where(r => r.SyncType == syncType && r.Status == SyncRunStatus.Succeeded)
            .OrderByDescending(r => r.FinishedAt)
            .Select(r => r.FinishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new SyncStatusDto(
            syncType,
            last?.Status,
            last?.StartedAt,
            last?.FinishedAt,
            lastSuccessAt,
            last?.Added ?? 0,
            last?.Updated ?? 0,
            last?.Failed ?? 0,
            last?.Error);
    }

    private async Task<SyncRun> Get(long runId, CancellationToken cancellationToken)
    {
        return await db.SyncRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken)
               ?? throw new EntityNotFoundException(nameof(SyncRun), runId);
    }

    private static ConflictException AlreadyRunning(string syncType)
    {
        return new ConflictException($"Sync '{syncType}' is already running");
    }
}
