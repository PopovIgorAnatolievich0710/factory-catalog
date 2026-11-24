using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class GetComponentQueryHandlerTests
{
    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ICurrentUser> currentUser;
    private Sku manufactured;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        currentUser = new Mock<ICurrentUser>();
        manufactured = new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "110-01", Name = "Ring", ProductType = SkuProductType.Manufactured };
        var dropshipper = new Sku { SkuUuid = Guid.NewGuid(), SiteSku = "110-02", Name = "Box", ProductType = SkuProductType.Dropshipper };

        db.SetupSet(x => x.Components,
            new Component { ErpId = "A", Name = "Alpha", Family = "Board", MeasuredBy = "thickness", MeasureValue = 0.8m, MeasureUnit = "mm", CriticalStockLevel = 2, UnitCostAmount = 245.5m, UnitCostCurrency = "USD", UnitCostSource = "SAP" },
            new Component { ErpId = "B", Name = "Beta", CriticalStockLevel = 4 });
        db.SetupSet(x => x.ComponentFactories,
            new ComponentFactory { ComponentErpId = "A", FactoryId = "110", WarehouseStock = 12, ProductionFloorStock = 1 },
            new ComponentFactory { ComponentErpId = "A", FactoryId = "125", WarehouseStock = 6, CriticalStockLevelOverride = 5, ManualOosActive = true, ManualOosSetBy = "fm.125", ManualOosNote = "Supply scheduled" },
            new ComponentFactory { ComponentErpId = "B", FactoryId = "110", WarehouseStock = 3 });
        db.SetupSet(x => x.ComponentAlternatives,
            new ComponentAlternative { ComponentErpId = "A", AlternativeErpId = "B", ForBalancer = true, ForPlanner = false });
        db.SetupSet(x => x.Skus, manufactured, dropshipper);
        db.SetupSet(x => x.SkuComponents,
            new SkuComponent { SkuUuid = manufactured.SkuUuid, ComponentErpId = "A", Quantity = 2, Usage = ComponentUsage.Dynamic },
            new SkuComponent { SkuUuid = dropshipper.SkuUuid, ComponentErpId = "A", Quantity = 1 });
    }

    private Task<ComponentDetailsDto> Get(string erpId)
    {
        return new GetComponentQueryHandler(db.Object, currentUser.Object).Handle(new GetComponentQuery(erpId), CancellationToken.None);
    }

    [Test]
    public async Task ReturnsEveryTabsData()
    {
        currentUser.Setup(u => u.HasPermission(Permission.ComponentsViewCost, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await Get("A");

        Assert.That(result.Measure, Is.EqualTo(new ComponentMeasureDto("thickness", 0.8m, "mm")));
        Assert.That(result.UnitCost, Is.EqualTo(new ComponentUnitCostDto(245.5m, "USD", "SAP")));
        Assert.That(result.StockStatus, Is.EqualTo(StockAlertStatus.OosConflict));
        Assert.That(result.StockStatusFactoryIds, Is.EqualTo(new[] { "125" }));
        Assert.That(result.ActionRequired, Is.True);

        var flagged = result.Factories.Single(f => f.FactoryId == "125");
        Assert.That(flagged.TotalStock, Is.EqualTo(6));
        Assert.That(flagged.CriticalLevel, Is.EqualTo(5));
        Assert.That(flagged.CriticalLevelOverride, Is.EqualTo(5));
        Assert.That(flagged.StockStatus, Is.EqualTo(StockStatus.OutOfStock));
        Assert.That(flagged.OosConflict, Is.True);
        Assert.That(flagged.ManualOos.SetBy, Is.EqualTo("fm.125"));
        Assert.That(result.Factories.Single(f => f.FactoryId == "110").Status, Is.EqualTo(StockAlertStatus.Ok));

        var alternative = result.Alternatives.Single();
        Assert.That(alternative.ErpId, Is.EqualTo("B"));
        Assert.That(alternative.ForBalancer, Is.True);
        Assert.That(alternative.ForPlanner, Is.False);
        Assert.That(alternative.Factories.Single(), Is.EqualTo(new ComponentAlternativeStockDto("110", 3, StockAlertStatus.Low)));

        Assert.That(result.UsedInSkus.Single(),
            Is.EqualTo(new ComponentSkuUsageDto(manufactured.SkuUuid, "Ring", "110-01", 2, ComponentUsage.Dynamic)));
    }

    [Test]
    public async Task UnitCostHiddenWithoutPermission()
    {
        var result = await Get("A");

        Assert.That(result.UnitCost, Is.Null);
    }

    [Test]
    public void UnknownErpId_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => Get("MISSING"));
    }
}
