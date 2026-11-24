using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Components;

[TestFixture]
public class SapComponentsSyncServiceTests
{
    private const long RunId = 7;
    private static readonly DateTime Now = new(2026, 11, 24, 2, 0, 0, DateTimeKind.Utc);

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<ISapExportReader> exportReader;
    private Mock<ISyncRunTracker> tracker;
    private Mock<IHistoryWriter> history;
    private List<Component> components;
    private List<ComponentFactory> stockRows;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        exportReader = new Mock<ISapExportReader>();
        tracker = new Mock<ISyncRunTracker>();
        history = new Mock<IHistoryWriter>();
        tracker.Setup(t => t.Start(SyncTypes.SapComponents, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(RunId);
        db.SetupSet(x => x.Factories, new Factory { Id = "110" }, new Factory { Id = "125" });
        db.SetupSet(x => x.Skus);
        db.SetupSet(x => x.Processes);
    }

    private void Seed(Component[] existingComponents, params ComponentFactory[] existingStock)
    {
        components = db.SetupSet(x => x.Components, existingComponents);
        stockRows = db.SetupSet(x => x.ComponentFactories, existingStock);
    }

    private void Export(SapComponentRow[] componentRows, SapStockRow[] stock = null, SapExportRowError[] errors = null)
    {
        exportReader.Setup(r => r.Read(DateOnly.FromDateTime(Now), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SapExport(componentRows, stock ?? [], errors ?? []));
    }

    private static SapComponentRow Row(string erpId, string name, ComponentStatus status = ComponentStatus.Active, decimal? unitCost = null)
    {
        return new SapComponentRow(erpId, name, status, "Board", null, null, null, null, null, null, null, null, null,
            unitCost, unitCost == null ? null : "USD");
    }

    private Task Run()
    {
        return new SapComponentsSyncService(db.Object, exportReader.Object, tracker.Object, history.Object,
            new SkuMappingValidator(db.Object, history.Object), TestDb.FixedTime(Now), NullLogger<SapComponentsSyncService>.Instance).Run("schedule");
    }

    private void VerifyHistory(string erpId, string field, object oldValue, object newValue, Times times)
    {
        history.Verify(h => h.Record(HistoryEntityTypes.Component, erpId, field, oldValue, newValue,
            HistorySource.Sync, null, SapComponentsSyncService.ChangedBy), times);
    }

    [Test]
    public async Task Run_AddsNewUpdatesChangedAndMarksMissingInactive()
    {
        var changed = new Component { ErpId = "A", Name = "Old name", Family = "Board", Status = ComponentStatus.Active, CriticalStockLevel = 3 };
        var same = new Component { ErpId = "B", Name = "Beta", Family = "Board", Status = ComponentStatus.Active };
        var missing = new Component { ErpId = "C", Name = "Gone", Status = ComponentStatus.Active };
        var alreadyInactive = new Component { ErpId = "D", Name = "Old", Status = ComponentStatus.Inactive };
        Seed([changed, same, missing, alreadyInactive]);
        Export([Row("A", "New name"), Row("B", "Beta"), Row("N", "New one", unitCost: 2.5m)]);

        await Run();

        Assert.That(components, Has.Count.EqualTo(5));
        var added = components.Single(c => c.ErpId == "N");
        Assert.That(added.Name, Is.EqualTo("New one"));
        Assert.That(added.UnitCostAmount, Is.EqualTo(2.5m));
        Assert.That(added.UnitCostSource, Is.EqualTo(SapComponentsSyncService.UnitCostSource));
        Assert.That(added.LastSapSyncAt, Is.EqualTo(Now));
        Assert.That(changed.Name, Is.EqualTo("New name"));
        Assert.That(changed.CriticalStockLevel, Is.EqualTo(3));
        Assert.That(changed.UpdatedAt, Is.EqualTo(Now));
        Assert.That(same.UpdatedAt, Is.EqualTo(default(DateTime)));
        Assert.That(same.LastSapSyncAt, Is.EqualTo(Now));
        Assert.That(missing.Status, Is.EqualTo(ComponentStatus.Inactive));
        Assert.That(missing.LastSapSyncAt, Is.Null);

        VerifyHistory("A", "name", "Old name", "New name", Times.Once());
        VerifyHistory("C", "status", ComponentStatus.Active, ComponentStatus.Inactive, Times.Once());
        VerifyHistory("N", "status", null, ComponentStatus.Active, Times.Once());
        VerifyHistory("D", "status", It.IsAny<object>(), It.IsAny<object>(), Times.Never());
        tracker.Verify(t => t.Complete(RunId, 1, 2, 0, It.IsAny<CancellationToken>()), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Run_RevalidatesSkus_SoASkuUsingANewlyInactiveComponentIsFlagged()
    {
        Seed([
            new Component { ErpId = "A", Name = "Alpha", Family = "Board", Status = ComponentStatus.Active },
            new Component { ErpId = "B", Name = "Beta", Family = "Board", Status = ComponentStatus.Active }
        ]);
        var sku = new Sku
        {
            SkuUuid = Guid.NewGuid(),
            ProductType = SkuProductType.Manufactured,
            Components = [new SkuComponent { ComponentErpId = "B", Quantity = 1 }]
        };
        db.SetupSet(x => x.Skus, sku);
        Export([Row("A", "Alpha")]);

        await Run();

        Assert.That(sku.ValidationStatus, Is.EqualTo(SkuValidationStatus.Invalid));
        Assert.That(sku.ValidationIssues, Is.EqualTo(new[] { "Component B is not active in SAP" }));
        history.Verify(h => h.Record(HistoryEntityTypes.Sku, sku.SkuUuid.ToString(), SkuMappingValidator.StatusField,
            SkuValidationStatus.Valid, SkuValidationStatus.Invalid, HistorySource.Sync, null, SapComponentsSyncService.ChangedBy));
        tracker.Verify(t => t.Complete(RunId, 0, 1, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Run_ReplacesStockSnapshot_AndKeepsManualOosAndOverride()
    {
        var flagged = new ComponentFactory
        {
            ComponentErpId = "A", FactoryId = "110", WarehouseStock = 1, ProductionFloorStock = 1,
            ManualOosActive = true, ManualOosSetBy = "fm", ManualOosNote = "note", CriticalStockLevelOverride = 9
        };
        var notExported = new ComponentFactory { ComponentErpId = "A", FactoryId = "125", WarehouseStock = 4, ProductionFloorStock = 2 };
        Seed([new Component { ErpId = "A", Name = "Alpha", Family = "Board" }, new Component { ErpId = "B", Name = "Beta", Family = "Board" }],
            flagged, notExported);
        Export([Row("A", "Alpha"), Row("B", "Beta")],
        [
            new SapStockRow("A", "110", SapStockLocation.Warehouse, 20),
            new SapStockRow("A", "110", SapStockLocation.Warehouse, 5),
            new SapStockRow("B", "125", SapStockLocation.ProductionFloor, 3)
        ]);

        await Run();

        Assert.That(flagged.WarehouseStock, Is.EqualTo(25));
        Assert.That(flagged.ProductionFloorStock, Is.EqualTo(0));
        Assert.That(flagged.StockSyncedAt, Is.EqualTo(Now));
        Assert.That(flagged.ManualOosActive, Is.True);
        Assert.That(flagged.ManualOosSetBy, Is.EqualTo("fm"));
        Assert.That(flagged.ManualOosNote, Is.EqualTo("note"));
        Assert.That(flagged.CriticalStockLevelOverride, Is.EqualTo(9));
        Assert.That(notExported.WarehouseStock, Is.EqualTo(0));
        Assert.That(notExported.ProductionFloorStock, Is.EqualTo(0));
        Assert.That(stockRows, Has.Count.EqualTo(3));
        var created = stockRows.Single(s => s.ComponentErpId == "B");
        Assert.That(created.FactoryId, Is.EqualTo("125"));
        Assert.That(created.ProductionFloorStock, Is.EqualTo(3));
        tracker.Verify(t => t.Complete(RunId, 0, 2, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Run_RowErrorsAreCountedAsFailedWithoutAbortingTheRun()
    {
        Seed([]);
        Export([Row("A", "Alpha")],
            [
                new SapStockRow("A", "110", SapStockLocation.Warehouse, 1),
                new SapStockRow("UNKNOWN", "110", SapStockLocation.Warehouse, 1),
                new SapStockRow("A", "999", SapStockLocation.Warehouse, 1)
            ],
            [new SapExportRowError("components.csv", 3, "name is required")]);

        await Run();

        Assert.That(stockRows, Has.Count.EqualTo(1));
        tracker.Verify(t => t.Complete(RunId, 1, 0, 3, It.IsAny<CancellationToken>()), Times.Once);
        tracker.Verify(t => t.Fail(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Run_NoValidComponentRows_FailsTheRunAndChangesNothing()
    {
        var existing = new Component { ErpId = "A", Name = "Alpha", Status = ComponentStatus.Active };
        Seed([existing]);
        Export([]);

        Assert.ThrowsAsync<InvalidDataException>(Run);

        Assert.That(existing.Status, Is.EqualTo(ComponentStatus.Active));
        tracker.Verify(t => t.Fail(RunId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Run_AlreadyRunning_ConflictAndNoRead()
    {
        tracker.Setup(t => t.Start(SyncTypes.SapComponents, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("running"));

        Assert.ThrowsAsync<ConflictException>(Run);

        exportReader.Verify(r => r.Read(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ScheduledJob_SkipsWhenASyncIsAlreadyRunning()
    {
        var service = new Mock<ISapComponentsSyncService>();
        service.Setup(s => s.Run(SapComponentsSyncJob.TriggeredBy, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("running"));

        await new SapComponentsSyncJob(service.Object, NullLogger<SapComponentsSyncJob>.Instance).Run(CancellationToken.None);

        service.Verify(s => s.Run(SapComponentsSyncJob.TriggeredBy, It.IsAny<CancellationToken>()), Times.Once);
    }
}
