using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Quotes;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

[TestFixture]
public class DerivationRulesTests
{
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
