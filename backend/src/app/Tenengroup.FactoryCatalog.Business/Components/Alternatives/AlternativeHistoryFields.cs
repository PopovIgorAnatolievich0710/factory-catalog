namespace Tenengroup.FactoryCatalog.Business.Components.Alternatives;

public static class AlternativeHistoryFields
{
    public static string ForBalancer(string alternativeErpId) => $"alternative {alternativeErpId} forBalancer";

    public static string ForPlanner(string alternativeErpId) => $"alternative {alternativeErpId} forPlanner";
}
