using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Sync;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class MasterCatalogSkuImporterTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 3, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Uuid = Guid.Parse("8f3c1a2e-5b7d-4c1a-9e21-7d4e5f6a8b90");

    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        db.SetupSet(x => x.SkuAliases);
    }

    private MasterCatalogSkuImporter CreateImporter()
    {
        return new MasterCatalogSkuImporter(
            db.Object, history.Object, TestDb.FixedTime(Now), NullLogger<MasterCatalogSkuImporter>.Instance);
    }

    private static MasterCatalogSku Manufactured(
        Guid? uuid = null,
        string status = "active",
        string name = "Name necklace",
        MasterCatalogSkuAlias[] aliases = null,
        MasterCatalogSkuComponent[] components = null)
    {
        return new MasterCatalogSku(
            uuid ?? Uuid,
            "TGI-N-4609",
            name,
            "manufactured",
            status,
            aliases ?? [new MasterCatalogSkuAlias("TGN-N-4609", "factory", true)],
            components ?? [new MasterCatalogSkuComponent("R.VM.BRD", 1, "static"), new MasterCatalogSkuComponent("CHN.VM", 2, "dynamic")],
            null);
    }

    private static Sku ExistingSku()
    {
        return new Sku
        {
            SkuUuid = Uuid,
            SiteSku = "TGI-N-4609",
            Name = "Name necklace",
            ProductType = SkuProductType.Manufactured,
            Status = SkuStatus.Active,
            Aliases = [new SkuAlias { SkuUuid = Uuid, Alias = "TGN-N-4609", Type = SkuAliasType.Factory, Deprecated = true }],
            Components =
            [
                new SkuComponent { SkuUuid = Uuid, ComponentErpId = "R.VM.BRD", Quantity = 1, Usage = ComponentUsage.Static },
                new SkuComponent { SkuUuid = Uuid, ComponentErpId = "CHN.VM", Quantity = 2, Usage = ComponentUsage.Dynamic }
            ],
            Processes = [new SkuProcess { SkuUuid = Uuid, ProcessId = "CUTTING", Quantity = 4 }],
            Factories = [new SkuFactory { SkuUuid = Uuid, FactoryId = "110", Producible = true }]
        };
    }

    [Test]
    public async Task Import_NewSku_AddedByUuidWithAliasesAndComponents()
    {
        var skus = db.SetupSet(x => x.Skus);

        var result = await CreateImporter().Import([Manufactured()], CancellationToken.None);

        Assert.That(result, Is.EqualTo(new MasterCatalogImportResult(1, 0, 0)));
        var sku = skus.Single();
        Assert.That(sku.SkuUuid, Is.EqualTo(Uuid));
        Assert.That(sku.SiteSku, Is.EqualTo("TGI-N-4609"));
        Assert.That(sku.ProductType, Is.EqualTo(SkuProductType.Manufactured));
        Assert.That(sku.Status, Is.EqualTo(SkuStatus.Active));
        Assert.That(sku.LastMasterSyncAt, Is.EqualTo(Now));
        Assert.That(sku.CreatedAt, Is.EqualTo(Now));
        Assert.That(sku.Aliases.Single().Alias, Is.EqualTo("TGN-N-4609"));
        Assert.That(sku.Aliases.Single().Deprecated, Is.True);
        Assert.That(sku.Components.Select(c => (c.ComponentErpId, c.Quantity, c.Usage)), Is.EqualTo(new[]
        {
            ("R.VM.BRD", 1m, ComponentUsage.Static),
            ("CHN.VM", 2m, ComponentUsage.Dynamic)
        }));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "created", null, "TGI-N-4609", HistorySource.Sync, null,
            MasterCatalogSkuImporter.ChangedBy), Times.Once);
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Import_UnchangedSku_NotCountedButSyncTimeRefreshed()
    {
        var existing = ExistingSku();
        db.SetupSet(x => x.Skus, existing);

        var result = await CreateImporter().Import([Manufactured()], CancellationToken.None);

        Assert.That(result, Is.EqualTo(new MasterCatalogImportResult(0, 0, 0)));
        Assert.That(existing.LastMasterSyncAt, Is.EqualTo(Now));
        history.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Import_ChangedSku_UpdatesFieldsMappingsAndWritesSyncHistory()
    {
        var existing = ExistingSku();
        db.SetupSet(x => x.Skus, existing);

        var result = await CreateImporter().Import(
            [Manufactured(name: "Renamed", aliases: [], components: [new MasterCatalogSkuComponent("R.VM.BRD", 3, "dynamic")])],
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(new MasterCatalogImportResult(0, 1, 0)));
        Assert.That(existing.Name, Is.EqualTo("Renamed"));
        Assert.That(existing.UpdatedAt, Is.EqualTo(Now));
        Assert.That(existing.Aliases, Is.Empty);
        Assert.That(existing.Components.Single().Quantity, Is.EqualTo(3m));
        Assert.That(existing.Components.Single().Usage, Is.EqualTo(ComponentUsage.Dynamic));
        Assert.That(existing.Processes, Has.Count.EqualTo(1));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "name", "Name necklace", "Renamed", HistorySource.Sync, null,
            MasterCatalogSkuImporter.ChangedBy));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "component:R.VM.BRD", "1 Static", "3 Dynamic", HistorySource.Sync, null,
            MasterCatalogSkuImporter.ChangedBy));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, Uuid.ToString(), "component:CHN.VM", "2 Dynamic", null, HistorySource.Sync, null,
            MasterCatalogSkuImporter.ChangedBy));
    }

    [Test]
    public async Task Import_RetiredSku_MarkedRetiredNeverDeleted()
    {
        var existing = ExistingSku();
        var skus = db.SetupSet(x => x.Skus, existing);

        var result = await CreateImporter().Import([Manufactured(status: "retired")], CancellationToken.None);

        Assert.That(result.Updated, Is.EqualTo(1));
        Assert.That(skus, Has.Count.EqualTo(1));
        Assert.That(existing.Status, Is.EqualTo(SkuStatus.Retired));
        Assert.That(existing.Components, Has.Count.EqualTo(2));
        Assert.That(existing.Factories, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Import_Dropshipper_LimitedRecordWithoutComponentsProcessesOrShare()
    {
        var existing = ExistingSku();
        db.SetupSet(x => x.Skus, existing);
        var item = new MasterCatalogSku(
            Uuid, "TGI-N-4609", "Name necklace", "dropshipper", "active", [],
            [new MasterCatalogSkuComponent("R.VM.BRD", 1, "static")],
            new MasterCatalogDropshipper("DS-7", "Acme Drop", new MasterCatalogMoney(14.5m, "USD")));

        var result = await CreateImporter().Import([item], CancellationToken.None);

        Assert.That(result.Updated, Is.EqualTo(1));
        Assert.That(existing.ProductType, Is.EqualTo(SkuProductType.Dropshipper));
        Assert.That(existing.DropshipperId, Is.EqualTo("DS-7"));
        Assert.That(existing.DropshipperName, Is.EqualTo("Acme Drop"));
        Assert.That(existing.DropshipperTotalCostAmount, Is.EqualTo(14.5m));
        Assert.That(existing.DropshipperTotalCostCurrency, Is.EqualTo("USD"));
        Assert.That(existing.Components, Is.Empty);
        Assert.That(existing.Processes, Is.Empty);
        Assert.That(existing.Factories, Is.Empty);
    }

    [Test]
    public async Task Import_DropshipperWithoutTotalCost_Failed()
    {
        var skus = db.SetupSet(x => x.Skus);
        var item = new MasterCatalogSku(
            Uuid, "TGI-N-4609", "Name", "dropshipper", "active", null, null, new MasterCatalogDropshipper("DS-7", "Acme", null));

        var result = await CreateImporter().Import([item], CancellationToken.None);

        Assert.That(result, Is.EqualTo(new MasterCatalogImportResult(0, 0, 1)));
        Assert.That(skus, Is.Empty);
    }

    [Test]
    public async Task Import_InvalidItems_CountedAsFailedAndOthersStillImported()
    {
        var skus = db.SetupSet(x => x.Skus);
        var other = Guid.NewGuid();
        db.SetupSet(x => x.SkuAliases, new SkuAlias { SkuUuid = other, Alias = "TGN-TAKEN", Type = SkuAliasType.Factory });

        var result = await CreateImporter().Import(
        [
            Manufactured(uuid: Guid.Empty),
            Manufactured(uuid: Guid.NewGuid(), status: "paused"),
            Manufactured(uuid: Guid.NewGuid(), aliases: [new MasterCatalogSkuAlias("TGN-TAKEN", "factory", true)]),
            Manufactured(uuid: Guid.NewGuid(), components: [new MasterCatalogSkuComponent("R.VM.BRD", 0, "static")]),
            Manufactured(uuid: Guid.NewGuid(), components: [new MasterCatalogSkuComponent("R.VM.BRD", 1, "optional")]),
            Manufactured()
        ], CancellationToken.None);

        Assert.That(result, Is.EqualTo(new MasterCatalogImportResult(1, 0, 4 + 1)));
        Assert.That(skus.Single().SkuUuid, Is.EqualTo(Uuid));
    }
}
