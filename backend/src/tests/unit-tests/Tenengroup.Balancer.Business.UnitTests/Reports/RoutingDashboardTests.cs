using Moq;
using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions.Reports;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Reports;
using Tenengroup.Balancer.Domains;
using static Tenengroup.Balancer.Business.UnitTests.TestData;

namespace Tenengroup.Balancer.Business.UnitTests.Reports;

[TestFixture]
public class RoutingDashboardTests
{
    [Test]
    public async Task Counts_live_and_fallback_allocations_but_not_shadow_decisions()
    {
        var live = OpenAllocation("O1", "I1", "110");
        var transferred = OpenAllocation("O2", "I1", "125");
        transferred.TransferCount = 1;
        var fallback = OpenAllocation("O3", "I1", "110");
        fallback.Mode = AllocationMode.Fallback;
        fallback.FallbackReason = "QuotesUnavailable";
        var shadow = OpenAllocation("O4", "I1", "125");
        shadow.Mode = AllocationMode.Shadow;

        var db = EmptyDb();
        db.SetupSet(x => x.Allocations, live, transferred, fallback, shadow);
        var settings = new Mock<IBalancerSettingsProvider>();
        settings.Setup(s => s.GetCurrent(It.IsAny<CancellationToken>())).ReturnsAsync(new BalancerSettings { ShareGapThresholdPct = 2m });

        var dashboard = await new GetRoutingDashboardQueryHandler(db.Object, settings.Object).Handle(
            new GetRoutingDashboardQuery(new DashboardFilter(null, null, Today, Today)), CancellationToken.None);

        Assert.That(dashboard.ItemsRouted, Is.EqualTo(3));
        Assert.That(dashboard.ItemsTransferred, Is.EqualTo(1));
        Assert.That(dashboard.TransferRatePct, Is.EqualTo(33.3m));
        Assert.That(dashboard.FallbackCount, Is.EqualTo(1));
    }
}
