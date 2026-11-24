using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.Balancer.Business.Abstractions.Reports;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Balancer.Domains;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.Balancer.Business.Reports;

internal static class DimCost
{
    private static readonly Dictionary<string, Expression<Func<Allocation, object>>> SortableFields = new()
    {
        ["decidedAt"] = a => a.DecidedAt,
        ["orderId"] = a => a.OrderId,
        ["factoryId"] = a => a.FactoryId,
        ["quotedTotal"] = a => a.ProductionCost,
        ["quotedMaterial"] = a => a.MaterialCost,
        ["quotedLabor"] = a => a.LaborCost,
        ["quotedPacking"] = a => a.PackingCost,
        ["actualLabor"] = a => a.ActualLaborCost,
        ["producedAt"] = a => a.ProducedAt
    };

    public static readonly ExcelColumn<DimCostRowDto>[] Columns =
    [
        new("orderId", "Order ID", r => r.OrderId),
        new("orderItemId", "Order item", r => r.OrderItemId),
        new("skuUuid", "SKU", r => r.SkuUuid),
        new("factoryId", "Factory", r => r.FactoryId),
        new("mode", "Mode", r => r.Mode.ToString()),
        new("decidedAt", "Allocated at", r => r.DecidedAt),
        new("producedAt", "Produced at", r => r.ProducedAt),
        new("quotedMaterial", "Quoted material", r => r.QuotedMaterial),
        new("quotedLabor", "Quoted labor", r => r.QuotedLabor),
        new("quotedPacking", "Quoted packing", r => r.QuotedPacking),
        new("quotedTotal", "Quoted total", r => r.QuotedTotal),
        new("actualLabor", "Actual labor", r => r.ActualLabor),
        new("laborVariance", "Labor variance", r => r.LaborVariance),
        new("currency", "Currency", r => r.Currency)
    ];

    public static IQueryable<DimCostRowDto> Query(IBalancerDbContext db, ListRequest request)
    {
        var allocations = db.Allocations
            .AsNoTracking()
            .Where(a => a.FactoryId != null && a.Mode != AllocationMode.Shadow);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            allocations = allocations.Where(a => a.OrderId.ToLower().Contains(search) || a.SkuUuid.ToLower().Contains(search));
        }

        return allocations
            .ApplyFilter(request, "orderId", a => a.OrderId)
            .ApplyFilter(request, "skuUuid", a => a.SkuUuid)
            .ApplyFilter(request, "factoryId", a => a.FactoryId)
            .ApplyFilter(request, "mode", a => a.Mode)
            .ApplyFilter(request, "decidedAt", a => a.DecidedAt)
            .ApplyFilter(request, "quotedTotal", a => a.ProductionCost)
            .ApplySorting(request, SortableFields, "decidedAt")
            .Select(Projection);
    }

    public static readonly Expression<Func<Allocation, DimCostRowDto>> Projection = a => new DimCostRowDto(
        a.Id,
        a.OrderId,
        a.OrderItemId,
        a.SkuUuid,
        a.FactoryId,
        a.Mode,
        a.DecidedAt,
        a.ProducedAt,
        a.MaterialCost,
        a.LaborCost,
        a.PackingCost,
        a.ProductionCost,
        a.ActualLaborCost,
        a.ActualLaborCost - a.LaborCost,
        a.Currency);
}

public class SearchDimCostQueryHandler(IBalancerDbContext db)
    : IRequestHandler<SearchDimCostQuery, ListResult<DimCostRowDto>>
{
    public Task<ListResult<DimCostRowDto>> Handle(SearchDimCostQuery request, CancellationToken cancellationToken)
    {
        return DimCost.Query(db, request.Request).ToListResult(request.Request, cancellationToken);
    }
}

public class ExportDimCostQueryHandler(IBalancerDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ExportDimCostQuery, ExcelFile>
{
    public async Task<ExcelFile> Handle(ExportDimCostQuery request, CancellationToken cancellationToken)
    {
        var rows = await DimCost.Query(db, request.Request).ToListAsync(cancellationToken);

        return ExcelExporter.Export("dim_cost", rows, DimCost.Columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}

public class GetDimCostDetailsQueryHandler(IBalancerDbContext db)
    : IRequestHandler<GetDimCostDetailsQuery, DimCostDetailsDto>
{
    public async Task<DimCostDetailsDto> Handle(GetDimCostDetailsQuery request, CancellationToken cancellationToken)
    {
        var summary = await db.Allocations
                          .AsNoTracking()
                          .Where(a => a.Id == request.AllocationId)
                          .Select(DimCost.Projection)
                          .FirstOrDefaultAsync(cancellationToken)
                      ?? throw new EntityNotFoundException("Allocation", request.AllocationId);

        var lines = await db.AllocationCostLines
            .AsNoTracking()
            .Where(l => l.AllocationId == request.AllocationId)
            .OrderBy(l => l.Kind)
            .ThenBy(l => l.Code)
            .Select(l => new DimCostLineDto(l.Kind, l.Code, l.Quantity, l.QuotedUnitCost, l.QuotedCost, l.ActualQuantity, l.ActualCost))
            .ToListAsync(cancellationToken);

        return new DimCostDetailsDto(summary, lines);
    }
}
