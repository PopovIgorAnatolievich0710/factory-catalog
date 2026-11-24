using NUnit.Framework;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class SkuFactorySharesTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Dictionary<string, decimal> Defaults = new() { ["110"] = 50, ["125"] = 30, ["315"] = 20 };

    private static List<SkuFactory> Rows(params (string FactoryId, bool Producible)[] rows)
    {
        return rows.Select(r => new SkuFactory { FactoryId = r.FactoryId, Producible = r.Producible }).ToList();
    }

    [Test]
    public void SetOverride_RebalancesOtherProducibleFactoriesInProportion()
    {
        var rows = Rows(("110", true), ("125", true), ("315", true));

        var error = SkuFactoryShares.SetOverride(rows, "110", 70, Defaults, "fm@tenengroup.com", Now);

        Assert.That(error, Is.Null);
        Assert.That(rows[0].OverrideSharePct, Is.EqualTo(70m));
        Assert.That(rows[0].OverrideBy, Is.EqualTo("fm@tenengroup.com"));
        Assert.That(rows[0].OverrideAt, Is.EqualTo(Now));
        Assert.That(rows[1].OverrideSharePct, Is.EqualTo(18m));
        Assert.That(rows[1].OverrideBy, Is.Null);
        Assert.That(rows[2].OverrideSharePct, Is.EqualTo(12m));
        Assert.That(SkuFactoryShares.Effective(rows, Defaults).Values.Sum(), Is.EqualTo(100m));
    }

    [Test]
    public void SetOverride_NonProducibleFactoriesAreNotTouched()
    {
        var rows = Rows(("110", true), ("125", true), ("315", false));

        SkuFactoryShares.SetOverride(rows, "110", 40, Defaults, "fm", Now);

        Assert.That(rows[1].OverrideSharePct, Is.EqualTo(60m));
        Assert.That(rows[2].OverrideSharePct, Is.Null);
    }

    [Test]
    public void SetOverride_SecondManagerRescalesTheFirst()
    {
        var rows = Rows(("110", true), ("125", true));
        SkuFactoryShares.SetOverride(rows, "110", 70, Defaults, "fm110", Now);

        SkuFactoryShares.SetOverride(rows, "125", 50, Defaults, "fm125", Now);

        Assert.That(rows[0].OverrideSharePct, Is.EqualTo(50m));
        Assert.That(rows[0].OverrideBy, Is.EqualTo("fm110"));
        Assert.That(rows[1].OverrideSharePct, Is.EqualTo(50m));
    }

    [Test]
    public void SetOverride_OnlyProducibleFactory_MustBe100()
    {
        var rows = Rows(("110", true), ("125", false));

        Assert.That(SkuFactoryShares.SetOverride(rows, "110", 80, Defaults, "fm", Now), Is.Not.Null);
        Assert.That(rows[0].OverrideSharePct, Is.Null);
        Assert.That(SkuFactoryShares.SetOverride(rows, "110", 100, Defaults, "fm", Now), Is.Null);
    }

    [TestCase(-1)]
    [TestCase(100.5)]
    [TestCase(10.123)]
    public void SetOverride_InvalidValue_Rejected(decimal pct)
    {
        var rows = Rows(("110", true), ("125", true));

        Assert.That(SkuFactoryShares.SetOverride(rows, "110", pct, Defaults, "fm", Now), Is.Not.Null);
    }

    [Test]
    public void SetOverride_NotProducible_Rejected()
    {
        var rows = Rows(("110", false), ("125", true));

        Assert.That(SkuFactoryShares.SetOverride(rows, "110", 50, Defaults, "fm", Now), Does.Contain("not producible"));
        Assert.That(SkuFactoryShares.SetOverride(rows, "315", 50, Defaults, "fm", Now), Does.Contain("not producible"));
    }

    [Test]
    public void ResetOverride_LastManagerOverride_ReturnsRowToDefaults()
    {
        var rows = Rows(("110", true), ("125", true), ("315", true));
        SkuFactoryShares.SetOverride(rows, "110", 70, Defaults, "fm", Now);

        SkuFactoryShares.ResetOverride(rows, "110", Defaults);

        Assert.That(rows.Select(r => r.OverrideSharePct), Is.All.Null);
        Assert.That(rows[0].OverrideBy, Is.Null);
        Assert.That(SkuFactoryShares.Effective(rows, Defaults)["110"], Is.EqualTo(50m));
    }

    [Test]
    public void ResetOverride_OtherManagerOverrideRemains_OthersShareTheRest()
    {
        var rows = Rows(("110", true), ("125", true), ("315", true));
        SkuFactoryShares.SetOverride(rows, "110", 70, Defaults, "fm110", Now);
        SkuFactoryShares.SetOverride(rows, "125", 20, Defaults, "fm125", Now);

        SkuFactoryShares.ResetOverride(rows, "110", Defaults);

        Assert.That(rows[0].OverrideSharePct, Is.Null);
        Assert.That(rows[1].OverrideBy, Is.EqualTo("fm125"));
        Assert.That(rows[1].OverrideSharePct + rows[2].OverrideSharePct, Is.EqualTo(50m));
        var effective = SkuFactoryShares.Effective(rows, Defaults);
        Assert.That(effective["110"], Is.EqualTo(50m));
        Assert.That(effective.Values.Sum(), Is.EqualTo(100m));
    }

    [Test]
    public void SetProducible_False_ClearsOverrideAndOrphanedRebalance()
    {
        var rows = Rows(("110", true), ("125", true));
        SkuFactoryShares.SetOverride(rows, "110", 70, Defaults, "fm", Now);

        SkuFactoryShares.SetProducible(rows, "110", false);

        Assert.That(rows[0].Producible, Is.False);
        Assert.That(rows.Select(r => r.OverrideSharePct), Is.All.Null);
        var effective = SkuFactoryShares.Effective(rows, Defaults);
        Assert.That(effective["110"], Is.EqualTo(0m));
        Assert.That(effective["125"], Is.EqualTo(100m));
    }
}
