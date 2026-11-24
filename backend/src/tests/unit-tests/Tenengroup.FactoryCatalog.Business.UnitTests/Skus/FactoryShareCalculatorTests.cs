using NUnit.Framework;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class FactoryShareCalculatorTests
{
    [Test]
    public void Effective_AllProducibleNoOverride_UsesDefaults()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 50, null, true),
            new FactoryShareInput("125", 30, null, true),
            new FactoryShareInput("315", 20, null, true)
        ]);

        Assert.That(shares["110"], Is.EqualTo(50m));
        Assert.That(shares["125"], Is.EqualTo(30m));
        Assert.That(shares["315"], Is.EqualTo(20m));
    }

    [Test]
    public void Effective_FactoryDropsOut_ShareSplitInProportion()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 50, null, true),
            new FactoryShareInput("125", 30, null, true),
            new FactoryShareInput("315", 20, null, false)
        ]);

        Assert.That(shares["110"], Is.EqualTo(62.5m));
        Assert.That(shares["125"], Is.EqualTo(37.5m));
        Assert.That(shares["315"], Is.EqualTo(0m));
    }

    [Test]
    public void Effective_OverrideWinsOverDefault()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 60, 70, true),
            new FactoryShareInput("125", 40, 30, true)
        ]);

        Assert.That(shares["110"], Is.EqualTo(70m));
        Assert.That(shares["125"], Is.EqualTo(30m));
    }

    [Test]
    public void Effective_RoundsToTwoDecimalsAndTotalsExactly100()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 1, null, true),
            new FactoryShareInput("125", 1, null, true),
            new FactoryShareInput("315", 1, null, true)
        ]);

        Assert.That(shares.Values.Sum(), Is.EqualTo(100m));
        Assert.That(shares["110"], Is.EqualTo(33.34m));
        Assert.That(shares["125"], Is.EqualTo(33.33m));
        Assert.That(shares["315"], Is.EqualTo(33.33m));
    }

    [Test]
    public void Effective_AllRemainingSharesZero_SplitsEqually()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 0, null, true),
            new FactoryShareInput("125", 0, null, true),
            new FactoryShareInput("315", 100, null, false)
        ]);

        Assert.That(shares["110"], Is.EqualTo(50m));
        Assert.That(shares["125"], Is.EqualTo(50m));
    }

    [Test]
    public void Effective_NobodyCanProduce_AllZero()
    {
        var shares = FactoryShareCalculator.Effective(
        [
            new FactoryShareInput("110", 60, null, false),
            new FactoryShareInput("125", 40, 100, false)
        ]);

        Assert.That(shares.Values, Is.All.EqualTo(0m));
    }
}
