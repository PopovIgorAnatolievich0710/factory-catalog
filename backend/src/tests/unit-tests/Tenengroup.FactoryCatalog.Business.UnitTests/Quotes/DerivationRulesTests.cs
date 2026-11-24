using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.FactoryCatalog.Domains.Components;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

[TestFixture]
public class DerivationRulesTests
{
    [TestCase(10, 0, false, null, null, StockStatuses.Ok)]
    [TestCase(10, 0, false, null, 10, StockStatuses.Ok)]
    [TestCase(6, 3, false, null, 10, StockStatuses.Low)]
    [TestCase(6, 3, false, 5, 10, StockStatuses.Ok)]
    [TestCase(6, 3, false, 20, 1, StockStatuses.Low)]
    [TestCase(0, 0, false, null, null, StockStatuses.OutOfStock)]
    [TestCase(-2, 1, false, null, null, StockStatuses.OutOfStock)]
    [TestCase(100, 100, true, null, null, StockStatuses.OutOfStock)]
    public void StockStatus_FollowsTheSharedRule(
        decimal warehouse,
        decimal productionFloor,
        bool manualOos,
        int? criticalOverride,
        int? globalCritical,
        string expected)
    {
        var stock = new ComponentFactory
        {
            WarehouseStock = warehouse,
            ProductionFloorStock = productionFloor,
            ManualOosActive = manualOos,
            CriticalStockLevelOverride = criticalOverride
        };

        Assert.That(StockStatusRule.Resolve(stock, globalCritical), Is.EqualTo(expected));
    }

    [Test]
    public void StockStatus_WithoutStockRow_IsOutOfStock()
    {
        Assert.That(StockStatusRule.Resolve(null, 5), Is.EqualTo(StockStatuses.OutOfStock));
    }

    [Test]
    public void Shares_AreScaledToOneHundredInProportion()
    {
        var shares = ShareRedistribution.Redistribute(new Dictionary<string, decimal> { ["110"] = 60, ["315"] = 20 });

        Assert.That(shares["110"], Is.EqualTo(75m));
        Assert.That(shares["315"], Is.EqualTo(25m));
    }

    [Test]
    public void Shares_AlreadyAtOneHundred_StayUnchanged()
    {
        var shares = ShareRedistribution.Redistribute(new Dictionary<string, decimal> { ["110"] = 60, ["125"] = 40 });

        Assert.That(shares["110"], Is.EqualTo(60m));
        Assert.That(shares["125"], Is.EqualTo(40m));
    }

    [Test]
    public void Shares_RoundingRemainderGoesToTheLargestFactory()
    {
        var shares = ShareRedistribution.Redistribute(
            new Dictionary<string, decimal> { ["110"] = 1, ["125"] = 1, ["315"] = 1 });

        Assert.That(shares.Values.Sum(), Is.EqualTo(100m));
        Assert.That(shares["110"], Is.EqualTo(33.34m));
        Assert.That(shares["125"], Is.EqualTo(33.33m));
        Assert.That(shares["315"], Is.EqualTo(33.33m));
    }

    [Test]
    public void Shares_AllZero_AreSplitEqually()
    {
        var shares = ShareRedistribution.Redistribute(new Dictionary<string, decimal> { ["110"] = 0, ["125"] = 0 });

        Assert.That(shares["110"], Is.EqualTo(50m));
        Assert.That(shares["125"], Is.EqualTo(50m));
    }

    [Test]
    public void Shares_SingleRemainingFactory_GetsEverything()
    {
        var shares = ShareRedistribution.Redistribute(new Dictionary<string, decimal> { ["125"] = 10 });

        Assert.That(shares["125"], Is.EqualTo(100m));
    }

    [Test]
    public void Shares_NoFactoryLeft_IsEmpty()
    {
        Assert.That(ShareRedistribution.Redistribute(new Dictionary<string, decimal>()), Is.Empty);
    }
}
