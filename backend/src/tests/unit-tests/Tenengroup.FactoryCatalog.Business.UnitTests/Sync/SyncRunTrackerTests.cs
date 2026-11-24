using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Shared.Sync;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Sync;

[TestFixture]
public class SyncRunTrackerTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 2, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
    }

    private SyncRunTracker CreateTracker() => new(db.Object, TestDb.FixedTime(Now));

    [Test]
    public async Task Start_NothingRunning_AddsRunningRun()
    {
        var runs = db.SetupSet(x => x.SyncRuns);

        await CreateTracker().Start(SyncTypes.SapComponents, "admin@tenengroup.com");

        Assert.That(runs, Has.Count.EqualTo(1));
        Assert.That(runs[0].SyncType, Is.EqualTo(SyncTypes.SapComponents));
        Assert.That(runs[0].Status, Is.EqualTo(SyncRunStatus.Running));
        Assert.That(runs[0].StartedAt, Is.EqualTo(Now));
        Assert.That(runs[0].TriggeredBy, Is.EqualTo("admin@tenengroup.com"));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Start_SameTypeRunning_Conflict()
    {
        var runs = db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 1, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Running, StartedAt = Now.AddMinutes(-5) });

        Assert.ThrowsAsync<ConflictException>(() => CreateTracker().Start(SyncTypes.SapComponents, "job"));
        Assert.That(runs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Start_OtherTypeRunning_Allowed()
    {
        var runs = db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 1, SyncType = SyncTypes.MasterCatalogSkus, Status = SyncRunStatus.Running, StartedAt = Now.AddMinutes(-5) });

        await CreateTracker().Start(SyncTypes.SapComponents, "job");

        Assert.That(runs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task Start_AbandonedRun_IsFailedAndReplaced()
    {
        var runs = db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 1, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Running, StartedAt = Now.AddHours(-7) });

        await CreateTracker().Start(SyncTypes.SapComponents, "job");

        Assert.That(runs[0].Status, Is.EqualTo(SyncRunStatus.Failed));
        Assert.That(runs[0].Error, Is.EqualTo(SyncRunTracker.AbandonedError));
        Assert.That(runs[1].Status, Is.EqualTo(SyncRunStatus.Running));
    }

    [Test]
    public void Start_UniqueIndexViolation_Conflict()
    {
        db.SetupSet(x => x.SyncRuns);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException());

        Assert.ThrowsAsync<ConflictException>(() => CreateTracker().Start(SyncTypes.SapComponents, "job"));
        db.Verify(x => x.DiscardChanges(), Times.Once);
    }

    [Test]
    public async Task Complete_StoresCounts()
    {
        var runs = db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 7, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Running, StartedAt = Now.AddMinutes(-4) });

        await CreateTracker().Complete(7, 3, 12, 1);

        Assert.That(runs[0].Status, Is.EqualTo(SyncRunStatus.Succeeded));
        Assert.That(runs[0].FinishedAt, Is.EqualTo(Now));
        Assert.That((runs[0].Added, runs[0].Updated, runs[0].Failed), Is.EqualTo((3, 12, 1)));
    }

    [Test]
    public async Task Fail_DiscardsPendingChangesAndStoresError()
    {
        var runs = db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 7, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Running, StartedAt = Now.AddMinutes(-4) });

        await CreateTracker().Fail(7, "file not found");

        Assert.That(runs[0].Status, Is.EqualTo(SyncRunStatus.Failed));
        Assert.That(runs[0].Error, Is.EqualTo("file not found"));
        db.Verify(x => x.DiscardChanges(), Times.Once);
    }

    [Test]
    public void Complete_UnknownRun_NotFound()
    {
        db.SetupSet(x => x.SyncRuns);

        Assert.ThrowsAsync<EntityNotFoundException>(() => CreateTracker().Complete(99, 0, 0, 0));
    }

    [Test]
    public async Task GetStatus_LastRunAndLastSuccess()
    {
        db.SetupSet(x => x.SyncRuns,
            new SyncRun { Id = 1, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Succeeded, StartedAt = Now.AddDays(-1), FinishedAt = Now.AddDays(-1).AddMinutes(4), Added = 5 },
            new SyncRun { Id = 2, SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Failed, StartedAt = Now, FinishedAt = Now.AddMinutes(1), Error = "boom" },
            new SyncRun { Id = 3, SyncType = SyncTypes.MasterCatalogSkus, Status = SyncRunStatus.Succeeded, StartedAt = Now.AddHours(1), FinishedAt = Now.AddHours(2) });

        var status = await CreateTracker().GetStatus(SyncTypes.SapComponents);

        Assert.That(status.Status, Is.EqualTo(SyncRunStatus.Failed));
        Assert.That(status.StartedAt, Is.EqualTo(Now));
        Assert.That(status.Error, Is.EqualTo("boom"));
        Assert.That(status.Added, Is.EqualTo(0));
        Assert.That(status.LastSuccessAt, Is.EqualTo(Now.AddDays(-1).AddMinutes(4)));
    }

    [Test]
    public async Task GetStatus_NeverRan_EmptyStatus()
    {
        db.SetupSet(x => x.SyncRuns);

        var status = await CreateTracker().GetStatus(SyncTypes.SapComponents);

        Assert.That(status.SyncType, Is.EqualTo(SyncTypes.SapComponents));
        Assert.That(status.Status, Is.Null);
        Assert.That(status.LastSuccessAt, Is.Null);
    }
}
