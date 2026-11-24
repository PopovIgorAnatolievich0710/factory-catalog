using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class MasterCatalogSyncJobTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 3, 0, 0, DateTimeKind.Utc);
    private const long RunId = 42;

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IMasterCatalogClient> client;
    private Mock<ISyncRunTracker> tracker;
    private List<Sku> skus;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        client = new Mock<IMasterCatalogClient>();
        tracker = new Mock<ISyncRunTracker>();
        tracker.Setup(t => t.Start(SyncTypes.MasterCatalogSkus, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(RunId);
        skus = db.SetupSet(x => x.Skus);
        db.SetupSet(x => x.SkuAliases);
        db.SetupSet(x => x.Components);
        db.SetupSet(x => x.Processes);
        db.SetupSet(x => x.SyncRuns);
    }

    private MasterCatalogSyncJob CreateJob()
    {
        var history = new Mock<IHistoryWriter>();
        return new MasterCatalogSyncJob(
            db.Object,
            client.Object,
            tracker.Object,
            new MasterCatalogSkuImporter(db.Object, history.Object, TestDb.FixedTime(Now), NullLogger<MasterCatalogSkuImporter>.Instance),
            new SkuMappingValidator(db.Object, history.Object),
            NullLogger<MasterCatalogSyncJob>.Instance);
    }

    private static MasterCatalogSku Item(string component = null)
    {
        return new MasterCatalogSku(
            Guid.NewGuid(), "SITE", "Name", "manufactured", "active", null,
            component == null ? null : [new MasterCatalogSkuComponent(component, 1, "static")], null);
    }

    [Test]
    public async Task Run_PullsEveryPageValidatesAndCompletesTheRun()
    {
        client.Setup(c => c.GetChangedSkus(null, 1, MasterCatalogSyncJob.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MasterCatalogSkuPage([Item(), Item("ERP-MISSING")], true));
        client.Setup(c => c.GetChangedSkus(null, 2, MasterCatalogSyncJob.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MasterCatalogSkuPage([Item(), Item() with { SiteSku = null }], false));

        await CreateJob().Run(CancellationToken.None);

        Assert.That(skus, Has.Count.EqualTo(3));
        Assert.That(skus.Count(s => s.ValidationStatus == SkuValidationStatus.Invalid), Is.EqualTo(1));
        tracker.Verify(t => t.Start(SyncTypes.MasterCatalogSkus, MasterCatalogSyncJob.ScheduleTrigger, It.IsAny<CancellationToken>()));
        tracker.Verify(t => t.Complete(RunId, 3, 0, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunStarted_PullsChangesSinceTheLastSuccessfulRun()
    {
        var lastStart = Now.AddDays(-1);
        db.SetupSet(x => x.SyncRuns,
            new SyncRun { SyncType = SyncTypes.MasterCatalogSkus, Status = SyncRunStatus.Succeeded, StartedAt = lastStart },
            new SyncRun { SyncType = SyncTypes.MasterCatalogSkus, Status = SyncRunStatus.Failed, StartedAt = Now.AddHours(-1) },
            new SyncRun { SyncType = SyncTypes.SapComponents, Status = SyncRunStatus.Succeeded, StartedAt = Now });
        client.Setup(c => c.GetChangedSkus(It.IsAny<DateTime?>(), 1, MasterCatalogSyncJob.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MasterCatalogSkuPage([], false));

        await CreateJob().RunStarted(RunId, CancellationToken.None);

        client.Verify(c => c.GetChangedSkus(lastStart, 1, MasterCatalogSyncJob.PageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void RunStarted_ClientFails_FailureRecordedOnTheRun()
    {
        client.Setup(c => c.GetChangedSkus(It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("master catalog is down"));

        Assert.ThrowsAsync<HttpRequestException>(() => CreateJob().RunStarted(RunId, CancellationToken.None));

        tracker.Verify(t => t.Fail(RunId, "master catalog is down", It.IsAny<CancellationToken>()), Times.Once);
        tracker.Verify(t => t.Complete(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Run_SyncAlreadyRunning_ScheduledRunIsSkipped()
    {
        tracker.Setup(t => t.Start(SyncTypes.MasterCatalogSkus, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("running"));

        await CreateJob().Run(CancellationToken.None);

        client.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Trigger_StartsTheRunAndEnqueuesTheJob()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns("admin@tenengroup.com");
        var backgroundJobs = new Mock<IBackgroundJobClient>();

        await new TriggerMasterCatalogSyncCommandHandler(tracker.Object, currentUser.Object, backgroundJobs.Object)
            .Handle(new TriggerMasterCatalogSyncCommand(), CancellationToken.None);

        tracker.Verify(t => t.Start(SyncTypes.MasterCatalogSkus, "admin@tenengroup.com", It.IsAny<CancellationToken>()), Times.Once);
        backgroundJobs.Verify(b => b.Create(
            It.Is<Job>(j => j.Method.Name == nameof(IMasterCatalogSyncJob.RunStarted) && (long)j.Args[0] == RunId),
            It.IsAny<IState>()), Times.Once);
    }

    [Test]
    public void Trigger_SyncAlreadyRunning_ConflictAndNothingEnqueued()
    {
        tracker.Setup(t => t.Start(SyncTypes.MasterCatalogSkus, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("running"));
        var backgroundJobs = new Mock<IBackgroundJobClient>();

        Assert.ThrowsAsync<ConflictException>(() =>
            new TriggerMasterCatalogSyncCommandHandler(tracker.Object, new Mock<ICurrentUser>().Object, backgroundJobs.Object)
                .Handle(new TriggerMasterCatalogSyncCommand(), CancellationToken.None));

        backgroundJobs.VerifyNoOtherCalls();
    }
}
