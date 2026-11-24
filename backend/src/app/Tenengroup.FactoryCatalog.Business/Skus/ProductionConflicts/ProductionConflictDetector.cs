using System.Text.Json;
using Tenengroup.FactoryCatalog.Domains.Skus;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;

public record ProductionLogEntry(string ProcessId, string MachineType);

public record ProductionConflict(ProductionConflictKind Kind, string ProcessId, string MachineType, string Expected, string Actual);

public static class ProductionConflictDetector
{
    public const string NotAssigned = "not assigned";
    public const string NotUsed = "not used";
    private const string ProcessesProperty = "processes";

    public static IReadOnlyCollection<ProductionLogEntry> ParseLog(string productionLogJson)
    {
        try
        {
            using var document = JsonDocument.Parse(productionLogJson);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(ProcessesProperty, out var processes))
            {
                root = processes;
            }

            if (root.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return root.Deserialize<List<ProductionLogEntry>>(JsonExtensions.Options)
                .Where(e => !string.IsNullOrWhiteSpace(e?.ProcessId))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyCollection<ProductionConflict> Detect(
        IReadOnlyCollection<ProductionLogEntry> assigned,
        IReadOnlyCollection<ProductionLogEntry> used)
    {
        var conflicts = new List<ProductionConflict>();
        foreach (var entry in used)
        {
            var expected = assigned.FirstOrDefault(a => SameText(a.ProcessId, entry.ProcessId));
            if (expected == null)
            {
                conflicts.Add(new ProductionConflict(
                    ProductionConflictKind.ProcessNotListed, entry.ProcessId, entry.MachineType, NotAssigned, Describe(entry)));
            }
            else if (!string.IsNullOrWhiteSpace(expected.MachineType)
                     && !string.IsNullOrWhiteSpace(entry.MachineType)
                     && !SameText(expected.MachineType, entry.MachineType))
            {
                conflicts.Add(new ProductionConflict(
                    ProductionConflictKind.MachineMismatch, expected.ProcessId, entry.MachineType, Describe(expected), Describe(entry)));
            }
        }

        foreach (var expected in assigned.Where(a => !used.Any(u => SameText(u.ProcessId, a.ProcessId))))
        {
            conflicts.Add(new ProductionConflict(
                ProductionConflictKind.ProcessNotUsed, expected.ProcessId, expected.MachineType, Describe(expected), NotUsed));
        }

        return conflicts;
    }

    private static bool SameText(string left, string right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Describe(ProductionLogEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.MachineType) ? entry.ProcessId : $"{entry.ProcessId} on {entry.MachineType}";
    }
}
