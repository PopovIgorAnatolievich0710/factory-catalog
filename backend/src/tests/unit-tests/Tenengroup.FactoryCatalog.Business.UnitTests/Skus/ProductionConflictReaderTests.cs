using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class ProductionConflictReaderTests
{
    private static readonly Guid SkuUuid = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 24, 9, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        db.SetupSet(x => x.QuoteSets, new QuoteSet { Id = 1, SkuUuid = SkuUuid, OrderId = "ORD1", OrderItemId = "ITM1" });
        db.SetupSet(x => x.SkuProcesses, new SkuProcess { SkuUuid = SkuUuid, ProcessId = "ENGRAVING", Quantity = 1 });
        db.SetupSet(x => x.Processes, new Process { ProcessId = "ENGRAVING", Name = "Engraving", Type = ProcessType.Machine });
        db.SetupSet(x => x.ProcessFactories,
            new ProcessFactory { ProcessId = "ENGRAVING", FactoryId = "125", RunsHere = true, MachineType = "LASER" });
    }

    private static Quote Produced(long id, DateTime producedAt, string machine)
    {
        return new Quote
        {
            Id = id,
            QuoteSetId = 1,
            FactoryId = "125",
            Status = QuoteStatus.Produced,
            ProducedAt = producedAt,
            ProductionLogJson = $"[{{\"processId\":\"ENGRAVING\",\"machineType\":\"{machine}\"}}]"
        };
    }

    [Test]
    public async Task Get_LatestLogDiffers_ConflictForThatFactory()
    {
        db.SetupSet(x => x.Quotes, Produced(1, Now.AddDays(-1), "LASER"), Produced(2, Now, "CNC"));

        var issues = await new ProductionConflictReader(db.Object).Get([SkuUuid], CancellationToken.None);

        var issue = issues[SkuUuid].Single();
        Assert.That(issue.FactoryId, Is.EqualTo("125"));
        Assert.That(issue.Kind, Is.EqualTo(ProductionConflictKind.MachineMismatch));
        Assert.That(issue.Expected, Is.EqualTo("ENGRAVING on LASER"));
        Assert.That(issue.Actual, Is.EqualTo("ENGRAVING on CNC"));
        Assert.That(issue.OrderItemId, Is.EqualTo("ITM1"));
        Assert.That(issue.SeenAt, Is.EqualTo(Now));
    }

    [Test]
    public async Task Get_LaterLogMatches_ConflictCleared()
    {
        db.SetupSet(x => x.Quotes, Produced(1, Now.AddDays(-1), "CNC"), Produced(2, Now, "LASER"));

        var issues = await new ProductionConflictReader(db.Object).Get([SkuUuid], CancellationToken.None);

        Assert.That(issues, Is.Empty);
    }
}
