using NUnit.Framework;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class SkuComponentStockTests
{
    [TestCase(6, 4, 5, null, false, SkuComponentStockStatus.Ok, false)]
    [TestCase(2, 2, 5, null, false, SkuComponentStockStatus.Low, false)]
    [TestCase(2, 2, 5, 3, false, SkuComponentStockStatus.Ok, false)]
    [TestCase(0, 0, 5, null, false, SkuComponentStockStatus.OutOfStock, false)]
    [TestCase(6, 4, 5, null, true, SkuComponentStockStatus.OutOfStock, true)]
    [TestCase(2, 2, 5, null, true, SkuComponentStockStatus.OutOfStock, false)]
    [TestCase(1, 0, null, null, false, SkuComponentStockStatus.Ok, false)]
    public void Evaluate(
        decimal warehouse, decimal floor, decimal? global, decimal? factoryOverride, bool manualOos,
        SkuComponentStockStatus expectedStatus, bool expectedConflict)
    {
        var stock = SkuComponentStock.Evaluate(warehouse, floor, global, factoryOverride, manualOos);

        Assert.That(stock.Total, Is.EqualTo(warehouse + floor));
        Assert.That(stock.Status, Is.EqualTo(expectedStatus));
        Assert.That(stock.OosConflict, Is.EqualTo(expectedConflict));
    }
}
