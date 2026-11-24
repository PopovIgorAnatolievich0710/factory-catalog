using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.Balancer.Business.Abstractions.DailyPlan;

public record GetDailyPlanQuery(string FactoryId, DateOnly Date) : IRequest<DailyPlanDto>;

public record DailyPlanItemDto(
    Guid AllocationId,
    string OrderId,
    string OrderItemId,
    string SkuUuid,
    string? QuoteId,
    DateOnly? DueDate,
    decimal Quantity);

public record DailyPlanProcessDto(
    string ProcessId,
    string Name,
    string? Type,
    string? MeasuredIn,
    int Items,
    decimal Quantity,
    decimal? Done,
    decimal? Available,
    decimal? LoadPct,
    bool IsOverCapacity,
    IReadOnlyCollection<DailyPlanItemDto> OrderItems);

public record DailyPlanDto(
    string FactoryId,
    DateOnly Date,
    int ItemsToProduce,
    bool CapacityKnown,
    IReadOnlyCollection<DailyPlanProcessDto> Processes);
