using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Abstractions.Transfers;

public record OpenItemDto(
    Guid AllocationId,
    string OrderId,
    string OrderItemId,
    string SkuUuid,
    string FactoryId,
    string? ProductionPage,
    string? EventName,
    string? Site,
    DateOnly? DueDate,
    string? QuoteId,
    bool CanTransfer);

public record SearchOpenItemsQuery(ListRequest Request) : IRequest<ListResult<OpenItemDto>>;

public record GetOpenItemIdsQuery(ListRequest Request) : IRequest<IReadOnlyCollection<Guid>>;

public record GetProductionPagesQuery(string FactoryId) : IRequest<IReadOnlyCollection<string>>;

public record TransferRequest(
    IReadOnlyCollection<Guid> AllocationIds,
    string ToFactoryId,
    string ToPage,
    TransferReason Reason,
    string? Note);

public record TransferItemDto(
    Guid AllocationId,
    string OrderId,
    string OrderItemId,
    string FromFactoryId,
    string? FromPage,
    string ToFactoryId,
    string ToPage,
    string? OldQuoteId,
    string? NewQuoteId,
    decimal? CostDelta,
    string? Currency,
    int? TargetCapacityPct,
    DateOnly? OldEta,
    DateOnly? NewEta,
    bool Skipped,
    string? Problem);

public record PreviewTransferQuery(TransferRequest Request) : IRequest<IReadOnlyCollection<TransferItemDto>>;

public record TransferItemsCommand(TransferRequest Request) : IRequest<TransferResultDto>;

public record TransferResultDto(int Transferred, IReadOnlyCollection<TransferItemDto> Items);

public record TransferLogDto(
    long Id,
    DateTime TransferredAt,
    string TransferredBy,
    TransferInitiator Initiator,
    string OrderId,
    string OrderItemId,
    string SkuUuid,
    string FromFactoryId,
    string? FromPage,
    string ToFactoryId,
    string ToPage,
    TransferReason Reason,
    string? Note,
    string? OldQuoteId,
    string? NewQuoteId,
    decimal? CostDelta,
    string? Currency,
    DateOnly? OldEta,
    DateOnly? NewEta);

public record SearchTransfersQuery(ListRequest Request) : IRequest<ListResult<TransferLogDto>>;
