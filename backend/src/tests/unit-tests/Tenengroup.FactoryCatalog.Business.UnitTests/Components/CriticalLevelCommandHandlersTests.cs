using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class CriticalLevelCommandHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;
    private Component component;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        component = new Component { ErpId = "A", Name = "Alpha", CriticalStockLevel = 2 };
        db.SetupSet(x => x.Components, component);
        db.SetupSet(x => x.Factories, new Factory { Id = "110" });
    }

    [Test]
    public async Task Global_SetsLevelAndWritesHistory()
    {
        await new SetCriticalLevelCommandHandler(db.Object, history.Object, TestDb.FixedTime(Now))
            .Handle(new SetCriticalLevelCommand("A", 5), CancellationToken.None);

        Assert.That(component.CriticalStockLevel, Is.EqualTo(5));
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "criticalStockLevel", 2m, 5m, HistorySource.User, null, null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Global_NegativeLevel_Rejected()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() =>
            new SetCriticalLevelCommandHandler(db.Object, history.Object, TestDb.FixedTime(Now))
                .Handle(new SetCriticalLevelCommand("A", -1), CancellationToken.None));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "level" }));
        Assert.That(component.CriticalStockLevel, Is.EqualTo(2));
    }

    [Test]
    public async Task Override_SetThenClear()
    {
        var rows = db.SetupSet(x => x.ComponentFactories);
        var handler = new SetCriticalLevelOverrideCommandHandler(db.Object, history.Object);

        await handler.Handle(new SetCriticalLevelOverrideCommand("A", "110", 7), CancellationToken.None);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].CriticalStockLevelOverride, Is.EqualTo(7));
        Assert.That(rows[0].GetEffectiveCriticalLevel(component.CriticalStockLevel), Is.EqualTo(7));

        await handler.Handle(new SetCriticalLevelOverrideCommand("A", "110", null), CancellationToken.None);

        Assert.That(rows[0].CriticalStockLevelOverride, Is.Null);
        Assert.That(rows[0].GetEffectiveCriticalLevel(component.CriticalStockLevel), Is.EqualTo(2));
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "criticalStockLevelOverride", null, 7m, HistorySource.User, "110", null), Times.Once);
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "criticalStockLevelOverride", 7m, null, HistorySource.User, "110", null), Times.Once);
    }

    [Test]
    public void Override_UnknownFactory_NotFound()
    {
        db.SetupSet(x => x.ComponentFactories);

        Assert.ThrowsAsync<EntityNotFoundException>(() =>
            new SetCriticalLevelOverrideCommandHandler(db.Object, history.Object)
                .Handle(new SetCriticalLevelOverrideCommand("A", "999", 1), CancellationToken.None));
    }
}
