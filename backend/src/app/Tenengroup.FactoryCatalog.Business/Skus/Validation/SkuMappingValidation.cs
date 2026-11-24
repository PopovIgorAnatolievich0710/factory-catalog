using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Skus.Validation;

public static class SkuMappingValidation
{
    public static List<string> Issues(
        Sku sku,
        IReadOnlyDictionary<string, ComponentStatus> componentStatuses,
        IReadOnlySet<string> processIds)
    {
        var issues = new List<string>();
        if (sku.ProductType == SkuProductType.Dropshipper)
        {
            return issues;
        }

        foreach (var component in sku.Components.OrderBy(c => c.ComponentErpId))
        {
            if (!componentStatuses.TryGetValue(component.ComponentErpId, out var status))
            {
                issues.Add($"Component {component.ComponentErpId} not found in SAP");
            }
            else if (status != ComponentStatus.Active)
            {
                issues.Add($"Component {component.ComponentErpId} is not active in SAP");
            }
        }

        foreach (var process in sku.Processes.OrderBy(p => p.ProcessId))
        {
            if (!processIds.Contains(process.ProcessId))
            {
                issues.Add($"Process {process.ProcessId} not found in the catalog");
            }
        }

        return issues;
    }
}
