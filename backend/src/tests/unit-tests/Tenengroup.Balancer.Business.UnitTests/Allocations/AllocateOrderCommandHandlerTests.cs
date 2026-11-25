using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.Business.Decisions;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using static Tenengroup.Balancer.Business.UnitTests.TestData;

namespace Tenengroup.Balancer.Business.UnitTests.Allocations;

[TestFixture]
public class AllocateOrderCommandHandlerTests
{
    private Mock<IBalancerDbContext> db;
    private Mock<ICatalogClient> catalog;
    private Mock<IDecisionContextFactory> contextFactory;
    private Mock<IBalancerSettingsProvider> settingsProvider;
    private Mock<IAllocationOutputQueue> outputQueue;
    private List<RouteOption> routes;
    private Dictionary<string, decimal> fallbackShares;
    private BalancerSettings settings;

    [SetUp]
    public void SetUp()
    {
        db = EmptyDb();
        catalog = new Mock<ICatalogClient>();
        contextFactory = new Mock<IDecisionContextFactory>();
        settingsProvider = new Mock<IBalancerSettingsProvider>();
        outputQueue = new Mock<IAllocationOutputQueue>();
        routes = [Route("110", 5m), Route("125", 20m)];
        fallbackShares = new Dictionary<string, decimal> { ["110"] = 40m, ["125"] = 60m };
        settings = new BalancerSettings { Version = 3, Mode = BalancerMode.Live, DefaultProductionDays = 3 };

        settingsProvider.Setup(s => s.GetCurrent(It.IsAny<CancellationToken>())).ReturnsAsync(() => settings);
        contextFactory
            .Setup(f => f.Create(It.IsAny<DecisionContextRequest>(), It.IsAny<BalancerSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DecisionContextRequest request, BalancerSettings _, CancellationToken _) => Input(
                request.Items,
                routes,
                request.PromisedDeliveryDate,
                request.SlaDeliveryDate,
                request.IsEventOrder,
                fallbackShare: fallbackShares,
                anchors: request.AnchorFactoryIds));
        catalog
            .Setup(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuoteRequest request, CancellationToken _) => new QuoteSet(
                $"PQ-{request.OrderId}-{request.OrderItemId}",
                Now.AddMinutes(15),
                [
                    Quote("110", 45m, quoteKey: $"PQ-{request.OrderId}-{request.OrderItemId}"),
                    Quote("125", 40m, quoteKey: $"PQ-{request.OrderId}-{request.OrderItemId}")
                ]));
    }

    private AllocateOrderCommandHandler Handler()
    {
        return new AllocateOrderCommandHandler(
            db.Object,
            catalog.Object,
            contextFactory.Object,
            new RoutingDecisionEngine(),
            settingsProvider.Object,
            outputQueue.Object,
            FixedTime(),
            NullLogger<AllocateOrderCommandHandler>.Instance);
    }

    private static AllocateOrderCommand Command(params AllocationItemRequest[] items)
    {
        return new AllocateOrderCommand("O1", "de", Today.AddDays(10), null, false, "Regular", "MYKA", Now.AddHours(-1), items);
    }

    private static AllocationItemRequest FactoryItem(string id = "I1")
    {
        return new AllocationItemRequest(id, "SKU-1", 1, [new CbdComponent("CMP-1", 1m)], FulfillmentType.Factory);
    }

    [Test]
    public async Task Live_mode_stores_the_decision_with_all_candidates_and_queues_the_output()
    {
        var result = await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(result.Single().Id, Is.EqualTo(allocation.Id));
        Assert.That(allocation.Mode, Is.EqualTo(AllocationMode.Live));
        Assert.That(allocation.Status, Is.EqualTo(AllocationStatus.Allocated));
        Assert.That(allocation.FactoryId, Is.EqualTo("110"));
        Assert.That(allocation.PackingFacilityId, Is.EqualTo("F110"));
        Assert.That(allocation.SelectedQuoteId, Is.EqualTo("PQ-O1-I1-110"));
        Assert.That(allocation.QuoteKey, Is.EqualTo("PQ-O1-I1"));
        Assert.That(allocation.ProductionCost, Is.EqualTo(45m));
        Assert.That(allocation.ShippingCost, Is.EqualTo(5m));
        Assert.That(allocation.TotalCost, Is.EqualTo(50m));
        Assert.That(allocation.DestinationCountry, Is.EqualTo("DE"));
        Assert.That(allocation.SettingsVersion, Is.EqualTo(3));
        Assert.That(allocation.DecidedAt, Is.EqualTo(Now));
        Assert.That(allocation.MeetsPromise, Is.True);
        Assert.That(allocation.DueDate, Is.EqualTo(Today.AddDays(5)));
        Assert.That(allocation.ReadCandidates().Select(c => c.FactoryId), Is.EquivalentTo(new[] { "110", "125" }));
        Assert.That(allocation.CostLines.Select(l => (l.Kind, l.Code)), Is.EquivalentTo(new[]
        {
            (CostLineKind.Component, "CMP-1"), (CostLineKind.Process, "ENGRAVING"), (CostLineKind.Packing, "PACKING")
        }));
        Assert.That(result.Single().DecisionReason, Is.EqualTo("Lowest total cost that meets the delivery promise"));

        catalog.Verify(c => c.RequestQuotes(It.Is<QuoteRequest>(r => r.Mode == QuoteModes.Live && r.Cbd.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        outputQueue.Verify(q => q.Enqueue(allocation.Id), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Shadow_mode_decides_and_logs_but_does_not_act()
    {
        settings.Mode = BalancerMode.Shadow;

        await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.Mode, Is.EqualTo(AllocationMode.Shadow));
        Assert.That(allocation.FactoryId, Is.EqualTo("110"));
        catalog.Verify(c => c.RequestQuotes(It.Is<QuoteRequest>(r => r.Mode == QuoteModes.Shadow), It.IsAny<CancellationToken>()), Times.Once);
        catalog.Verify(c => c.SelectQuote(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        outputQueue.Verify(q => q.Enqueue(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Routes_by_default_share_and_flags_the_fallback_when_quotes_do_not_come_back()
    {
        catalog
            .Setup(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("catalog is down"));

        await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.Mode, Is.EqualTo(AllocationMode.Fallback));
        Assert.That(allocation.FallbackReason, Is.EqualTo(FallbackReasons.QuotesUnavailable));
        Assert.That(allocation.FactoryId, Is.EqualTo("125"));
        Assert.That(allocation.SelectedQuoteId, Is.Null);
        Assert.That(allocation.DueDate, Is.EqualTo(Today.AddDays(3)));
        outputQueue.Verify(q => q.Enqueue(allocation.Id), Times.Once);
    }

    [Test]
    public async Task Routes_by_quoted_share_when_routes_are_unavailable()
    {
        routes.Clear();

        await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.Mode, Is.EqualTo(AllocationMode.Fallback));
        Assert.That(allocation.FallbackReason, Is.EqualTo(FallbackReasons.RoutesUnavailable));
        Assert.That(allocation.SelectedQuoteId, Is.Not.Null);
        Assert.That(allocation.ProductionCost, Is.Not.Null);
        Assert.That(allocation.ShippingCost, Is.Null);
    }

    [Test]
    public async Task Virtual_items_are_never_quoted_or_routed()
    {
        var item = FactoryItem() with { FulfillmentType = FulfillmentType.Virtual };

        await Handler().Handle(Command(item), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.Status, Is.EqualTo(AllocationStatus.NotRouted));
        Assert.That(allocation.FactoryId, Is.Null);
        catalog.Verify(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        outputQueue.Verify(q => q.Enqueue(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task Dropshipper_items_get_the_catalog_cost_attached_and_are_not_routed()
    {
        catalog
            .Setup(c => c.GetDropshipperCost("SKU-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DropshipperCost("uuid", "DS-1", "DS-77", "Print Partner", new CatalogMoney(18.5m, "USD")));
        var item = FactoryItem() with { FulfillmentType = FulfillmentType.Dropshipper };

        var result = await Handler().Handle(Command(item), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.Status, Is.EqualTo(AllocationStatus.NotRouted));
        Assert.That(allocation.FulfillmentType, Is.EqualTo(FulfillmentType.Dropshipper));
        Assert.That(allocation.FactoryId, Is.Null);
        Assert.That(result.Single().TotalCost, Is.EqualTo(18.5m));
        Assert.That(result.Single().Currency, Is.EqualTo("USD"));
        catalog.Verify(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task An_item_the_catalog_reports_as_dropshipper_is_not_routed_either()
    {
        catalog
            .Setup(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Set(NotProducible("110", QuoteReasonCodes.Dropshipper), NotProducible("125", QuoteReasonCodes.Dropshipper)));
        catalog
            .Setup(c => c.GetDropshipperCost("SKU-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DropshipperCost("uuid", null, null, null, null));

        await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        var allocation = db.Object.Allocations.Single();
        Assert.That(allocation.FulfillmentType, Is.EqualTo(FulfillmentType.Dropshipper));
        Assert.That(allocation.Status, Is.EqualTo(AllocationStatus.NotRouted));
        Assert.That(allocation.TotalCost, Is.Null);
    }

    [Test]
    public async Task An_item_that_is_already_allocated_is_returned_and_not_decided_again()
    {
        var existing = OpenAllocation();
        db.SetupSet(x => x.Allocations, existing);

        var result = await Handler().Handle(Command(FactoryItem()), CancellationToken.None);

        Assert.That(result.Single().Id, Is.EqualTo(existing.Id));
        Assert.That(db.Object.Allocations.Count(), Is.EqualTo(1));
        catalog.Verify(c => c.RequestQuotes(It.IsAny<QuoteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        outputQueue.Verify(q => q.Enqueue(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task A_later_item_of_the_order_is_anchored_to_the_factory_already_used()
    {
        db.SetupSet(x => x.Allocations, OpenAllocation(factoryId: "125"));

        await Handler().Handle(Command(FactoryItem("I2")), CancellationToken.None);

        contextFactory.Verify(f => f.Create(
            It.Is<DecisionContextRequest>(r => r.AnchorFactoryIds.Single() == "125"),
            It.IsAny<BalancerSettings>(),
            It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task All_items_of_an_order_are_decided_together()
    {
        var result = await Handler().Handle(Command(FactoryItem("I1"), FactoryItem("I2")), CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(r => r.FactoryId).Distinct().Count(), Is.EqualTo(1));
        contextFactory.Verify(f => f.Create(
            It.Is<DecisionContextRequest>(r => r.Items.Count == 2),
            It.IsAny<BalancerSettings>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
