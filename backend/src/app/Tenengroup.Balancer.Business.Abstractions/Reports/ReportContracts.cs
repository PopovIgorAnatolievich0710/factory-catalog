using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Abstractions.Reports;

public record DashboardFilter(string? FactoryId, string? Country, DateOnly From, DateOnly To);

public record GetRoutingDashboardQuery(DashboardFilter Filter) : IRequest<RoutingDashboardDto>;

public record RoutedPerDayDto(DateOnly Date, string FactoryId, int Items);

public record FactoryShareDto(string FactoryId, decimal TargetPct, decimal ActualPct, decimal GapPct, bool IsGapHighlighted);

public record RoutingDashboardDto(
    int ItemsRouted,
    int OrdersRouted,
    int ItemsTransferred,
    decimal? TransferRatePct,
    decimal? OnTimePct,
    decimal? AvgCostVsCheapestPct,
    int FallbackCount,
    decimal ShareGapThresholdPct,
    IReadOnlyCollection<RoutedPerDayDto> RoutedPerDay,
    IReadOnlyCollection<FactoryShareDto> Shares,
    IReadOnlyCollection<string> Countries);

public record GetShadowComparisonQuery(DashboardFilter Filter) : IRequest<ShadowComparisonDto>;

public record DisagreementReasonDto(string Reason, int Count, decimal SharePct);

public record ShadowComparisonDto(
    int Compared,
    int Agreed,
    decimal? AgreementRatePct,
    decimal EstimatedSavings,
    string? Currency,
    IReadOnlyCollection<DisagreementReasonDto> DisagreementReasons);

public record DimCostRowDto(
    Guid AllocationId,
    string OrderId,
    string OrderItemId,
    string SkuUuid,
    string? FactoryId,
    AllocationMode Mode,
    DateTime DecidedAt,
    DateTime? ProducedAt,
    decimal? QuotedMaterial,
    decimal? QuotedLabor,
    decimal? QuotedPacking,
    decimal? QuotedTotal,
    decimal? ActualLabor,
    decimal? LaborVariance,
    string? Currency);

public record DimCostLineDto(
    CostLineKind Kind,
    string Code,
    decimal Quantity,
    decimal? QuotedUnitCost,
    decimal QuotedCost,
    decimal? ActualQuantity,
    decimal? ActualCost);

public record DimCostDetailsDto(DimCostRowDto Summary, IReadOnlyCollection<DimCostLineDto> Lines);

public record SearchDimCostQuery(ListRequest Request) : IRequest<ListResult<DimCostRowDto>>;

public record ExportDimCostQuery(ExportRequest Request) : IRequest<ExcelFile>;

public record GetDimCostDetailsQuery(Guid AllocationId) : IRequest<DimCostDetailsDto>;
