using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class ComponentFactoryStockStatusTests
{
    private static ComponentFactory Row(decimal warehouse, decimal floor, bool manualOos = false, decimal? levelOverride = null, string factoryId = "110")
    {
        return new ComponentFactory
        {
            ComponentErpId = "A",
            FactoryId = factoryId,
            WarehouseStock = warehouse,
            ProductionFloorStock = floor,
            ManualOosActive = manualOos,
            CriticalStockLevelOverride = levelOverride
        };
    }

    [TestCase(10, 2, 5, StockStatus.Ok)]
    [TestCase(3, 2, 5, StockStatus.Ok)]
    [TestCase(3, 1, 5, StockStatus.Low)]
    [TestCase(0, 0, 5, StockStatus.OutOfStock)]
    [TestCase(-1, 0, 5, StockStatus.OutOfStock)]
    public void StockStatus_ComparesTotalWithGlobalLevel(decimal warehouse, decimal floor, decimal global, StockStatus expected)
    {
        Assert.That(Row(warehouse, floor).GetStockStatus(global), Is.EqualTo(expected));
    }

    [Test]
    public void StockStatus_OverrideWinsOverGlobal()
    {
        var row = Row(6, 0, levelOverride: 10);

        Assert.That(row.GetEffectiveCriticalLevel(2), Is.EqualTo(10));
        Assert.That(row.GetStockStatus(2), Is.EqualTo(StockStatus.Low));
    }

    [Test]
    public void StockStatus_NoCriticalLevel_NeverLow()
    {
        Assert.That(Row(1, 0).GetStockStatus(null), Is.EqualTo(StockStatus.Ok));
        Assert.That(Row(1, 0, manualOos: true).IsOosConflict(null), Is.False);
    }

    [Test]
    public void ManualOos_IsOutOfStock_AndConflictsWhenStockReachesCriticalLevel()
    {
        var conflict = Row(5, 0, manualOos: true);
        var noConflict = Row(4, 0, manualOos: true);

        Assert.That(conflict.GetStockStatus(5), Is.EqualTo(StockStatus.OutOfStock));
        Assert.That(conflict.IsOosConflict(5), Is.True);
        Assert.That(conflict.GetStockAlertStatus(5), Is.EqualTo(StockAlertStatus.OosConflict));
        Assert.That(noConflict.IsOosConflict(5), Is.False);
        Assert.That(noConflict.GetStockAlertStatus(5), Is.EqualTo(StockAlertStatus.OutOfStock));
    }

    [Test]
    public void Summary_TakesWorstStatusAndItsFactories()
    {
        var summary = ComponentStockSummary.From(5,
        [
            Row(10, 0, factoryId: "110"),
            Row(1, 0, factoryId: "125"),
            Row(0, 0, factoryId: "315"),
            Row(0, 0, factoryId: "200")
        ]);

        Assert.That(summary.Status, Is.EqualTo(StockAlertStatus.OutOfStock));
        Assert.That(summary.FactoryIds, Is.EqualTo(new[] { "200", "315" }));
        Assert.That(summary.TotalStock, Is.EqualTo(11));
        Assert.That(summary.IsOutOfStockOrConflict, Is.True);
    }

    [Test]
    public void Summary_ConflictIsWorseThanOutOfStock_AndNoRowsIsOk()
    {
        var summary = ComponentStockSummary.From(5, [Row(0, 0, factoryId: "110"), Row(9, 0, manualOos: true, factoryId: "125")]);

        Assert.That(summary.Status, Is.EqualTo(StockAlertStatus.OosConflict));
        Assert.That(summary.FactoryIds, Is.EqualTo(new[] { "125" }));

        var empty = ComponentStockSummary.From(5, []);
        Assert.That(empty.Status, Is.EqualTo(StockAlertStatus.Ok));
        Assert.That(empty.FactoryIds, Is.Empty);
    }
}
