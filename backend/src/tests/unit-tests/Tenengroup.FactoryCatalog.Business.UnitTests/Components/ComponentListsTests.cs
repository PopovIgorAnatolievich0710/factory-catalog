using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.Business.Components.StockLevels;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class ComponentListsTests
{
    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.HasPermission(Permission.ComponentsViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var manufactured = new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "S-1", Name = "Ring", ProductType = SkuProductType.Manufactured };
        var dropshipper = new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "S-2", Name = "Box", ProductType = SkuProductType.Dropshipper };

        db.SetupSet(x => x.Factories, new Factory { Id = "110" }, new Factory { Id = "125" });
        db.SetupSet(x => x.Components,
            new Component { ErpId = "OK-1", Name = "Healthy chain", Family = "Chain", Status = ComponentStatus.Active, CriticalStockLevel = 5, UnitCostAmount = 3, UnitCostCurrency = "USD" },
            new Component { ErpId = "LOW-1", Name = "Low clasp", Family = "Clasp", Status = ComponentStatus.Active, CriticalStockLevel = 5 },
            new Component { ErpId = "OOS-1", Name = "Empty board", Family = "Board", Status = ComponentStatus.Active, CriticalStockLevel = 5 },
            new Component { ErpId = "CONF-1", Name = "Flagged board", Family = "Board", Status = ComponentStatus.Active, CriticalStockLevel = 5 },
            new Component { ErpId = "OLD-1", Name = "Retired chain", Family = "Chain", Status = ComponentStatus.Inactive, CriticalStockLevel = 5 });
        db.SetupSet(x => x.ComponentFactories,
            new ComponentFactory { ComponentErpId = "OK-1", FactoryId = "110", WarehouseStock = 10 },
            new ComponentFactory { ComponentErpId = "OK-1", FactoryId = "125", WarehouseStock = 8, ProductionFloorStock = 2 },
            new ComponentFactory { ComponentErpId = "LOW-1", FactoryId = "110", WarehouseStock = 10 },
            new ComponentFactory { ComponentErpId = "LOW-1", FactoryId = "125", WarehouseStock = 5, CriticalStockLevelOverride = 6 },
            new ComponentFactory { ComponentErpId = "OOS-1", FactoryId = "110", WarehouseStock = 0 },
            new ComponentFactory { ComponentErpId = "OOS-1", FactoryId = "125", WarehouseStock = 1 },
            new ComponentFactory { ComponentErpId = "CONF-1", FactoryId = "125", WarehouseStock = 9, ManualOosActive = true, ManualOosSetBy = "fm", ManualOosNote = "note" },
            new ComponentFactory { ComponentErpId = "OLD-1", FactoryId = "110", WarehouseStock = 0 });
        db.SetupSet(x => x.Skus, manufactured, dropshipper);
        db.SetupSet(x => x.SkuComponents,
            new SkuComponent { SkuUuid = manufactured.SkuUuid, ComponentErpId = "OK-1", Quantity = 1 },
            new SkuComponent { SkuUuid = dropshipper.SkuUuid, ComponentErpId = "OK-1", Quantity = 1 });
    }

    private Task<List<ComponentListItemDto>> ReadComponents(ListRequest request)
    {
        return new ComponentListReader(db.Object, currentUser.Object).Read(request, CancellationToken.None);
    }

    private Task<List<ComponentStockLevelDto>> ReadStockLevels(ListRequest request)
    {
        return new ComponentStockLevelReader(db.Object).Read(request, CancellationToken.None);
    }

    private static ListRequest Filter(string field, ListFilter filter)
    {
        return new ListRequest { Filters = new Dictionary<string, ListFilter> { [field] = filter } };
    }

    [Test]
    public async Task Components_WorstStatusFactoriesAndActionRequired()
    {
        var rows = (await ReadComponents(new ListRequest())).ToDictionary(r => r.ErpId);

        Assert.That(rows.Keys, Is.EqualTo(new[] { "CONF-1", "LOW-1", "OK-1", "OLD-1", "OOS-1" }));
        Assert.That(rows["OK-1"].Status, Is.EqualTo(StockAlertStatus.Ok));
        Assert.That(rows["OK-1"].StatusFactoryIds, Is.Empty);
        Assert.That(rows["OK-1"].TotalStock, Is.EqualTo(20));
        Assert.That(rows["OK-1"].SkusUsing, Is.EqualTo(1));
        Assert.That(rows["OK-1"].StockByFactory["125"], Is.EqualTo(10));
        Assert.That(rows["OK-1"].UnitCost, Is.EqualTo(3));
        Assert.That(rows["LOW-1"].Status, Is.EqualTo(StockAlertStatus.Low));
        Assert.That(rows["LOW-1"].StatusFactoryIds, Is.EqualTo(new[] { "125" }));
        Assert.That(rows["LOW-1"].HasCriticalLevelOverrides, Is.True);
        Assert.That(rows["LOW-1"].ActionRequired, Is.False);
        Assert.That(rows["OOS-1"].Status, Is.EqualTo(StockAlertStatus.OutOfStock));
        Assert.That(rows["OOS-1"].StatusFactoryIds, Is.EqualTo(new[] { "110" }));
        Assert.That(rows["OOS-1"].ActionRequired, Is.True);
        Assert.That(rows["CONF-1"].Status, Is.EqualTo(StockAlertStatus.OosConflict));
        Assert.That(rows["CONF-1"].ActionRequired, Is.True);
        Assert.That(rows["OLD-1"].Status, Is.EqualTo(StockAlertStatus.OutOfStock));
        Assert.That(rows["OLD-1"].ActionRequired, Is.False);
    }

    [Test]
    public async Task Components_SearchFilterAndSortOnDerivedColumns()
    {
        var bySearch = await ReadComponents(new ListRequest { Search = "board" });
        var byStatus = await ReadComponents(Filter("status", new ListFilter { Values = ["OosConflict", "Low"] }));
        var byFactoryStock = await ReadComponents(Filter("stock_125", new ListFilter { Operator = "gte", Value = "9" }));
        var sorted = await ReadComponents(new ListRequest { SortBy = "totalStock", SortDirection = "desc" });

        Assert.That(bySearch.Select(r => r.ErpId), Is.EqualTo(new[] { "CONF-1", "OOS-1" }));
        Assert.That(byStatus.Select(r => r.ErpId), Is.EqualTo(new[] { "CONF-1", "LOW-1" }));
        Assert.That(byFactoryStock.Select(r => r.ErpId), Is.EqualTo(new[] { "CONF-1", "OK-1" }));
        Assert.That(sorted.First().ErpId, Is.EqualTo("OK-1"));
        Assert.That(sorted.Last().ErpId, Is.EqualTo("OLD-1"));
    }

    [Test]
    public async Task Components_UnitCostHiddenWithoutPermission_AndPaged()
    {
        currentUser.Setup(u => u.HasPermission(Permission.ComponentsViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await new SearchComponentsQueryHandler(new ComponentListReader(db.Object, currentUser.Object))
            .Handle(new SearchComponentsQuery(new ListRequest { Page = 2, PageSize = 2 }), CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(5));
        Assert.That(result.Items.Select(r => r.ErpId), Is.EqualTo(new[] { "OK-1", "OLD-1" }));
        Assert.That(result.Items.First().UnitCost, Is.Null);
        Assert.That(result.Items.First().UnitCostCurrency, Is.Null);
    }

    [Test]
    public async Task StockLevels_OneRowPerActiveComponentAndFactoryThatIsNotOk()
    {
        var rows = await ReadStockLevels(new ListRequest());

        Assert.That(rows.Select(r => (r.ErpId, r.FactoryId, r.Status)), Is.EqualTo(new[]
        {
            ("CONF-1", "125", StockAlertStatus.OosConflict),
            ("LOW-1", "125", StockAlertStatus.Low),
            ("OOS-1", "110", StockAlertStatus.OutOfStock),
            ("OOS-1", "125", StockAlertStatus.Low)
        }));
        var low = rows.Single(r => r.ErpId == "LOW-1");
        Assert.That(low.CriticalLevel, Is.EqualTo(6));
        Assert.That(low.IsCriticalLevelOverride, Is.True);
        Assert.That(low.TotalStock, Is.EqualTo(5));
        Assert.That(rows[0].ManualOosSetBy, Is.EqualTo("fm"));
    }

    [Test]
    public async Task StockLevels_FilterByFactoryAndStatus_SortByStatusDescending()
    {
        var byFactory = await ReadStockLevels(Filter("factoryId", new ListFilter { Values = ["110"] }));
        var byStatus = await ReadStockLevels(Filter("status", new ListFilter { Values = ["Low"] }));
        var sorted = await ReadStockLevels(new ListRequest { SortBy = "status", SortDirection = "desc" });

        Assert.That(byFactory.Select(r => r.ErpId), Is.EqualTo(new[] { "OOS-1" }));
        Assert.That(byStatus.Select(r => r.ErpId), Is.EqualTo(new[] { "LOW-1", "OOS-1" }));
        Assert.That(sorted.Select(r => r.Status), Is.EqualTo(new[]
        {
            StockAlertStatus.OosConflict, StockAlertStatus.OutOfStock, StockAlertStatus.Low, StockAlertStatus.Low
        }));
    }
}
