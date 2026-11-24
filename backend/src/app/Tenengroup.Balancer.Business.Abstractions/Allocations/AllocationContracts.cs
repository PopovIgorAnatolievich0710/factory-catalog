using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Abstractions.Allocations;

public record AllocationItemRequest(
    string OrderItemId,
    string SkuUuid,
    int Quantity,
    IReadOnlyCollection<CbdComponent>? Cbd,
    FulfillmentType FulfillmentType);

public record AllocateOrderCommand(
    string OrderId,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate,
    DateOnly? SlaDeliveryDate,
    bool IsEventOrder,
    string? EventName,
    string? Site,
    DateTime? PaidAt,
    IReadOnlyCollection<AllocationItemRequest> Items) : IRequest<IReadOnlyCollection<AllocationDto>>;

public record AllocationDto(
    Guid Id,
    string OrderId,
    string OrderItemId,
    string SkuUuid,
    int Quantity,
    FulfillmentType FulfillmentType,
    string DestinationCountry,
    AllocationMode Mode,
    AllocationStatus Status,
    string? FallbackReason,
    string? QuoteKey,
    string? SelectedQuoteId,
    string? FactoryId,
    string? OriginalFactoryId,
    string? PackingFacilityId,
    string? ProductionPage,
    decimal? ProductionCost,
    decimal? ShippingCost,
    decimal? TotalCost,
    string? Currency,
    DateOnly? EtaFrom,
    DateOnly? EtaTo,
    DateOnly? DueDate,
    DateOnly? PromisedDeliveryDate,
    DateOnly? SlaDeliveryDate,
    bool IsEventOrder,
    bool? MeetsPromise,
    bool IsSplit,
    string DecisionReason,
    IReadOnlyCollection<Candidate> Candidates,
    string? LegacyFactoryId,
    int TransferCount,
    DateTime DecidedAt,
    DateTime? ProducedAt);

public record SearchAllocationsQuery(ListRequest Request) : IRequest<ListResult<AllocationDto>>;

public record GetAllocationQuery(Guid Id) : IRequest<AllocationDto>;

public record EstimateAllocationQuery(
    string SkuUuid,
    int Quantity,
    IReadOnlyCollection<CbdComponent>? Cbd,
    string DestinationCountry,
    DateOnly? PromisedDeliveryDate) : IRequest<EstimateDto>;

public record EstimateDto(
    string FactoryId,
    string PackingFacilityId,
    DateOnly? EtaFrom,
    DateOnly? EtaTo,
    decimal? TotalCost,
    string? Currency,
    bool IsFallback,
    IReadOnlyCollection<Candidate> Candidates);

public record RecordLegacyDecisionCommand(
    string OrderId,
    string OrderItemId,
    string FactoryId,
    decimal? TotalCost,
    DateOnly? Eta) : IRequest;

public interface IAllocationOutputQueue
{
    void Enqueue(Guid allocationId);
}

public interface IAllocationOutputJob
{
    Task Publish(Guid allocationId, CancellationToken cancellationToken);
}

public interface IOrderQueueHandler
{
    Task Execute(CancellationToken cancellationToken);
}
