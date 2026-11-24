using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Components.Replacements;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class ComponentReplacementHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;
    private List<ComponentReplacement> replacements;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        db.SetupSet(x => x.Factories, new Factory { Id = "110" }, new Factory { Id = "125" });
        db.SetupSet(x => x.Components,
            new Component { ErpId = "A", Name = "Alpha" },
            new Component { ErpId = "P1", Name = "Planner one", CriticalStockLevel = 5, UnitOfMeasureUnit = "kg" },
            new Component { ErpId = "P2", Name = "Planner two" },
            new Component { ErpId = "P3", Name = "Inactive planner", Status = ComponentStatus.Inactive },
            new Component { ErpId = "B1", Name = "Balancer only" });
        db.SetupSet(x => x.ComponentAlternatives,
            new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "P1", ForPlanner = true },
            new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "P2", ForPlanner = true, ForBalancer = true },
            new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "P3", ForPlanner = true },
            new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "B1", ForBalancer = true });
        db.SetupSet(x => x.ComponentFactories,
            new ComponentFactory { ComponentErpId = "P1", FactoryId = "110", WarehouseStock = 3, ProductionFloorStock = 1 },
            new ComponentFactory { ComponentErpId = "P1", FactoryId = "125", WarehouseStock = 50 });
        replacements = db.SetupSet(x => x.ComponentReplacements);
    }

    private Task<ComponentReplacementDto> Record(string replacementErpId, string factoryId = "110", decimal? quantity = 1)
    {
        return new RecordComponentReplacementCommandHandler(db.Object, history.Object, TestDb.FixedTime(Now)).Handle(
            new RecordComponentReplacementCommand("ORD-1", "ITEM-1", factoryId, "A", replacementErpId, quantity, "floor.user", null),
            CancellationToken.None);
    }

    [Test]
    public async Task Alternatives_OnlyActivePlannerOnes_WithStockInTheFactory()
    {
        var result = await new GetReplacementAlternativesQueryHandler(db.Object)
            .Handle(new GetReplacementAlternativesQuery("A", "110"), CancellationToken.None);

        Assert.That(result, Is.EqualTo(new[]
        {
            new ReplacementAlternativeDto("P1", "Planner one", "kg", 3, 1, 4, StockStatus.Low),
            new ReplacementAlternativeDto("P2", "Planner two", null, 0, 0, 0, StockStatus.OutOfStock)
        }));
    }

    [Test]
    public void Alternatives_UnknownComponentOrFactory_NotFound()
    {
        var handler = new GetReplacementAlternativesQueryHandler(db.Object);

        Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(new GetReplacementAlternativesQuery("MISSING", "110"), CancellationToken.None));
        Assert.ThrowsAsync<EntityNotFoundException>(() =>
            handler.Handle(new GetReplacementAlternativesQuery("A", "999"), CancellationToken.None));
    }

    [Test]
    public async Task Record_ApprovedPlannerAlternative_IsStored()
    {
        var result = await Record("P1");

        Assert.That(replacements, Has.Count.EqualTo(1));
        Assert.That(replacements[0].OriginalErpId, Is.EqualTo("A"));
        Assert.That(replacements[0].ReplacementErpId, Is.EqualTo("P1"));
        Assert.That(replacements[0].FactoryId, Is.EqualTo("110"));
        Assert.That(replacements[0].ReplacedBy, Is.EqualTo("floor.user"));
        Assert.That(replacements[0].ReplacedAt, Is.EqualTo(Now));
        Assert.That(result.ReplacementErpId, Is.EqualTo("P1"));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestCase("B1")]
    [TestCase("MISSING")]
    public void Record_NotAPlannerAlternative_Rejected(string replacementErpId)
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Record(replacementErpId));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "replacementErpId" }));
        Assert.That(replacements, Is.Empty);
    }

    [Test]
    public void Record_UnknownFactoryOrBadQuantity_Rejected()
    {
        Assert.ThrowsAsync<ValidationException>(() => Record("P1", factoryId: "999"));
        Assert.ThrowsAsync<ValidationException>(() => Record("P1", quantity: 0));
        Assert.That(replacements, Is.Empty);
    }
}
