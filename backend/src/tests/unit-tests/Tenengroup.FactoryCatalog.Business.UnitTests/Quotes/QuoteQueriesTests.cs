using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Quotes;
using Tenengroup.FactoryCatalog.Business.Quotes;
using Tenengroup.FactoryCatalog.Domains.Quotes;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;

[TestFixture]
public class QuoteQueriesTests
{
    private CatalogFixture catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = new CatalogFixture();
    }

    private Task<ListResult<QuoteDetailsDto>> Search(ListRequest request)
    {
        return new SearchQuotesQueryHandler(catalog.Db.Object).Handle(new SearchQuotesQuery(request), CancellationToken.None);
    }

    private Task<DropshipperCostDto> DropshipperCost(string sku)
    {
        return new GetDropshipperCostQueryHandler(catalog.Db.Object).Handle(new GetDropshipperCostQuery(sku), CancellationToken.None);
    }

    private void SeedQuotes()
    {
        var first = catalog.AddQuoteSet(1, orderId: "ORD-1");
        catalog.AddQuote(10, first, CatalogFixture.Factory110, QuoteStatus.Selected, selectedAt: CatalogFixture.Now.AddDays(-1));
        catalog.AddQuote(11, first, CatalogFixture.Factory125, producible: false);

        var second = catalog.AddQuoteSet(2, orderId: "ORD-2");
        catalog.AddQuote(20, second, CatalogFixture.Factory110, QuoteStatus.Selected, selectedAt: CatalogFixture.Now.AddDays(-5));
        catalog.AddQuote(21, second, CatalogFixture.Factory125, QuoteStatus.Produced, producedAt: CatalogFixture.Now.AddHours(-1));

        var shadow = catalog.AddQuoteSet(3, orderId: "ORD-3", mode: QuoteMode.Shadow);
        catalog.AddQuote(30, shadow, CatalogFixture.Factory110, QuoteStatus.Selected, selectedAt: CatalogFixture.Now.AddDays(-1));
    }

    [Test]
    public async Task GetQuoteSet_ReturnsTheStoredSetWithEveryQuote()
    {
        var set = catalog.AddQuoteSet(1);
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Selected);
        catalog.AddQuote(11, set, CatalogFixture.Factory125, producible: false);
        var otherSet = catalog.AddQuoteSet(2, orderItemId: "ITM002");
        catalog.AddQuote(20, otherSet, CatalogFixture.Factory110);

        var result = await new GetQuoteSetQueryHandler(catalog.Db.Object)
            .Handle(new GetQuoteSetQuery("PQ-ORD456789-ITM001"), CancellationToken.None);

        Assert.That(result.QuoteKey, Is.EqualTo("PQ-ORD456789-ITM001"));
        Assert.That(result.RequestId, Is.EqualTo(set.RequestId));
        Assert.That(result.ValidUntil, Is.EqualTo(set.ValidUntil));
        Assert.That(result.Quotes.Select(q => q.QuoteId), Is.EqualTo(new[] { "PQ-ORD456789-ITM001-110", "PQ-ORD456789-ITM001-125" }));
        Assert.That(result.Quotes[0].Status, Is.EqualTo(QuoteStatus.Selected));
        Assert.That(result.Quotes[0].Cost.Total, Is.EqualTo(45.7m));
        Assert.That(result.Quotes[0].Components[0].ErpId, Is.EqualTo(CatalogFixture.Board));
        Assert.That(result.Quotes[0].Processes, Has.Count.EqualTo(2));
        Assert.That(result.Quotes[1].Producible, Is.False);
        Assert.That(result.Quotes[1].Cost, Is.Null);
        Assert.That(result.Quotes[1].Reasons, Is.EqualTo(new[] { QuoteReasonCodes.ComponentOos }));
    }

    [Test]
    public async Task GetQuoteSet_ShowsReproductionsLinkedToTheOriginal()
    {
        var set = catalog.AddQuoteSet(1);
        catalog.AddQuote(10, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now);
        var reproduction = catalog.AddQuote(11, set, CatalogFixture.Factory110, QuoteStatus.Produced, producedAt: CatalogFixture.Now);
        reproduction.QuoteId = "PQ-ORD456789-ITM001-110-RP1";
        reproduction.ReproductionOfQuoteId = 10;

        var result = await new GetQuoteSetQueryHandler(catalog.Db.Object)
            .Handle(new GetQuoteSetQuery(set.QuoteKey), CancellationToken.None);

        Assert.That(result.Quotes[0].ReproductionOf, Is.Null);
        Assert.That(result.Quotes[1].ReproductionOf, Is.EqualTo("PQ-ORD456789-ITM001-110"));
    }

    [Test]
    public void GetQuoteSet_UnknownKey_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => new GetQuoteSetQueryHandler(catalog.Db.Object)
            .Handle(new GetQuoteSetQuery("PQ-NONE"), CancellationToken.None));
    }

    [Test]
    public async Task Search_WithoutFilters_ReturnsEveryQuoteWithItsSetHeader()
    {
        SeedQuotes();

        var result = await Search(new ListRequest());

        Assert.That(result.TotalCount, Is.EqualTo(5));
        var item = result.Items.Single(i => i.Quote.QuoteId == "PQ-ORD-2-ITM001-125");
        Assert.That(item.OrderId, Is.EqualTo("ORD-2"));
        Assert.That(item.OrderItemId, Is.EqualTo("ITM001"));
        Assert.That(item.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(item.Quote.Status, Is.EqualTo(QuoteStatus.Produced));
        Assert.That(item.Quote.Processes.Select(p => p.ProcessId), Is.EqualTo(new[] { "ENGRAVING", "PACKING" }));
    }

    [Test]
    public async Task Search_ByFactoryStatusAndSelectedDateRange_BuildsTheDailyPlanInput()
    {
        SeedQuotes();
        var request = new ListRequest
        {
            Filters =
            {
                ["factoryId"] = new ListFilter { Values = [CatalogFixture.Factory110] },
                ["status"] = new ListFilter { Values = ["Selected"] },
                ["mode"] = new ListFilter { Values = ["Live"] },
                ["selectedAt"] = new ListFilter { Operator = "between", Value = "2026-11-22T00:00:00Z", ValueTo = "2026-11-24T23:59:59Z" }
            }
        };

        var result = await Search(request);

        Assert.That(result.Items.Select(i => i.Quote.QuoteId), Is.EqualTo(new[] { "PQ-ORD-1-ITM001-110" }));
        Assert.That(result.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Search_ByOrderId_ReturnsThatOrdersQuotes()
    {
        SeedQuotes();
        var request = new ListRequest { Filters = { ["orderId"] = new ListFilter { Values = ["ORD-2"] } }, SortBy = "factoryId" };

        var result = await Search(request);

        Assert.That(result.Items.Select(i => i.Quote.FactoryId), Is.EqualTo(new[] { "110", "125" }));
        Assert.That(result.Items.All(i => i.OrderId == "ORD-2"), Is.True);
    }

    [Test]
    public async Task Search_FreeText_MatchesOrderAndQuoteIds()
    {
        SeedQuotes();

        var result = await Search(new ListRequest { Search = "ord-3" });

        Assert.That(result.Items.Select(i => i.Quote.QuoteId), Is.EqualTo(new[] { "PQ-ORD-3-ITM001-110" }));
    }

    [Test]
    public async Task Search_IsPaged()
    {
        SeedQuotes();

        var result = await Search(new ListRequest { Page = 2, PageSize = 2, SortBy = "quoteId" });

        Assert.That(result.TotalCount, Is.EqualTo(5));
        Assert.That(result.Items.Select(i => i.Quote.QuoteId), Is.EqualTo(new[] { "PQ-ORD-2-ITM001-110", "PQ-ORD-2-ITM001-125" }));
    }

    [Test]
    public void Search_InvalidStatusFilter_IsRejected()
    {
        SeedQuotes();
        var request = new ListRequest { Filters = { ["status"] = new ListFilter { Values = ["Shipped"] } } };

        var exception = Assert.ThrowsAsync<ValidationException>(() => Search(request));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "filters.status" }));
    }

    [Test]
    public async Task DropshipperCost_ReturnsDropshipperAndTotalCost()
    {
        var sku = catalog.AddSku(CatalogFixture.SkuUuid, "DS-1001", SkuProductType.Dropshipper);
        sku.DropshipperId = "DS-77";
        sku.DropshipperName = "Print Partner";
        sku.DropshipperTotalCostAmount = 18.5m;
        sku.DropshipperTotalCostCurrency = "USD";
        catalog.AddAlias(CatalogFixture.SkuUuid, "TGN-DS-1001");

        var byUuid = await DropshipperCost(CatalogFixture.SkuUuid.ToString());
        var byAlias = await DropshipperCost("TGN-DS-1001");

        Assert.That(byUuid, Is.EqualTo(new DropshipperCostDto(
            CatalogFixture.SkuUuid, "DS-1001", "DS-77", "Print Partner", new DropshipperTotalCostDto(18.5m, "USD"))));
        Assert.That(byAlias, Is.EqualTo(byUuid));
    }

    [Test]
    public async Task DropshipperCost_WithoutCost_ReturnsNullTotal()
    {
        catalog.AddSku(CatalogFixture.SkuUuid, "DS-1001", SkuProductType.Dropshipper);

        var result = await DropshipperCost("DS-1001");

        Assert.That(result.TotalCost, Is.Null);
    }

    [Test]
    public void DropshipperCost_ManufacturedSku_IsRejected()
    {
        catalog.AddSku(CatalogFixture.SkuUuid);

        var exception = Assert.ThrowsAsync<ValidationException>(() => DropshipperCost(CatalogFixture.SkuUuid.ToString()));

        Assert.That(exception.ValidationMessages.Keys, Is.EqualTo(new[] { "sku" }));
    }

    [Test]
    public void DropshipperCost_UnknownSku_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => DropshipperCost("NOPE"));
    }
}
