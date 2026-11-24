using Moq;
using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class SkuMappingValidatorTests
{
    private Mock<IFactoryCatalogDbContext> db;
    private Mock<IHistoryWriter> history;

    [SetUp]
    public void SetUp()
    {
        db = new Mock<IFactoryCatalogDbContext>();
        history = new Mock<IHistoryWriter>();
        db.SetupSet(x => x.Components,
            new Component { ErpId = "C-OK", Status = ComponentStatus.Active },
            new Component { ErpId = "C-OFF", Status = ComponentStatus.Inactive });
        db.SetupSet(x => x.Processes, new Process { ProcessId = "CUTTING" });
    }

    private static Sku Sku(string[] components, string[] processes, SkuProductType type = SkuProductType.Manufactured)
    {
        return new Sku
        {
            SkuUuid = Guid.NewGuid(),
            ProductType = type,
            Components = components.Select(c => new SkuComponent { ComponentErpId = c, Quantity = 1 }).ToList(),
            Processes = processes.Select(p => new SkuProcess { ProcessId = p, Quantity = 1 }).ToList()
        };
    }

    [Test]
    public async Task Validate_UnknownAndInactiveComponentsAndUnknownProcess_Invalid()
    {
        var sku = Sku(["C-OK", "C-OFF", "C-MISSING"], ["CUTTING", "PLATING"]);

        await new SkuMappingValidator(db.Object, history.Object).Validate([sku], HistorySource.Sync, "Master catalog sync", CancellationToken.None);

        Assert.That(sku.ValidationStatus, Is.EqualTo(SkuValidationStatus.Invalid));
        Assert.That(sku.ValidationIssues, Is.EqualTo(new[]
        {
            "Component C-MISSING not found in SAP",
            "Component C-OFF is not active in SAP",
            "Process PLATING not found in the catalog"
        }));
        history.Verify(h => h.Record(
            HistoryEntityTypes.Sku, sku.SkuUuid.ToString(), SkuMappingValidator.StatusField,
            SkuValidationStatus.Valid, SkuValidationStatus.Invalid, HistorySource.Sync, null, "Master catalog sync"));
    }

    [Test]
    public async Task Validate_FixedMapping_BecomesValid()
    {
        var sku = Sku(["C-OK"], ["CUTTING"]);
        sku.ValidationStatus = SkuValidationStatus.Invalid;
        sku.ValidationIssues = ["Component C-OLD not found in SAP"];

        await new SkuMappingValidator(db.Object, history.Object).Validate([sku], HistorySource.Sync, null, CancellationToken.None);

        Assert.That(sku.ValidationStatus, Is.EqualTo(SkuValidationStatus.Valid));
        Assert.That(sku.ValidationIssues, Is.Empty);
    }

    [Test]
    public async Task Validate_NothingChanged_NoHistory()
    {
        var sku = Sku(["C-OK"], ["CUTTING"]);

        await new SkuMappingValidator(db.Object, history.Object).Validate([sku], HistorySource.Sync, null, CancellationToken.None);

        history.VerifyNoOtherCalls();
    }

    [Test]
    public void Issues_Dropshipper_AlwaysValid()
    {
        var sku = Sku(["C-MISSING"], ["PLATING"], SkuProductType.Dropshipper);

        var issues = SkuMappingValidation.Issues(sku, new Dictionary<string, ComponentStatus>(), new HashSet<string>());

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public async Task ValidateAll_GoesThroughEveryBatchAndSaves()
    {
        var skus = Enumerable.Range(0, SkuMappingValidator.BatchSize + 1).Select(_ => Sku(["C-MISSING"], [])).ToArray();
        db.SetupSet(x => x.Skus, skus);

        await new SkuMappingValidator(db.Object, history.Object).ValidateAll(HistorySource.Sync, "sync", CancellationToken.None);

        Assert.That(skus.Select(s => s.ValidationStatus), Is.All.EqualTo(SkuValidationStatus.Invalid));
        db.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
