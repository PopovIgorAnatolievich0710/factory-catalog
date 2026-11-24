using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Json;

namespace Tenengroup.Balancer.Business.Allocations;

internal static class AllocationMapper
{
    public const string PackingProcessId = "PACKING";

    public static IReadOnlyCollection<Candidate> ReadCandidates(this Allocation allocation)
    {
        return allocation.CandidatesJson.FromJsonString<List<Candidate>>() ?? [];
    }

    public static AllocationDto ToDto(this Allocation allocation, string legacyFactoryId)
    {
        return new AllocationDto(
            allocation.Id,
            allocation.OrderId,
            allocation.OrderItemId,
            allocation.SkuUuid,
            allocation.Quantity,
            allocation.FulfillmentType,
            allocation.DestinationCountry,
            allocation.Mode,
            allocation.Status,
            allocation.FallbackReason,
            allocation.QuoteKey,
            allocation.SelectedQuoteId,
            allocation.FactoryId,
            allocation.OriginalFactoryId,
            allocation.PackingFacilityId,
            allocation.ProductionPage,
            allocation.ProductionCost,
            allocation.ShippingCost,
            allocation.TotalCost,
            allocation.Currency,
            allocation.EtaFrom,
            allocation.EtaTo,
            allocation.DueDate,
            allocation.PromisedDeliveryDate,
            allocation.SlaDeliveryDate,
            allocation.IsEventOrder,
            allocation.MeetsPromise,
            allocation.IsSplit,
            allocation.DecisionReason,
            allocation.ReadCandidates(),
            legacyFactoryId,
            allocation.TransferCount,
            allocation.DecidedAt,
            allocation.ProducedAt);
    }

    public static void ApplyQuote(this Allocation allocation, string quoteKey, FactoryQuote quote)
    {
        allocation.QuoteKey = quoteKey;
        allocation.SelectedQuoteId = quote.QuoteId;
        allocation.MaterialCost = quote.Cost?.Material;
        allocation.LaborCost = quote.Cost?.Labor;
        allocation.PackingCost = quote.Cost?.Packing;
        allocation.ProductionCost = quote.Cost?.Total;
        allocation.Currency = quote.Cost?.Currency;
    }

    public static List<AllocationCostLine> ToCostLines(this FactoryQuote quote)
    {
        var components = (quote.Components ?? []).Select(c => new AllocationCostLine
        {
            Kind = CostLineKind.Component,
            Code = c.AlternativeUsed ?? c.ErpId,
            Quantity = c.Quantity,
            QuotedUnitCost = c.UnitCost,
            QuotedCost = c.Cost ?? (c.UnitCost ?? 0m) * c.Quantity
        });

        var processes = (quote.Processes ?? []).Select(p => new AllocationCostLine
        {
            Kind = p.ProcessId == PackingProcessId ? CostLineKind.Packing : CostLineKind.Process,
            Code = p.ProcessId,
            Quantity = p.Quantity,
            QuotedUnitCost = p.LaborCost.HasValue && p.Quantity != 0 ? p.LaborCost.Value / p.Quantity : null,
            QuotedCost = p.LaborCost ?? 0m
        });

        return components.Concat(processes).ToList();
    }
}
