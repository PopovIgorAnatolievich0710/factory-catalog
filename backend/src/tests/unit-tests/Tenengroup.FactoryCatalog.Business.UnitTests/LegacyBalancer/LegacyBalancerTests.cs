using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.Business.LegacyBalancer;
using Tenengroup.FactoryCatalog.Business.UnitTests.Quotes;
using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.LegacyBalancer;

[TestFixture]
public class LegacyBalancerTests
{
    private const string OrderId = "ORD456789";
    private const string Stone = "STN.DIA.RND.002";
    private const string Chain = "CHN.VM.CABLE.45";
    private const string AlternativeBoard = "R.VM.BRD.09MM";

    private static readonly string[] BothFactories = [CatalogFixture.Factory110, CatalogFixture.Factory125];

    private CatalogFixture catalog;

    [SetUp]
    public void SetUp()
    {
        catalog = CatalogFixture.Standard();
        catalog.MapComponent(CatalogFixture.SkuUuid, CatalogFixture.Board, ComponentUsage.Static);
        catalog.AddComponent(Stone);
        catalog.MapComponent(CatalogFixture.SkuUuid, Stone, ComponentUsage.Dynamic);
        catalog.SetStock(Stone, CatalogFixture.Factory110, warehouse: 5);
        catalog.SetStock(Stone, CatalogFixture.Factory125, warehouse: 5);
    }

    private Task<LegacyEligibilityDto> Check(
        string sku = null,
        string[] cbd = null,
        string[] factoryIds = null,
        string orderItemId = "ITM001",
        bool withoutSku = false)
    {
        return new CheckLegacyEligibilityCommandHandler(catalog.Db.Object, catalog.CurrentUser.Object, catalog.Time)
            .Handle(
                new CheckLegacyEligibilityCommand(
                    OrderId,
                    orderItemId,
                    withoutSku ? null : sku ?? CatalogFixture.SkuUuid.ToString(),
                    cbd ?? [CatalogFixture.Board],
                    factoryIds ?? BothFactories),
                CancellationToken.None);
    }

    private void VerifySaved(Times times)
    {
        catalog.Db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), times);
    }

    [Test]
    public async Task EverythingInStock_AllFactoriesEligibleAndNothingLogged()
    {
        var result = await Check();

        Assert.That(result.AnyEligible, Is.True);
        Assert.That(result.FirstEligibleFactoryId, Is.EqualTo(CatalogFixture.Factory110));
        Assert.That(result.Message, Is.Null);
        Assert.That(result.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(result.Factories.Select(f => f.FactoryId), Is.EqualTo(BothFactories));
        Assert.That(result.Factories.All(f => f.Eligible && f.Skips.Count == 0), Is.True);
        Assert.That(catalog.SkipLogs, Is.Empty);
        VerifySaved(Times.Never());
    }

    [Test]
    public async Task CbdComponentOutOfStock_SkipsThatFactoryAndLogsIt()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);

        var result = await Check();

        Assert.That(result.Factories[0], Is.EqualTo(new LegacyFactoryEligibilityDto(
                CatalogFixture.Factory110, false, [new LegacySkipDto(SkipReason.ComponentOos, CatalogFixture.Board, null)]))
            .Using<LegacyFactoryEligibilityDto>((a, b) =>
                a.FactoryId == b.FactoryId && a.Eligible == b.Eligible && a.Skips.SequenceEqual(b.Skips)));
        Assert.That(result.Factories[1].Eligible, Is.True);
        Assert.That(result.FirstEligibleFactoryId, Is.EqualTo(CatalogFixture.Factory125));

        Assert.That(catalog.SkipLogs, Has.Count.EqualTo(1));
        var log = catalog.SkipLogs[0];
        Assert.That(log.OrderId, Is.EqualTo(OrderId));
        Assert.That(log.OrderItemId, Is.EqualTo("ITM001"));
        Assert.That(log.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(log.FactoryId, Is.EqualTo(CatalogFixture.Factory110));
        Assert.That(log.Reason, Is.EqualTo(SkipReason.ComponentOos));
        Assert.That(log.ComponentErpId, Is.EqualTo(CatalogFixture.Board));
        Assert.That(log.ProcessId, Is.Null);
        Assert.That(log.CreatedAt, Is.EqualTo(CatalogFixture.Now));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task ManualOosFlag_SkipsTheFactory()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 30, manualOos: true);

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.False);
    }

    [Test]
    public async Task StaticComponent_CountsEvenWhenMissingFromTheCbd()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);

        var result = await Check(cbd: []);

        Assert.That(result.Factories[0].Skips, Is.EqualTo(new[] { new LegacySkipDto(SkipReason.ComponentOos, CatalogFixture.Board, null) }));
    }

    [Test]
    public async Task DynamicComponent_NotInTheCbd_DoesNotBlock()
    {
        catalog.SetStock(Stone, CatalogFixture.Factory110, warehouse: 0);

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.True);
        Assert.That(catalog.SkipLogs, Is.Empty);
    }

    [Test]
    public async Task DynamicComponent_InTheCbd_Blocks()
    {
        catalog.SetStock(Stone, CatalogFixture.Factory110, warehouse: 0);

        var result = await Check(cbd: [CatalogFixture.Board, Stone]);

        Assert.That(result.Factories[0].Skips, Is.EqualTo(new[] { new LegacySkipDto(SkipReason.ComponentOos, Stone, null) }));
    }

    [Test]
    public async Task WithoutSkuMapping_OnlyTheCbdIsChecked()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 0);

        var result = await Check(cbd: [Stone], withoutSku: true);

        Assert.That(result.SkuUuid, Is.Null);
        Assert.That(result.Factories.All(f => f.Eligible), Is.True);
    }

    [Test]
    public async Task UnknownSku_FallsBackToTheCbdCheck()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);

        var result = await Check(sku: "NO-SUCH-SKU");

        Assert.That(result.SkuUuid, Is.Null);
        Assert.That(result.Factories[0].Eligible, Is.False);
        Assert.That(catalog.SkipLogs.Single().SkuUuid, Is.Null);
    }

    [Test]
    public async Task SkuIsResolvedByAlias()
    {
        catalog.AddAlias(CatalogFixture.SkuUuid, "TGN-N-4609");
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 0);

        var result = await Check(sku: "TGN-N-4609");

        Assert.That(result.SkuUuid, Is.EqualTo(CatalogFixture.SkuUuid));
        Assert.That(result.Factories[1].Eligible, Is.False);
    }

    [Test]
    public async Task BalancerAlternativeInStock_KeepsTheFactoryEligible()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        catalog.AddComponent(AlternativeBoard);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory110, warehouse: 2);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard);

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.True);
        Assert.That(catalog.SkipLogs, Is.Empty);
    }

    [Test]
    public async Task PlannerOnlyAlternative_DoesNotKeepTheFactoryEligible()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        catalog.AddComponent(AlternativeBoard);
        catalog.SetStock(AlternativeBoard, CatalogFixture.Factory110, warehouse: 2);
        catalog.AddAlternative(CatalogFixture.Board, AlternativeBoard, forBalancer: false, forPlanner: true);

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.False);
    }

    [Test]
    public async Task ProcessAtZeroCapacity_SkipsTheFactoryAndLogsTheProcess()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 0);

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.True);
        Assert.That(result.Factories[1].Skips, Is.EqualTo(new[] { new LegacySkipDto(SkipReason.ProcessOffline, null, CatalogFixture.Engraving) }));
        var log = catalog.SkipLogs.Single();
        Assert.That(log.FactoryId, Is.EqualTo(CatalogFixture.Factory125));
        Assert.That(log.Reason, Is.EqualTo(SkipReason.ProcessOffline));
        Assert.That(log.ProcessId, Is.EqualTo(CatalogFixture.Engraving));
        Assert.That(log.ComponentErpId, Is.Null);
    }

    [Test]
    public async Task PartialCapacity_IsIgnored()
    {
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory125, capacityPct: 10);

        var result = await Check();

        Assert.That(result.Factories.All(f => f.Eligible), Is.True);
    }

    [Test]
    public async Task ProcessAtZeroCapacity_OfAnotherSku_DoesNotBlock()
    {
        catalog.SetProcessFactory("CASTING", CatalogFixture.Factory125, capacityPct: 0);

        var result = await Check();

        Assert.That(result.Factories.All(f => f.Eligible), Is.True);
    }

    [Test]
    public async Task EveryCause_IsReportedAndLogged()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        catalog.SetStock(Stone, CatalogFixture.Factory110, warehouse: 0);
        catalog.SetProcessFactory(CatalogFixture.Engraving, CatalogFixture.Factory110, capacityPct: 0);

        var result = await Check(cbd: [CatalogFixture.Board, Stone]);

        Assert.That(result.Factories[0].Skips, Is.EqualTo(new[]
        {
            new LegacySkipDto(SkipReason.ComponentOos, CatalogFixture.Board, null),
            new LegacySkipDto(SkipReason.ComponentOos, Stone, null),
            new LegacySkipDto(SkipReason.ProcessOffline, null, CatalogFixture.Engraving)
        }));
        Assert.That(catalog.SkipLogs, Has.Count.EqualTo(3));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task NoFactoryQualifies_SaysSoAndLogsEverySkip()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);

        var result = await Check();

        Assert.That(result.AnyEligible, Is.False);
        Assert.That(result.FirstEligibleFactoryId, Is.Null);
        Assert.That(result.Message, Does.Contain("keep the current allocation behaviour"));
        Assert.That(catalog.SkipLogs.Select(l => l.FactoryId), Is.EqualTo(BothFactories));
    }

    [Test]
    public async Task CandidateOrder_IsKept()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory125, warehouse: 0);

        var result = await Check(factoryIds: [CatalogFixture.Factory125, CatalogFixture.Factory110, CatalogFixture.Factory125]);

        Assert.That(result.Factories.Select(f => f.FactoryId), Is.EqualTo(new[] { "125", "110" }));
        Assert.That(result.FirstEligibleFactoryId, Is.EqualTo(CatalogFixture.Factory110));
    }

    [Test]
    public async Task RepeatedCheckOnTheSameDay_DoesNotDuplicateTheLog()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        await Check();

        var result = await Check();

        Assert.That(result.Factories[0].Eligible, Is.False);
        Assert.That(catalog.SkipLogs, Has.Count.EqualTo(1));
        VerifySaved(Times.Once());
    }

    [Test]
    public async Task SkipLoggedOnAnEarlierDay_IsLoggedAgain()
    {
        catalog.SetStock(CatalogFixture.Board, CatalogFixture.Factory110, warehouse: 0);
        await Check();
        catalog.SkipLogs[0].CreatedAt = CatalogFixture.Now.AddDays(-1);

        await Check();

        Assert.That(catalog.SkipLogs, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ComponentUnknownToTheCatalog_IsReportedButDoesNotSkip()
    {
        var result = await Check(cbd: [CatalogFixture.Board, Chain]);

        Assert.That(result.Factories.All(f => f.Eligible), Is.True);
        Assert.That(result.UnknownComponents, Is.EqualTo(new[] { Chain }));
    }

    [Test]
    public void UnknownFactory_IsRejected()
    {
        var exception = Assert.ThrowsAsync<ValidationException>(() => Check(factoryIds: [CatalogFixture.Factory110, "999"]));

        Assert.That(exception.ValidationMessages["factoryIds"][0], Does.Contain("999"));
    }

    [Test]
    public void MissingOrderOrFactories_IsRejected()
    {
        var handler = new CheckLegacyEligibilityCommandHandler(catalog.Db.Object, catalog.CurrentUser.Object, catalog.Time);

        var exception = Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CheckLegacyEligibilityCommand(" ", null, null, [], []), CancellationToken.None));

        Assert.That(exception.ValidationMessages.Keys, Is.EquivalentTo(new[] { "orderId", "factoryIds" }));
    }

    [Test]
    public void UiUser_CannotRunTheCheck()
    {
        catalog.CurrentUser.Setup(u => u.IsServiceAccount).Returns(false);

        Assert.ThrowsAsync<ForbiddenException>(() => Check());
    }

    [Test]
    public async Task SkipLog_IsSearchableByOrderId()
    {
        SeedSkipLog();

        var result = await new SearchSkipLogQueryHandler(catalog.Db.Object)
            .Handle(new SearchSkipLogQuery(new ListRequest { Search = "ord-2" }), CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items.Single(), Is.EqualTo(new SkipLogEntryDto(
            3, "ORD-2", null, null, CatalogFixture.Factory110, SkipReason.ProcessOffline, null, CatalogFixture.Engraving,
            CatalogFixture.Now.AddHours(-1))));
    }

    [Test]
    public async Task SkipLog_FiltersByFactoryAndReason_NewestFirstWhenAsked()
    {
        SeedSkipLog();
        var request = new ListRequest
        {
            SortDirection = "desc",
            Filters =
            {
                ["orderId"] = new ListFilter { Values = ["ORD-1"] },
                ["reason"] = new ListFilter { Values = ["ComponentOos"] }
            }
        };

        var result = await new SearchSkipLogQueryHandler(catalog.Db.Object)
            .Handle(new SearchSkipLogQuery(request), CancellationToken.None);

        Assert.That(result.Items.Select(i => i.Id), Is.EqualTo(new long[] { 2, 1 }));
    }

    [Test]
    public async Task SkipLog_ExportsTheCurrentViewToExcel()
    {
        SeedSkipLog();
        var request = new ExportRequest { Search = "ORD-1", Columns = ["orderId", "factoryId", "componentErpId"] };

        var file = await new ExportSkipLogQueryHandler(catalog.Db.Object, catalog.Time)
            .Handle(new ExportSkipLogQuery(request), CancellationToken.None);

        Assert.That(file.FileName, Is.EqualTo("balancer_skip_log_20261124_100000.xlsx"));
        Assert.That(file.Content, Is.Not.Empty);
    }

    [Test]
    public async Task CountrySkuLists_AreGeneratedPerFactoryWithAliases()
    {
        catalog.AddAlias(CatalogFixture.SkuUuid, "TGN-N-4609");
        catalog.SetSkuFactory(CatalogFixture.SkuUuid, CatalogFixture.Factory125, producible: false);
        var retired = catalog.AddSku(Guid.NewGuid(), "TGI-OLD", status: SkuStatus.Retired);
        catalog.SetSkuFactory(retired.SkuUuid, CatalogFixture.Factory110);
        var dropshipper = catalog.AddSku(Guid.NewGuid(), "DS-1", SkuProductType.Dropshipper);
        catalog.SetSkuFactory(dropshipper.SkuUuid, CatalogFixture.Factory110);

        var result = await new GetCountrySkuListsQueryHandler(catalog.Db.Object)
            .Handle(new GetCountrySkuListsQuery(null), CancellationToken.None);

        Assert.That(result.Select(l => l.FactoryId), Is.EqualTo(BothFactories));
        Assert.That(result[0].CountryName, Is.EqualTo("Factory 110"));
        Assert.That(result[0].Skus, Is.EqualTo(new[]
        {
            new CountrySkuDto(CatalogFixture.SkuUuid, "TGI-N-4609", true, false),
            new CountrySkuDto(retired.SkuUuid, "TGI-OLD", true, true),
            new CountrySkuDto(CatalogFixture.SkuUuid, "TGN-N-4609", true, false)
        }));
        Assert.That(result[1].Skus, Is.EqualTo(new[]
        {
            new CountrySkuDto(CatalogFixture.SkuUuid, "TGI-N-4609", false, true),
            new CountrySkuDto(CatalogFixture.SkuUuid, "TGN-N-4609", false, true)
        }));
    }

    [Test]
    public async Task CountrySkuLists_CanBeLimitedToOneFactory()
    {
        var result = await new GetCountrySkuListsQueryHandler(catalog.Db.Object)
            .Handle(new GetCountrySkuListsQuery(CatalogFixture.Factory125), CancellationToken.None);

        Assert.That(result.Select(l => l.FactoryId), Is.EqualTo(new[] { CatalogFixture.Factory125 }));
        Assert.ThrowsAsync<EntityNotFoundException>(() => new GetCountrySkuListsQueryHandler(catalog.Db.Object)
            .Handle(new GetCountrySkuListsQuery("999"), CancellationToken.None));
    }

    private void SeedSkipLog()
    {
        catalog.SkipLogs.Add(new BalancerSkipLog
        {
            Id = 1, OrderId = "ORD-1", OrderItemId = "ITM001", FactoryId = CatalogFixture.Factory110,
            Reason = SkipReason.ComponentOos, ComponentErpId = CatalogFixture.Board, CreatedAt = CatalogFixture.Now.AddHours(-3)
        });
        catalog.SkipLogs.Add(new BalancerSkipLog
        {
            Id = 2, OrderId = "ORD-1", OrderItemId = "ITM001", FactoryId = CatalogFixture.Factory125,
            Reason = SkipReason.ComponentOos, ComponentErpId = CatalogFixture.Board, CreatedAt = CatalogFixture.Now.AddHours(-2)
        });
        catalog.SkipLogs.Add(new BalancerSkipLog
        {
            Id = 3, OrderId = "ORD-2", FactoryId = CatalogFixture.Factory110,
            Reason = SkipReason.ProcessOffline, ProcessId = CatalogFixture.Engraving, CreatedAt = CatalogFixture.Now.AddHours(-1)
        });
    }
}
