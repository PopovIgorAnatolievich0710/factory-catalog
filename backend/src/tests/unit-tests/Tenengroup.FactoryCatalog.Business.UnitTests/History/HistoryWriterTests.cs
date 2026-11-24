using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Auth;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.History;

[TestFixture]
public class HistoryWriterTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IUserIdentity> identity;
    private List<HistoryEntry> entries;
    private HistoryWriter writer;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        entries = db.SetupSet(x => x.HistoryEntries);
        identity = new Mock<IUserIdentity>();
        identity.Setup(i => i.Email).Returns("sc@tenengroup.com");
        identity.Setup(i => i.Name).Returns("Supply Chain");
        writer = new HistoryWriter(db.Object, identity.Object, TestDb.FixedTime(Now));
    }

    [Test]
    public void Record_ChangedValue_AddsEntryWithoutSaving()
    {
        writer.Record(HistoryEntityTypes.Component, "R.WG.10K", "criticalStockLevel", 2m, 5.50m, factoryId: "125");

        Assert.That(entries, Has.Count.EqualTo(1));
        var entry = entries[0];
        Assert.That(entry.EntityType, Is.EqualTo("component"));
        Assert.That(entry.EntityId, Is.EqualTo("R.WG.10K"));
        Assert.That(entry.FactoryId, Is.EqualTo("125"));
        Assert.That(entry.Field, Is.EqualTo("criticalStockLevel"));
        Assert.That(entry.OldValue, Is.EqualTo("2"));
        Assert.That(entry.NewValue, Is.EqualTo("5.5"));
        Assert.That(entry.ChangedBy, Is.EqualTo("sc@tenengroup.com"));
        Assert.That(entry.ChangedAt, Is.EqualTo(Now));
        Assert.That(entry.Source, Is.EqualTo(HistorySource.User));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Record_SameValue_AddsNothing()
    {
        writer.Record(HistoryEntityTypes.Process, "ENGRAVING", "capacityPct", 80, 80);
        writer.Record(HistoryEntityTypes.Process, "ENGRAVING", "capacityReason", null, null);

        Assert.That(entries, Is.Empty);
    }

    [Test]
    public void Record_NullToValue_IsAChange()
    {
        writer.Record(HistoryEntityTypes.Process, "ENGRAVING", "expectedRecovery", null, new DateOnly(2026, 11, 28));

        Assert.That(entries[0].OldValue, Is.Null);
        Assert.That(entries[0].NewValue, Is.EqualTo("2026-11-28"));
    }

    [Test]
    public void Record_SyncSourceWithExplicitActor()
    {
        writer.Record(HistoryEntityTypes.Component, "R.WG.10K", "status", "Active", "Inactive", HistorySource.Sync, changedBy: "SAP sync");

        Assert.That(entries[0].Source, Is.EqualTo(HistorySource.Sync));
        Assert.That(entries[0].ChangedBy, Is.EqualTo("SAP sync"));
    }

    [Test]
    public void Record_NoEmail_FallsBackToName()
    {
        identity.Setup(i => i.Email).Returns(string.Empty);

        writer.Record(HistoryEntityTypes.Sku, "8f3c1a2e", "producible", false, true);

        Assert.That(entries[0].ChangedBy, Is.EqualTo("Supply Chain"));
        Assert.That(entries[0].OldValue, Is.EqualTo("false"));
        Assert.That(entries[0].NewValue, Is.EqualTo("true"));
    }

    [Test]
    public async Task GetHistory_NewestFirstForTheEntityOnly()
    {
        db.SetupSet(x => x.HistoryEntries,
            new HistoryEntry { Id = 1, EntityType = "component", EntityId = "A", Field = "old", ChangedAt = Now.AddDays(-2) },
            new HistoryEntry { Id = 2, EntityType = "component", EntityId = "A", Field = "new", ChangedAt = Now },
            new HistoryEntry { Id = 3, EntityType = "component", EntityId = "B", Field = "other", ChangedAt = Now },
            new HistoryEntry { Id = 4, EntityType = "process", EntityId = "A", Field = "other", ChangedAt = Now });

        var result = await new GetHistoryQueryHandler(db.Object)
            .Handle(new GetHistoryQuery("component", "A", 1, 1), CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(2));
        Assert.That(result.Items.Select(i => i.Field), Is.EqualTo(new[] { "new" }));
    }
}
