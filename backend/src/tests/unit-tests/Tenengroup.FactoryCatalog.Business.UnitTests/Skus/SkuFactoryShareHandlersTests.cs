using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class SkuFactoryShareHandlersTests
{
    private const string Email = "fm.110@tenengroup.com";
    private static readonly DateTime Now = new(2026, 11, 24, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Uuid = Guid.NewGuid();

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;
    private Mock<ICurrentUser> currentUser;
    private Sku sku;
    private List<GlobalFactoryShare> globalShares;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.Email).Returns(Email);
        currentUser.Setup(u => u.EnsureCanEditFactory("125", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("not your factory"));

        sku = new Sku { SkuUuid = Uuid, SiteSku = "SITE", ProductType = SkuProductType.Manufactured };
        db.SetupSet(x => x.Skus, sku);
        db.SetupSet(x => x.Factories,
            new Factory { Id = "110", IsActive = true },
            new Factory { Id = "125", IsActive = true },
            new Factory { Id = "999", IsActive = false });
        globalShares = db.SetupSet(x => x.GlobalFactoryShares,
            new GlobalFactoryShare { FactoryId = "110", Pct = 60, UpdatedBy = "admin", UpdatedAt = Now.AddDays(-1) },
            new GlobalFactoryShare { FactoryId = "125", Pct = 40, UpdatedBy = "admin", UpdatedAt = Now.AddDays(-1) });
    }

    private SkuFactoryShareService Service() => new(db.Object, history.Object, currentUser.Object, TestDb.FixedTime(Now));

    private void MakeProducible(params string[] factoryIds)
    {
        sku.Factories.AddRange(factoryIds.Select(id => new SkuFactory { SkuUuid = Uuid, FactoryId = id, Producible = true }));
    }

    [Test]
    public async Task SetProducible_OwnFactory_CreatesRowAndRecordsHistory()
    {
        var result = await new SetSkuProducibleCommandHandler(db.Object, Service())
            .Handle(new SetSkuProducibleCommand(Uuid, "110", true), CancellationToken.None);

        var row = sku.Factories.Single();
        Assert.That(row.Producible, Is.True);
        Assert.That(row.UpdatedAt, Is.EqualTo(Now));
        Assert.That(result.Factories.Select(f => (f.FactoryId, f.Producible, f.EffectiveSharePct, f.Source)), Is.EqualTo(new[]
        {
            ("110", true, 100m, SkuShareSource.Default),
            ("125", false, 0m, SkuShareSource.NotProducible)
        }));
        Assert.That(result.ShareTotalPct, Is.EqualTo(100m));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), SkuFactoryShareService.ProducibleField, false, true, HistorySource.User, "110", null));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void SetProducible_OtherFactory_Forbidden()
    {
        Assert.ThrowsAsync<ForbiddenException>(() => new SetSkuProducibleCommandHandler(db.Object, Service())
            .Handle(new SetSkuProducibleCommand(Uuid, "125", true), CancellationToken.None));

        Assert.That(sku.Factories, Is.Empty);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void SetProducible_InactiveFactory_NotFound()
    {
        Assert.ThrowsAsync<EntityNotFoundException>(() => new SetSkuProducibleCommandHandler(db.Object, Service())
            .Handle(new SetSkuProducibleCommand(Uuid, "999", true), CancellationToken.None));
    }

    [Test]
    public void SetProducible_DropshipperSku_Rejected()
    {
        sku.ProductType = SkuProductType.Dropshipper;

        Assert.ThrowsAsync<ValidationException>(() => new SetSkuProducibleCommandHandler(db.Object, Service())
            .Handle(new SetSkuProducibleCommand(Uuid, "110", true), CancellationToken.None));
    }

    [Test]
    public async Task BulkSetProducible_OwnFactory_CountsOnlyChangedManufacturedSkus()
    {
        MakeProducible("110");
        var second = new Sku { SkuUuid = Guid.NewGuid(), ProductType = SkuProductType.Manufactured };
        var dropshipper = new Sku { SkuUuid = Guid.NewGuid(), ProductType = SkuProductType.Dropshipper };
        db.SetupSet(x => x.Skus, sku, second, dropshipper);

        var result = await new BulkSetSkuProducibleCommandHandler(db.Object, Service()).Handle(
            new BulkSetSkuProducibleCommand("110", [Uuid, second.SkuUuid, dropshipper.SkuUuid], true), CancellationToken.None);

        Assert.That(result.Updated, Is.EqualTo(1));
        Assert.That(second.Factories.Single().Producible, Is.True);
        Assert.That(dropshipper.Factories, Is.Empty);
    }

    [Test]
    public void BulkSetProducible_OtherFactory_Forbidden()
    {
        Assert.ThrowsAsync<ForbiddenException>(() => new BulkSetSkuProducibleCommandHandler(db.Object, Service()).Handle(
            new BulkSetSkuProducibleCommand("125", [Uuid], true), CancellationToken.None));
    }

    [Test]
    public async Task SetShareOverride_OwnFactory_OthersRebalancedAndTotalIs100()
    {
        MakeProducible("110", "125");

        var result = await new SetSkuShareOverrideCommandHandler(db.Object, Service())
            .Handle(new SetSkuShareOverrideCommand(Uuid, "110", 70), CancellationToken.None);

        Assert.That(result.Factories.Select(f => (f.FactoryId, f.EffectiveSharePct, f.Source)), Is.EqualTo(new[]
        {
            ("110", 70m, SkuShareSource.Override),
            ("125", 30m, SkuShareSource.Rebalanced)
        }));
        Assert.That(result.ShareTotalPct, Is.EqualTo(100m));
        Assert.That(sku.Factories[0].OverrideBy, Is.EqualTo(Email));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), SkuFactoryShareService.OverrideField, null, 70m, HistorySource.User, "110", null));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), SkuFactoryShareService.OverrideField, null, 30m, HistorySource.System, "125", null));
    }

    [Test]
    public void SetShareOverride_OtherFactory_Forbidden()
    {
        MakeProducible("110", "125");

        Assert.ThrowsAsync<ForbiddenException>(() => new SetSkuShareOverrideCommandHandler(db.Object, Service())
            .Handle(new SetSkuShareOverrideCommand(Uuid, "125", 70), CancellationToken.None));

        Assert.That(sku.Factories.Select(f => f.OverrideSharePct), Is.All.Null);
    }

    [Test]
    public void SetShareOverride_NotProducible_ValidationError()
    {
        MakeProducible("125");

        Assert.ThrowsAsync<ValidationException>(() => new SetSkuShareOverrideCommandHandler(db.Object, Service())
            .Handle(new SetSkuShareOverrideCommand(Uuid, "110", 70), CancellationToken.None));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ResetShareOverride_ReturnsToDefaultShares()
    {
        MakeProducible("110", "125");
        await new SetSkuShareOverrideCommandHandler(db.Object, Service())
            .Handle(new SetSkuShareOverrideCommand(Uuid, "110", 70), CancellationToken.None);

        var result = await new ResetSkuShareOverrideCommandHandler(db.Object, Service())
            .Handle(new ResetSkuShareOverrideCommand(Uuid, "110"), CancellationToken.None);

        Assert.That(result.Factories.Select(f => (f.EffectiveSharePct, f.Source)), Is.EqualTo(new[]
        {
            (60m, SkuShareSource.Default),
            (40m, SkuShareSource.Default)
        }));
    }

    [Test]
    public async Task UpdateGlobalShare_Total100_SavedWithHistory()
    {
        var handler = new UpdateGlobalFactoryShareCommandHandler(
            db.Object, Service(), history.Object, currentUser.Object, TestDb.FixedTime(Now));

        var result = await handler.Handle(
            new UpdateGlobalFactoryShareCommand([new FactorySharePctDto("110", 55), new FactorySharePctDto("125", 45)]),
            CancellationToken.None);

        Assert.That(globalShares.Select(s => s.Pct), Is.EqualTo(new[] { 55m, 45m }));
        Assert.That(globalShares.Select(s => s.UpdatedBy), Is.All.EqualTo(Email));
        Assert.That(result.TotalPct, Is.EqualTo(100m));
        history.Verify(h => h.Record(
            HistoryEntityTypes.GlobalFactoryShare, UpdateGlobalFactoryShareCommandHandler.HistoryEntityId,
            UpdateGlobalFactoryShareCommandHandler.PctField, 60m, 55m, HistorySource.User, "110", null));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateGlobalShare_TotalNot100OrFactoriesWrong_Rejected()
    {
        var handler = new UpdateGlobalFactoryShareCommandHandler(
            db.Object, Service(), history.Object, currentUser.Object, TestDb.FixedTime(Now));

        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateGlobalFactoryShareCommand([new FactorySharePctDto("110", 55), new FactorySharePctDto("125", 40)]),
            CancellationToken.None));
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateGlobalFactoryShareCommand([new FactorySharePctDto("110", 100)]), CancellationToken.None));
        Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new UpdateGlobalFactoryShareCommand([new FactorySharePctDto("110", 60), new FactorySharePctDto("999", 40)]),
            CancellationToken.None));

        Assert.That(globalShares.Select(s => s.Pct), Is.EqualTo(new[] { 60m, 40m }));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetGlobalShare_ListsEveryActiveFactory()
    {
        globalShares.RemoveAt(1);

        var result = await new GetGlobalFactoryShareQueryHandler(db.Object, Service())
            .Handle(new GetGlobalFactoryShareQuery(), CancellationToken.None);

        Assert.That(result.FactoryShares, Is.EqualTo(new[] { new FactorySharePctDto("110", 60), new FactorySharePctDto("125", 0) }));
        Assert.That(result.TotalPct, Is.EqualTo(60m));
    }
}
