using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class SetManualOosCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;
    private Mock<IHistoryWriter> history;
    private List<ComponentFactory> rows;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        history = new Mock<IHistoryWriter>();
        currentUser.Setup(u => u.Name).Returns("Zsofia Nagy");
        db.SetupSet(x => x.Components, new Component { ErpId = "A", Name = "Alpha" });
        db.SetupSet(x => x.Factories, new Factory { Id = "110" }, new Factory { Id = "125" });
    }

    private Task Handle(SetManualOosCommand command)
    {
        return new SetManualOosCommandHandler(db.Object, currentUser.Object, history.Object, TestDb.FixedTime(Now))
            .Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task Tick_FlagsRowWithUserDateAndNote()
    {
        rows = db.SetupSet(x => x.ComponentFactories, new ComponentFactory { ComponentErpId = "A", FactoryId = "110", WarehouseStock = 5 });

        await Handle(new SetManualOosCommand("A", "110", true, "  Supply scheduled for Feb 1 "));

        Assert.That(rows[0].ManualOosActive, Is.True);
        Assert.That(rows[0].ManualOosSetBy, Is.EqualTo("Zsofia Nagy"));
        Assert.That(rows[0].ManualOosSetAt, Is.EqualTo(Now));
        Assert.That(rows[0].ManualOosNote, Is.EqualTo("Supply scheduled for Feb 1"));
        Assert.That(rows[0].WarehouseStock, Is.EqualTo(5));
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "manualOos", false, true, HistorySource.User, "110", null), Times.Once);
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "manualOosNote", null, "Supply scheduled for Feb 1", HistorySource.User, "110", null), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EditNoteOfActiveFlag_KeepsWhoAndWhen()
    {
        var setAt = Now.AddDays(-3);
        rows = db.SetupSet(x => x.ComponentFactories, new ComponentFactory
        {
            ComponentErpId = "A", FactoryId = "110", ManualOosActive = true, ManualOosSetBy = "First user", ManualOosSetAt = setAt, ManualOosNote = "old"
        });

        await Handle(new SetManualOosCommand("A", "110", true, "new"));

        Assert.That(rows[0].ManualOosNote, Is.EqualTo("new"));
        Assert.That(rows[0].ManualOosSetBy, Is.EqualTo("First user"));
        Assert.That(rows[0].ManualOosSetAt, Is.EqualTo(setAt));
    }

    [Test]
    public async Task Untick_ClearsFlagNoteAndAuthor()
    {
        rows = db.SetupSet(x => x.ComponentFactories, new ComponentFactory
        {
            ComponentErpId = "A", FactoryId = "110", ManualOosActive = true, ManualOosSetBy = "x", ManualOosSetAt = Now, ManualOosNote = "old"
        });

        await Handle(new SetManualOosCommand("A", "110", false, "ignored"));

        Assert.That(rows[0].ManualOosActive, Is.False);
        Assert.That(rows[0].ManualOosSetBy, Is.Null);
        Assert.That(rows[0].ManualOosSetAt, Is.Null);
        Assert.That(rows[0].ManualOosNote, Is.Null);
        history.Verify(h => h.Record(HistoryEntityTypes.Component, "A", "manualOos", true, false, HistorySource.User, "110", null), Times.Once);
    }

    [Test]
    public async Task Tick_FactoryWithoutStockRow_CreatesTheRow()
    {
        rows = db.SetupSet(x => x.ComponentFactories);

        await Handle(new SetManualOosCommand("A", "125", true, null));

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].FactoryId, Is.EqualTo("125"));
        Assert.That(rows[0].ManualOosActive, Is.True);
    }

    [Test]
    public void NotOwnFactory_ForbiddenAndNothingChanges()
    {
        rows = db.SetupSet(x => x.ComponentFactories, new ComponentFactory { ComponentErpId = "A", FactoryId = "110" });
        currentUser.Setup(u => u.EnsureCanEditFactory("110", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("not your factory"));

        Assert.ThrowsAsync<ForbiddenException>(() => Handle(new SetManualOosCommand("A", "110", true, null)));

        Assert.That(rows[0].ManualOosActive, Is.False);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UnknownComponentOrFactory_NotFound()
    {
        db.SetupSet(x => x.ComponentFactories);

        Assert.ThrowsAsync<EntityNotFoundException>(() => Handle(new SetManualOosCommand("MISSING", "110", true, null)));
        Assert.ThrowsAsync<EntityNotFoundException>(() => Handle(new SetManualOosCommand("A", "999", true, null)));
    }
}
