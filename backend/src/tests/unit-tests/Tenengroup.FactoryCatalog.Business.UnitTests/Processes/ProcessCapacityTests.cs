using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Processes;

[TestFixture]
public class ProcessCapacityTests
{
    private const string Email = "fm.110@tenengroup.com";
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;
    private Mock<IHistoryWriter> historyWriter;
    private ProcessFactory row;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns(Email);
        currentUser.Setup(u => u.EnsureCanEditFactory("125", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("not your factory"));
        historyWriter = new Mock<IHistoryWriter>();

        row = new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "110", RunsHere = true, CapacityPct = 100, MaxPerDay = 100 };
        db.SetupSet(x => x.Processes,
            new Process { ProcessId = "ENGRAVING", Name = "Engraving", Type = ProcessType.Machine, Status = ProcessStatus.Active },
            new Process { ProcessId = "POLISH", Name = "Polishing", Type = ProcessType.Work, Status = ProcessStatus.Active },
            new Process { ProcessId = "OLD", Name = "Old", Type = ProcessType.Work, Status = ProcessStatus.Inactive });
        db.SetupSet(x => x.ProcessFactories,
            row,
            new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "125", RunsHere = true, CapacityPct = 0, CapacityReason = "Laser down" },
            new ProcessFactory { ProcessId = "POLISH", FactoryId = "110", RunsHere = false });
        db.SetupSet(x => x.Factories,
            new Factory { Id = "110", Name = "Factory 110", IsActive = true },
            new Factory { Id = "125", Name = "Factory 125", IsActive = true },
            new Factory { Id = "900", Name = "Closed", IsActive = false });
        db.SetupSet(x => x.Quotes);
        db.SetupSet(x => x.QuoteSets);
        db.SetupSet(x => x.SkuProcesses);
    }

    private Task Set(string factoryId, int pct, string reason = null, DateOnly? recovery = null, string processId = "ENGRAVING")
    {
        return new SetProcessCapacityCommandHandler(db.Object, currentUser.Object, historyWriter.Object, TestDb.FixedTime(Now))
            .Handle(new SetProcessCapacityCommand(processId, factoryId, pct, reason, recovery), CancellationToken.None);
    }

    [TestCase(100, 100, 20, 100, 80)]
    [TestCase(20, 50, 5, 10, 25)]
    [TestCase(20, 50, 15, 10, 0)]
    [TestCase(1000, 80, 200, 800, 60)]
    public void Today_FollowsTheStoryFormula(int max, int capacityPct, int used, int available, int remainingPct)
    {
        var today = ProcessCapacityCalculator.Today(max, capacityPct, used);

        Assert.That(today.Available, Is.EqualTo(available));
        Assert.That(today.Used, Is.EqualTo(used));
        Assert.That(today.RemainingPct, Is.EqualTo(remainingPct));
    }

    [Test]
    public void Today_NoMaximum_IsUnknown()
    {
        Assert.That(ProcessCapacityCalculator.Today(null, 100, 0), Is.Null);
        Assert.That(ProcessCapacityCalculator.Today(0, 100, 0), Is.Null);
    }

    [TestCase(0, CapacityStatus.Offline)]
    [TestCase(10, CapacityStatus.Reduced)]
    [TestCase(90, CapacityStatus.Reduced)]
    [TestCase(100, CapacityStatus.Full)]
    public void Status_ByPercent(int pct, CapacityStatus expected)
    {
        Assert.That(ProcessCapacityCalculator.Status(pct), Is.EqualTo(expected));
    }

    [TestCase(-10)]
    [TestCase(5)]
    [TestCase(55)]
    [TestCase(110)]
    public void Set_NotATenPercentStep_Rejected(int pct)
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Set("110", pct));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "capacityPct" }));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Set_EveryStepFromZeroToHundred_Accepted()
    {
        for (var pct = 0; pct <= 100; pct += 10)
        {
            await Set("110", pct);
            Assert.That(row.CapacityPct, Is.EqualTo(pct));
        }
    }

    [Test]
    public void Set_OtherFactory_Forbidden()
    {
        Assert.ThrowsAsync<ForbiddenException>(() => Set("125", 50));

        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Set_ProcessNotRunAtFactory_Rejected()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Set("110", 50, processId: "POLISH"));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "factoryId" }));
    }

    [Test]
    public void Set_NoSetupRow_Rejected()
    {
        Assert.ThrowsAsync<ValidationException>(() => Set("110", 50, processId: "OLD"));
    }

    [Test]
    public void Set_UnknownProcess_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Set("110", 50, processId: "NOPE"));
    }

    [Test]
    public async Task Set_Reduced_StoresNoteWhoWhenAndHistory()
    {
        var recovery = new DateOnly(2026, 11, 28);

        await Set("110", 80, " 1 of 5 lasers down ", recovery);

        Assert.That(row.CapacityPct, Is.EqualTo(80));
        Assert.That(row.CapacityReason, Is.EqualTo("1 of 5 lasers down"));
        Assert.That(row.ExpectedRecovery, Is.EqualTo(recovery));
        Assert.That(row.CapacityUpdatedBy, Is.EqualTo(Email));
        Assert.That(row.CapacityUpdatedAt, Is.EqualTo(Now));
        historyWriter.Verify(h => h.Record(
            HistoryEntityTypes.Process, "ENGRAVING", "capacityPct", "100%", "80% (reason: 1 of 5 lasers down)",
            HistorySource.User, "110", null), Times.Once);
        historyWriter.Verify(h => h.Record(
            HistoryEntityTypes.Process, "ENGRAVING", "capacityReason", null, "1 of 5 lasers down",
            HistorySource.User, "110", null), Times.Once);
        historyWriter.Verify(h => h.Record(
            HistoryEntityTypes.Process, "ENGRAVING", "expectedRecovery", null, recovery,
            HistorySource.User, "110", null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Set_BackToFull_ClearsReasonAndRecovery()
    {
        row.CapacityPct = 50;
        row.CapacityReason = "Short shift";
        row.ExpectedRecovery = new DateOnly(2026, 11, 28);

        await Set("110", 100, "ignored", new DateOnly(2026, 12, 1));

        Assert.That(row.CapacityPct, Is.EqualTo(100));
        Assert.That(row.CapacityReason, Is.Null);
        Assert.That(row.ExpectedRecovery, Is.Null);
    }

    [Test]
    public async Task Set_NothingChanged_NoSaveNoHistory()
    {
        await Set("110", 100);

        Assert.That(row.CapacityUpdatedAt, Is.Null);
        historyWriter.VerifyNoOtherCalls();
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Set_PastExpectedRecovery_DoesNotResetCapacity()
    {
        row.CapacityPct = 30;
        row.ExpectedRecovery = new DateOnly(2026, 11, 1);

        var rows = await new GetFactoryProcessCapacityQueryHandler(db.Object, CreateReader())
            .Handle(new GetFactoryProcessCapacityQuery("110"), CancellationToken.None);

        Assert.That(rows.Single().Cells.Single().CapacityPct, Is.EqualTo(30));
    }

    [Test]
    public async Task Overview_GridOfActiveProcessesAndActiveFactories()
    {
        var rows = await new GetProcessCapacityOverviewQueryHandler(db.Object, CreateReader())
            .Handle(new GetProcessCapacityOverviewQuery(), CancellationToken.None);

        Assert.That(rows.Select(r => r.ProcessId), Is.EqualTo(new[] { "ENGRAVING", "POLISH" }));
        var engraving = rows.First().Cells.ToList();
        Assert.That(engraving.Select(c => c.FactoryId), Is.EqualTo(new[] { "110", "125" }));
        Assert.That(engraving[0].Status, Is.EqualTo(CapacityStatus.Full));
        Assert.That(engraving[0].Today.RemainingPct, Is.EqualTo(100));
        Assert.That(engraving[1].Status, Is.EqualTo(CapacityStatus.Offline));
        Assert.That(engraving[1].Reason, Is.EqualTo("Laser down"));
        Assert.That(engraving[1].Today, Is.Null);
        var polish = rows.Last().Cells.ToList();
        Assert.That(polish.Select(c => c.RunsHere), Is.EqualTo(new[] { false, false }));
        Assert.That(polish[1].CapacityPct, Is.EqualTo(100));
    }

    [Test]
    public async Task FactoryView_OnlyProcessesTheFactoryRuns()
    {
        var rows = await new GetFactoryProcessCapacityQueryHandler(db.Object, CreateReader())
            .Handle(new GetFactoryProcessCapacityQuery("110"), CancellationToken.None);

        Assert.That(rows.Select(r => r.ProcessId), Is.EqualTo(new[] { "ENGRAVING" }));
    }

    [Test]
    public void FactoryView_UnknownFactory_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => new GetFactoryProcessCapacityQueryHandler(db.Object, CreateReader())
            .Handle(new GetFactoryProcessCapacityQuery("999"), CancellationToken.None));
    }

    [Test]
    public async Task UsedToday_CountsQuotesProducedTodayPerProcessAndFactory()
    {
        var ring = Guid.NewGuid();
        var chain = Guid.NewGuid();
        db.SetupSet(x => x.QuoteSets,
            new QuoteSet { Id = 1, SkuUuid = ring, Quantity = 3 },
            new QuoteSet { Id = 2, SkuUuid = chain, Quantity = 2 },
            new QuoteSet { Id = 3, SkuUuid = ring, Quantity = 7 });
        db.SetupSet(x => x.Quotes,
            Quote(1, "110", QuoteStatus.Produced, Now.AddHours(-2)),
            Quote(2, "110", QuoteStatus.Produced, Now.AddHours(-1)),
            Quote(2, "125", QuoteStatus.Quoted, null),
            Quote(3, "110", QuoteStatus.Produced, Now.AddDays(-1)),
            Quote(3, "125", QuoteStatus.Selected, null));
        db.SetupSet(x => x.SkuProcesses,
            new SkuProcess { SkuUuid = ring, ProcessId = "ENGRAVING", Quantity = 2 },
            new SkuProcess { SkuUuid = ring, ProcessId = "PACKING", Quantity = 1 },
            new SkuProcess { SkuUuid = chain, ProcessId = "PACKING", Quantity = 1 });

        var used = await new ProcessUsageReader(db.Object, TestDb.FixedTime(Now)).GetUsedToday(CancellationToken.None);

        Assert.That(used, Has.Count.EqualTo(2));
        Assert.That(used[new ProcessFactoryKey("ENGRAVING", "110")], Is.EqualTo(1));
        Assert.That(used[new ProcessFactoryKey("PACKING", "110")], Is.EqualTo(2));
    }

    [Test]
    public async Task FactoryView_ShowsUsedAndRemainingToday()
    {
        var sku = Guid.NewGuid();
        row.CapacityPct = 50;
        row.MaxPerDay = 20;
        db.SetupSet(x => x.QuoteSets, Enumerable.Range(1, 5).Select(id => new QuoteSet { Id = id, SkuUuid = sku }).ToArray());
        db.SetupSet(x => x.Quotes, Enumerable.Range(1, 5).Select(id => Quote(id, "110", QuoteStatus.Produced, Now)).ToArray());
        db.SetupSet(x => x.SkuProcesses, new SkuProcess { SkuUuid = sku, ProcessId = "ENGRAVING", Quantity = 2 });

        var rows = await new GetFactoryProcessCapacityQueryHandler(db.Object, CreateReader())
            .Handle(new GetFactoryProcessCapacityQuery("110"), CancellationToken.None);

        var today = rows.Single().Cells.Single().Today;
        Assert.That(today.Available, Is.EqualTo(10));
        Assert.That(today.Used, Is.EqualTo(5));
        Assert.That(today.RemainingPct, Is.EqualTo(25));
    }

    private ProcessCapacityReader CreateReader()
    {
        return new ProcessCapacityReader(db.Object, new ProcessUsageReader(db.Object, TestDb.FixedTime(Now)));
    }

    private static Quote Quote(long quoteSetId, string factoryId, QuoteStatus status, DateTime? producedAt)
    {
        return new Quote { QuoteSetId = quoteSetId, FactoryId = factoryId, Status = status, ProducedAt = producedAt };
    }
}
