using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

[TestFixture]
public class RequestQuotesCommandHandlerTests
{
    private const string OrderId = "ORD456789";
    private const string OrderItemId = "ITM001";
    private const string Stone = "STN.DIA.RND.002";
    private const string AlternativeBoard = "R.VM.BRD.09MM";

    private static readonly Guid RequestId = Guid.Parse("b7e1c0d4-1f2a-4c55-9a10-3e2f9d8c7a61");

    private CatalogFixture catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = CatalogFixture.Standard();
    }

    private Task<QuoteSetDto> Quote(RequestQuotesCommand command)
    {
        return new RequestQuotesCommandHandler(
                catalog.Db.Object,
                catalog.CurrentUser.Object,
                catalog.Time,
                new QuotesOptions(QuotesOptions.DefaultValidityMinutes, QuotesOptions.DefaultCurrency))
            .Handle(command, CancellationToken.None);
    }

    private static RequestQuotesCommand Command(
        Guid? requestId = null,
        string orderId = OrderId,
        string orderItemId = OrderItemId,
        string sku = null,
        int quantity = 1,
        QuoteMode mode = QuoteMode.Live,
        params CbdItem[] cbd)
    {
        return new RequestQuotesCommand(
            requestId ?? RequestId,
            orderId,
            orderItemId,
            sku ?? CatalogFixture.SkuUuid.ToString(),
            quantity,
            cbd.Length == 0 ? [new CbdItem(CatalogFixture.Board, 1)] : cbd,
            mode);
    }

    private static QuoteDto For(QuoteSetDto set, string factoryId)
    {
        return set.Quotes.Single(q => q.FactoryId == factoryId);
    }

    [Test]
    public async Task ProducibleFactories_ReturnCostSignalAndShare()
    {
        var result = await Quote(Command());

        Assert.That(result.QuoteKey, Is.EqualTo("PQ-ORD456789-ITM001"));
        Assert.That(result.ValidUntil, Is.EqualTo(CatalogFixture.Now.AddMinutes(15)));
        Assert.That(result.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(result.Quotes.Select(q => q.FactoryId), Is.EqualTo(new[] { "110", "125" }));

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-110"));
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.Reasons, Is.Empty);
        Assert.That(quote.Cost, Is.EqualTo(new QuoteCostDto(21m, 1.1m, 1.4m, 23.5m, "USD")));
        Assert.That(quote.CapacitySignalPct, Is.EqualTo(100));
        Assert.That(quote.SharePct, Is.EqualTo(60m));
        Assert.That(quote.Status, Is.EqualTo(QuoteStatus.Quoted));
        Assert.That(quote.Warnings, Is.Empty);
        Assert.That(quote.Components, Is.EqualTo(new[]
        {
            new QuoteComponentDto(CatalogFixture.Board, 1, 21m, StockStatuses.Ok, null, 21m, "USD")
        }));
        Assert.That(quote.Processes, Is.EqualTo(new[]
        {
            new QuoteProcessDto(CatalogFixture.Engraving, 2, 100, 1.1m, 100, "USD"),
            new QuoteProcessDto(Process.PackingProcessId, 1, 100, 1.4m, 100, "USD")
        }));
        Assert.That(For(result, CatalogFixture.Factory125).SharePct, Is.EqualTo(40m));
    }

    [Test]
    public async Task LiveQuote_IsStoredForLaterLookup()
    {
        await Quote(Command());

        Assert.That(catalog.QuoteSets, Has.Count.EqualTo(1));
        Assert.That(catalog.QuoteSets[0].RequestId, Is.EqualTo(RequestId));
        Assert.That(catalog.QuoteSets[0].Mode, Is.EqualTo(QuoteMode.Live));
        Assert.That(catalog.QuoteSets[0].CbdJson, Does.Contain(CatalogFixture.Board));
        Assert.That(catalog.Quotes.Select(q => q.QuoteId), Is.EqualTo(new[] { "PQ-ORD456789-ITM001-110", "PQ-ORD456789-ITM001-125" }));
        Assert.That(catalog.Quotes.All(q => q.Status == QuoteStatus.Quoted && q.QuoteSet == catalog.QuoteSets[0]), Is.True);
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ShadowQuote_IsStored()
    {
        var result = await Quote(Command(mode: QuoteMode.Shadow));

        Assert.That(result.Mode, Is.EqualTo(QuoteMode.Shadow));
        Assert.That(catalog.QuoteSets.Single().Mode, Is.EqualTo(QuoteMode.Shadow));
    }

    [Test]
    public async Task EstimateQuote_StoresNothing()
    {
        var result = await Quote(Command(orderId: null, orderItemId: null, mode: QuoteMode.Estimate));

        Assert.That(result.QuoteKey, Is.EqualTo($"EST-{RequestId:N}"));
        Assert.That(For(result, CatalogFixture.Factory110).Cost.Total, Is.EqualTo(23.5m));
        Assert.That(catalog.QuoteSets, Is.Empty);
        Assert.That(catalog.Quotes, Is.Empty);
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SameRequestId_ReturnsStoredSetWithoutRequoting()
    {
        var first = await Quote(Command());
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);

        var second = await Quote(Command());

        Assert.That(second.QuoteKey, Is.EqualTo(first.QuoteKey));
        Assert.That(For(second, CatalogFixture.Factory110).Producible, Is.True);
        Assert.That(catalog.QuoteSets, Has.Count.EqualTo(1));
        Assert.That(catalog.Quotes, Has.Count.EqualTo(2));
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SameRequestIdForAnotherOrderItem_Conflicts()
    {
        await Quote(Command());

        Assert.ThrowsAsync<ConflictException>(() => Quote(Command(orderItemId: "ITM002")));
    }

    [Test]
    public async Task ConcurrentRequestWithSameRequestId_ReturnsTheStoredSet()
    {
        catalog.Db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateException("duplicate request_id"));

        var result = await Quote(Command());

        Assert.That(result.QuoteKey, Is.EqualTo("PQ-ORD456789-ITM001"));
        catalog.Db.Verify(x => x.DiscardChanges(), Times.Once);
    }

    [Test]
    public async Task RequoteOfTheSameOrderItem_GetsARevisionSuffix()
    {
        await Quote(Command());

        var requote = await Quote(Command(requestId: Guid.NewGuid()));

        Assert.That(requote.QuoteKey, Is.EqualTo("PQ-ORD456789-ITM001-R2"));
        Assert.That(requote.Quotes[0].QuoteId, Is.EqualTo("PQ-ORD456789-ITM001-R2-110"));
    }

    [Test]
    public async Task SkuIsResolvedByLegacyAlias()
    {
        catalog.AddAlias(CatalogFixture.SkuUuid, "TGN-N-4609-SD-VMGP_1.1");

        var result = await Quote(Command(sku: "tgn-n-4609-sd-vmgp_1.1"));

        Assert.That(result.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(catalog.QuoteSets.Single().SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
    }

    [Test]
    public async Task SkuIsResolvedBySiteSku()
    {
        var result = await Quote(Command(sku: "TGI-N-4609"));

        Assert.That(result.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
    }

    [Test]
    public void AmbiguousSiteSku_IsRejected()
    {
        catalog.AddSku(Guid.NewGuid());

        var exception = Assert.ThrowsAsync<ValidationException>(() => Quote(Command(sku: "TGI-N-4609")));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "sku" }));
    }

    [Test]
    public void UnknownSku_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Quote(Command(sku: "NO-SUCH-SKU")));
        Assert.ThrowsAsync<EntityNotFoundException>(() => Quote(Command(sku: Guid.NewGuid().ToString())));
    }

    [Test]
    public async Task ItemQuantity_MultipliesComponentsAndProcesses()
    {
        var result = await Quote(Command(quantity: 3, cbd: new CbdItem(CatalogFixture.Board, 2)));

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Components[0].Quantity, Is.EqualTo(6m));
        Assert.That(quote.Processes[0].Quantity, Is.EqualTo(6m));
        Assert.That(quote.Cost, Is.EqualTo(new QuoteCostDto(126m, 3.3m, 4.2m, 133.5m, "USD")));
    }

    [Test]
    public async Task DuplicatedCbdEntries_AreMerged()
    {
        var result = await Quote(Command(cbd: [new CbdItem(CatalogFixture.Board, 1), new CbdItem(CatalogFixture.Board, 2)]));

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Components, Has.Count.EqualTo(1));
        Assert.That(quote.Components[0].Quantity, Is.EqualTo(3m));
    }

    [Test]
    public async Task FactoryWithoutProducibleFlag_IsReturnedWithReasonAndItsShareFlowsToTheOthers()
    {
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory125, producible: false);

        var result = await Quote(Command());

        var notProducible = For(result, CatalogFixture.Factory125);
        Assert.That(notProducible.Producible, Is.False);
        Assert.That(notProducible.Reasons, Is.EqualTo(new[] { QuoteReasonCodes.NotProducible }));
        Assert.That(notProducible.Cost, Is.Null);
        Assert.That(notProducible.CapacitySignalPct, Is.Null);
        Assert.That(notProducible.SharePct, Is.EqualTo(0m));
        Assert.That(For(result, CatalogFixture.Factory110).SharePct, Is.EqualTo(100m));
    }

    [Test]
    public async Task FactoryWithoutSkuFactoryRow_IsNotProducible()
    {
        catalog.SkuFactories.RemoveAll(f => f.FactoryId == CatalogFixture.Factory125);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.NotProducible }));
    }

    [Test]
    public async Task InactiveFactory_IsNotQuoted()
    {
        catalog.AddFactory(CatalogFixture.Factory315, isActive: false);

        var result = await Quote(Command());

        Assert.That(result.Quotes.Select(q => q.FactoryId), Is.EqualTo(new[] { "110", "125" }));
    }

    [Test]
    public async Task ComponentWithZeroStock_RulesTheFactoryOut()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory125);
        Assert.That(quote.Producible, Is.False);
        Assert.That(quote.Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
        Assert.That(quote.Components[0].StockStatus, Is.EqualTo(StockStatuses.OutOfStock));
        Assert.That(quote.SharePct, Is.EqualTo(0m));
        Assert.That(For(result, CatalogFixture.Factory110).SharePct, Is.EqualTo(100m));
    }

    [Test]
    public async Task ManualOosFlag_RulesTheFactoryOutEvenWithStock()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 50, manualOos: true);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task ComponentWithoutStockRow_IsOutOfStock()
    {
        catalog.ComponentFactories.RemoveAll(s => s.FactoryId == CatalogFixture.Factory125);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task ProductionFloorStock_CountsTowardsTheTotal()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0, productionFloor: 3);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Producible, Is.True);
    }

    [Test]
    public async Task BalancerAlternativeInStock_KeepsTheFactoryEligibleAndIsPriced()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.AddComponent(AlternativeBoard, unitCost: 25m);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory125, warehouse: 4);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory125);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.Reasons, Is.Empty);
        Assert.That(quote.Components[0], Is.EqualTo(
            new QuoteComponentDto(CatalogFixture.Board, 1, 25m, StockStatuses.OutOfStock, AlternativeBoard, 25m, "USD")));
        Assert.That(quote.Cost.Material, Is.EqualTo(25m));
        Assert.That(quote.SharePct, Is.EqualTo(40m));
        Assert.That(For(result, CatalogFixture.Factory110).Components[0].AlternativeUsed, Is.Null);
    }

    [Test]
    public async Task AlternativeNotFlaggedForBalancer_DoesNotHelp()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.AddComponent(AlternativeBoard, unitCost: 25m);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory125, warehouse: 4);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard, forBalancer: false, forPlanner: true);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task AlternativeOutOfStockInThatFactory_DoesNotHelp()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.AddComponent(AlternativeBoard, unitCost: 25m);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory110, warehouse: 4);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task InactiveAlternative_DoesNotHelp()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.AddComponent(AlternativeBoard, unitCost: 25m, status: ComponentStatus.Inactive);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory125, warehouse: 4);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Producible, Is.False);
    }

    [Test]
    public async Task AlternativesAreOneWay()
    {
        catalog.AddComponent(AlternativeBoard, unitCost: 25m);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory125, warehouse: 0);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory110, warehouse: 0);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Quote(Command(cbd: new CbdItem(AlternativeBoard, 1)));

        Assert.That(result.Quotes.All(q => q.Reasons.SequenceEqual(new[] { QuoteReasonCodes.ComponentOos })), Is.True);
    }

    [Test]
    public async Task AlternativeWithOkStock_IsPreferredOverLowStock()
    {
        const string lowAlternative = "A.LOW";
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.AddComponent(lowAlternative, unitCost: 1m, criticalLevel: 10);
        catalog.SetStock(lowAlternative, CatalogFixture.Factory125, warehouse: 2);
        catalog.AddComponent(AlternativeBoard, unitCost: 25m);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory125, warehouse: 4);
        catalog.AddAlternative(CatalogFixture.Board, lowAlternative);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Components[0].AlternativeUsed, Is.EqualTo(AlternativeBoard));
    }

    [Test]
    public async Task LowStock_LowersTheCapacitySignal()
    {
        catalog.Components.Single(c => c.ErpId == CatalogFixture.Board).CriticalStockLevel = 20;

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.Components[0].StockStatus, Is.EqualTo(StockStatuses.Low));
        Assert.That(quote.CapacitySignalPct, Is.EqualTo(50));
    }

    [Test]
    public async Task FactoryCriticalLevelOverride_WinsOverTheGlobalLevel()
    {
        catalog.Components.Single(c => c.ErpId == CatalogFixture.Board).CriticalStockLevel = 20;
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 10, criticalOverride: 5);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory110).Components[0].StockStatus, Is.EqualTo(StockStatuses.Ok));
        Assert.That(For(result, CatalogFixture.Factory125).Components[0].StockStatus, Is.EqualTo(StockStatuses.Low));
    }

    [Test]
    public async Task OnlyCbdComponents_AreCheckedAndPriced()
    {
        catalog.AddComponent(Stone, unitCost: 100m);
        catalog.MapComponent(CatalogFixture.SkuUuid, CatalogFixture.Board, ComponentUsage.Static);
        catalog.MapComponent(CatalogFixture.SkuUuid, Stone, ComponentUsage.Dynamic);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.Components.Select(c => c.ErpId), Is.EqualTo(new[] { CatalogFixture.Board }));
        Assert.That(quote.Cost.Material, Is.EqualTo(21m));
    }

    [Test]
    public async Task SelectedDynamicComponentOutOfStock_RulesTheFactoryOut()
    {
        catalog.AddComponent(Stone, unitCost: 100m);
        catalog.SetStock(Stone, CatalogFixture.Factory110, warehouse: 1);
        catalog.MapComponent(CatalogFixture.SkuUuid, Stone, ComponentUsage.Dynamic);

        var result = await Quote(Command(cbd: [new CbdItem(CatalogFixture.Board, 1), new CbdItem(Stone, 1)]));

        Assert.That(For(result, CatalogFixture.Factory110).Cost.Material, Is.EqualTo(121m));
        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task UnknownCbdComponent_FailsEveryFactoryWithAClearReason()
    {
        var result = await Quote(Command(cbd: [new CbdItem(CatalogFixture.Board, 1), new CbdItem("NO.SUCH.COMPONENT", 1)]));

        Assert.That(result.Quotes, Has.Count.EqualTo(2));
        Assert.That(result.Quotes.All(q => !q.Producible && q.Cost == null && q.SharePct == 0), Is.True);
        Assert.That(result.Quotes.All(q => q.Reasons.SequenceEqual(new[] { QuoteReasonCodes.UnknownComponent })), Is.True);
        Assert.That(result.Quotes[0].Components[1], Is.EqualTo(
            new QuoteComponentDto("NO.SUCH.COMPONENT", 1, null, StockStatuses.Unknown, null, null, null)));
        Assert.That(catalog.QuoteSets, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ProcessAtZeroCapacity_RulesTheFactoryOut()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 0, laborCost: 0.55m);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory125);
        Assert.That(quote.Producible, Is.False);
        Assert.That(quote.Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ProcessOffline }));
        Assert.That(quote.Processes[0].CapacityPct, Is.EqualTo(0));
    }

    [Test]
    public async Task ProcessNotRunInTheFactory_RulesTheFactoryOut()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, runsHere: false);
        catalog.ProcessFactories.RemoveAll(p => p.FactoryId == CatalogFixture.Factory110 && p.ProcessId == CatalogFixture.Engraving);

        var result = await Quote(Command());

        Assert.That(result.Quotes.All(q => q.Reasons.SequenceEqual(new[] { QuoteReasonCodes.ProcessNotAvailable })), Is.True);
        Assert.That(result.Quotes[0].Processes[0], Is.EqualTo(new QuoteProcessDto(CatalogFixture.Engraving, 2, null, null, null, null)));
    }

    [Test]
    public async Task SeveralProblems_AreAllReported()
    {
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory125, producible: false);
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 0);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory125).Reasons, Is.EqualTo(new[]
        {
            QuoteReasonCodes.NotProducible, QuoteReasonCodes.ComponentOos, QuoteReasonCodes.ProcessOffline
        }));
    }

    [Test]
    public async Task PartialCapacity_LowersTheSignalButKeepsTheFactory()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110, capacityPct: 30, laborCost: 0.55m);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.CapacitySignalPct, Is.EqualTo(30));
        Assert.That(quote.Processes[0].CapacityPct, Is.EqualTo(30));
    }

    [Test]
    public async Task CapacitySignal_UsesTodaysRemainingCapacity()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110, capacityPct: 50, laborCost: 0.55m, maxPerDay: 20);
        var producedSet = catalog.AddQuoteSet(50, orderId: "OLD");
        for (var i = 0; i < 5; i++)
        {
            catalog.AddQuote(100 + i, producedSet, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-1));
        }

        catalog.AddQuote(200, producedSet, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddDays(-1));
        catalog.AddQuote(201, producedSet, CatalogFixture.Factory125, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-1));
        catalog.AddQuote(202, producedSet, CatalogFixture.Factory110, QuoteStatus.Selected);
        var otherSkuSet = catalog.AddQuoteSet(51, orderId: "OTHER", skuUuid: Guid.NewGuid());
        catalog.AddQuote(203, otherSkuSet, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-1));

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Processes[0].CapacityPct, Is.EqualTo(50));
        Assert.That(quote.Processes[0].RemainingPct, Is.EqualTo(25));
        Assert.That(quote.CapacitySignalPct, Is.EqualTo(25));
        Assert.That(quote.Producible, Is.True);
    }

    [Test]
    public async Task CapacityUsedUpToday_GivesZeroSignalButStaysProducible()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110, laborCost: 0.55m, maxPerDay: 1);
        var producedSet = catalog.AddQuoteSet(50, orderId: "OLD");
        catalog.AddQuote(100, producedSet, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-1));
        catalog.AddQuote(101, producedSet, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-2));

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.CapacitySignalPct, Is.EqualTo(0));
    }

    [Test]
    public async Task InvalidSku_GetsNoProducibleQuotes()
    {
        catalog.Skus.Single().ValidationStatus = SkuValidationStatus.Invalid;

        var result = await Quote(Command());

        Assert.That(result.Quotes.All(q => !q.Producible && q.SharePct == 0), Is.True);
        Assert.That(result.Quotes.All(q => q.Reasons.SequenceEqual(new[] { QuoteReasonCodes.SkuInvalid })), Is.True);
    }

    [Test]
    public async Task DropshipperSku_IsNeverRoutedToAFactory()
    {
        catalog.Skus.Single().ProductType = SkuProductType.Dropshipper;

        var result = await Quote(Command());

        Assert.That(result.Quotes, Has.Count.EqualTo(2));
        Assert.That(result.Quotes.All(q => !q.Producible && q.Cost == null && q.Components.Count == 0), Is.True);
        Assert.That(result.Quotes.All(q => q.Reasons.SequenceEqual(new[] { QuoteReasonCodes.Dropshipper })), Is.True);
    }

    [Test]
    public async Task ShareOverride_WinsOverTheGlobalShare()
    {
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory110, overrideSharePct: 90);
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory125, overrideSharePct: 10);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory110).SharePct, Is.EqualTo(90m));
        Assert.That(For(result, CatalogFixture.Factory125).SharePct, Is.EqualTo(10m));
        Assert.That(catalog.SkuFactories.Select(f => f.OverrideSharePct), Is.EqualTo(new decimal?[] { 90, 10 }));
    }

    [Test]
    public async Task DroppedFactoryShare_IsSplitInProportionAmongTheRest()
    {
        catalog.GlobalFactoryShares.Clear();
        catalog.GlobalFactoryShares.Add(new GlobalFactoryShare { FactoryId = CatalogFixture.Factory110, Pct = 50 });
        catalog.GlobalFactoryShares.Add(new GlobalFactoryShare { FactoryId = CatalogFixture.Factory125, Pct = 30 });
        catalog.AddFactory(CatalogFixture.Factory315, sharePct: 20);
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory315, warehouse: 10);
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory315, laborCost: 0.55m);
        catalog.SetProcessFactory(Process.PackingProcessId, CatalogFixture.Factory315, laborCost: 1.4m);
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory315);
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory110).SharePct, Is.EqualTo(71.43m));
        Assert.That(For(result, CatalogFixture.Factory125).SharePct, Is.EqualTo(0m));
        Assert.That(For(result, CatalogFixture.Factory315).SharePct, Is.EqualTo(28.57m));
        Assert.That(catalog.GlobalFactoryShares.Select(s => s.Pct), Is.EqualTo(new[] { 50m, 30m, 20m }));
    }

    [Test]
    public async Task MissingCosts_ArePricedAsZeroAndFlagged()
    {
        catalog.Components.Single(c => c.ErpId == CatalogFixture.Board).UnitCostAmount = null;
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110);
        catalog.SkuProcesses.RemoveAll(p => p.ProcessId == Process.PackingProcessId);

        var result = await Quote(Command());

        var quote = For(result, CatalogFixture.Factory110);
        Assert.That(quote.Producible, Is.True);
        Assert.That(quote.Cost, Is.EqualTo(new QuoteCostDto(0m, 0m, 0m, 0m, "USD")));
        Assert.That(quote.Components[0].UnitCost, Is.Null);
        Assert.That(quote.Warnings, Is.EqualTo(new[]
        {
            QuoteWarningCodes.ComponentCostMissing, QuoteWarningCodes.LaborCostMissing, QuoteWarningCodes.PackingNotMapped
        }));
    }

    [Test]
    public async Task CostInAnotherCurrency_IsFlagged()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110, laborCost: 0.55m, currency: "EUR");

        var result = await Quote(Command());

        Assert.That(For(result, CatalogFixture.Factory110).Warnings, Is.EqualTo(new[] { QuoteWarningCodes.CurrencyMismatch }));
        Assert.That(For(result, CatalogFixture.Factory125).Warnings, Is.Empty);
    }

    [Test]
    public void InvalidRequest_IsRejectedWithFieldErrors()
    {
        var command = new RequestQuotesCommand(Guid.Empty, " ", null, "", 0, [new CbdItem(CatalogFixture.Board, 0)], QuoteMode.Live);

        var exception = Assert.ThrowsAsync<ValidationException>(() => Quote(command));

        Assert.That(exception.ValidationMessages.Keys, Is.EquivalentTo(new[]
        {
            "requestId", "skuUuid", "quantity", "cbd", "orderId", "orderItemId"
        }));
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void UiUser_CannotRequestQuotes()
    {
        catalog.CurrentUser.Setup(u => u.IsServiceAccount).Returns(false);

        Assert.ThrowsAsync<ForbiddenException>(() => Quote(Command()));
    }
}
