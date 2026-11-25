using Tenengroup.Balancer.Business.Abstractions.Catalog;

namespace Tenengroup.Balancer.Business.Abstractions.Decisions;

public static class ExclusionReasons
{
    public const string NotProducible = "NOT_PRODUCIBLE";
    public const string NoCapacity = "NO_CAPACITY";
    public const string CapacityReserved = "CAPACITY_RESERVED";
    public const string NoRoute = "NO_ROUTE";
}

public static class FallbackReasons
{
    public const string QuotesUnavailable = "QuotesUnavailable";
    public const string RoutesUnavailable = "RoutesUnavailable";
    public const string NoFeasibleFactory = "NoFeasibleFactory";
}

public record DecisionSettings(
    decimal LatePenaltyPerDay,
    decimal SplitPenalty,
    decimal ShareWeight,
    bool SuccessRateEnabled,
    decimal SuccessRateWeight,
    int DefaultProductionDays);

public record RouteOption(
    string FactoryId,
    string PackingFacilityId,
    decimal CostAmount,
    string CostCurrency,
    int TransitDaysMin,
    int TransitDaysMax,
    IReadOnlyCollection<int> WorkingDays)
{
    public DateOnly DeliveryAfter(DateOnly shipDate, int transitDays)
    {
        var date = shipDate;
        for (var counted = 0; counted < transitDays;)
        {
            date = date.AddDays(1);
            if (IsWorkingDay(date))
            {
                counted++;
            }
        }

        return date;
    }

    public DateOnly LatestShipDate(DateOnly deliveryDate, int transitDays)
    {
        var date = deliveryDate;
        for (var counted = 0; counted < transitDays;)
        {
            if (IsWorkingDay(date))
            {
                counted++;
            }

            date = date.AddDays(-1);
        }

        return date;
    }

    private bool IsWorkingDay(DateOnly date)
    {
        return WorkingDays.Count == 0 || WorkingDays.Contains(date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek);
    }
}

public record DecisionItem(string OrderItemId, QuoteSet? QuoteSet);

public record DecisionInput(
    IReadOnlyCollection<DecisionItem> Items,
    DateOnly Today,
    DateOnly? PromisedDeliveryDate,
    DateOnly? SlaDeliveryDate,
    bool IsEventOrder,
    IReadOnlyCollection<RouteOption> Routes,
    IReadOnlyDictionary<string, int> ReservedCapacityPct,
    IReadOnlyDictionary<string, decimal> ActualSharePct,
    IReadOnlyDictionary<string, decimal> FallbackSharePct,
    IReadOnlyDictionary<string, int> ProductionDays,
    IReadOnlyDictionary<string, decimal> SuccessRates,
    IReadOnlyCollection<string> AnchorFactoryIds,
    IReadOnlyDictionary<string, string> PackingFacilityIds,
    DecisionSettings Settings);

public record Candidate(
    string FactoryId,
    string? QuoteId,
    decimal? ProductionCost,
    decimal? ShippingCost,
    decimal? TotalCost,
    string? Currency,
    DateOnly? EtaFrom,
    DateOnly? EtaTo,
    int? TransitDaysMax,
    decimal SharePct,
    int? CapacityPct,
    decimal? EffectiveSharePct,
    decimal? Score,
    bool? MeetsPromise,
    bool? MeetsSla,
    string? ExcludedReason,
    bool IsWinner);

public record ItemDecision(
    string OrderItemId,
    Candidate Winner,
    IReadOnlyCollection<Candidate> Candidates,
    string? FallbackReason,
    decimal? CheapestFeasibleTotalCost,
    bool IsSplit,
    string Reason)
{
    public bool IsFallback => FallbackReason != null;
}

public record OrderDecision(IReadOnlyCollection<ItemDecision> Items);

public interface IRoutingDecisionEngine
{
    OrderDecision Decide(DecisionInput input);
}
