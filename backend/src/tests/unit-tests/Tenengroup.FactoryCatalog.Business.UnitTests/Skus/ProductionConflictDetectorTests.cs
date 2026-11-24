using NUnit.Framework;
using Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.UnitTests.Skus;

[TestFixture]
public class ProductionConflictDetectorTests
{
    private static readonly ProductionLogEntry[] Assigned = [new("ENGRAVING", "LASER"), new("PACKING", null)];

    [Test]
    public void Detect_LogMatchesMapping_NoConflict()
    {
        var conflicts = ProductionConflictDetector.Detect(Assigned, [new("engraving", "laser"), new("PACKING", null)]);

        Assert.That(conflicts, Is.Empty);
    }

    [Test]
    public void Detect_DifferentMachine_MachineMismatch()
    {
        var conflicts = ProductionConflictDetector.Detect(Assigned, [new("ENGRAVING", "CNC"), new("PACKING", null)]);

        Assert.That(conflicts, Has.Count.EqualTo(1));
        var conflict = conflicts.Single();
        Assert.That(conflict.Kind, Is.EqualTo(ProductionConflictKind.MachineMismatch));
        Assert.That(conflict.Expected, Is.EqualTo("ENGRAVING on LASER"));
        Assert.That(conflict.Actual, Is.EqualTo("ENGRAVING on CNC"));
    }

    [Test]
    public void Detect_ProcessUsedButNotListedAndListedButNotUsed()
    {
        var conflicts = ProductionConflictDetector.Detect(Assigned, [new("ENGRAVING", "LASER"), new("PLATING", "TANK")]);

        Assert.That(conflicts.Select(c => (c.Kind, c.ProcessId)), Is.EquivalentTo(new[]
        {
            (ProductionConflictKind.ProcessNotListed, "PLATING"),
            (ProductionConflictKind.ProcessNotUsed, "PACKING")
        }));
    }

    [Test]
    public void ParseLog_ArrayOrObjectWithProcesses()
    {
        var array = ProductionConflictDetector.ParseLog("[{\"processId\":\"ENGRAVING\",\"machineType\":\"CNC\"},{\"processId\":\"\"}]");
        var wrapped = ProductionConflictDetector.ParseLog("{\"processes\":[{\"processId\":\"PACKING\"}]}");

        Assert.That(array, Is.EqualTo(new[] { new ProductionLogEntry("ENGRAVING", "CNC") }));
        Assert.That(wrapped, Is.EqualTo(new[] { new ProductionLogEntry("PACKING", null) }));
    }

    [TestCase("not json")]
    [TestCase("\"text\"")]
    public void ParseLog_Unreadable_Empty(string json)
    {
        Assert.That(ProductionConflictDetector.ParseLog(json), Is.Empty);
    }
}
