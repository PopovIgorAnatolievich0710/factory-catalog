using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Components;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components.Replacements;

public class GetReplacementAlternativesQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetReplacementAlternativesQuery, IReadOnlyCollection<ReplacementAlternativeDto>>
{
    public async Task<IReadOnlyCollection<ReplacementAlternativeDto>> Handle(
        GetReplacementAlternativesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FactoryId))
        {
            throw new ValidationException("factoryId", "The factory id is required.");
        }

        if (!await db.Components.AnyAsync(c => c.ErpId == request.ErpId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Component), request.ErpId);
        }

        if (!await db.Factories.AnyAsync(f => f.Id == request.FactoryId, cancellationToken))
        {
            throw new EntityNotFoundException(nameof(Factory), request.FactoryId);
        }

        var alternativeIds = await db.ComponentAlternatives.AsNoTracking()
            .Where(a => a.ComponentErpId == request.ErpId && a.ForPlanner)
            .Select(a => a.AlternativeErpId)
            .ToListAsync(cancellationToken);
        var alternatives = await db.Components.AsNoTracking()
            .Where(c => alternativeIds.Contains(c.ErpId) && c.Status == ComponentStatus.Active)
            .OrderBy(c => c.ErpId)
            .ToListAsync(cancellationToken);
        var stock = await db.ComponentFactories.AsNoTracking()
            .Where(f => f.FactoryId == request.FactoryId && alternativeIds.Contains(f.ComponentErpId))
            .ToDictionaryAsync(f => f.ComponentErpId, cancellationToken);

        return alternatives
            .Select(c =>
            {
                var row = stock.GetValueOrDefault(c.ErpId)
                          ?? new ComponentFactory { ComponentErpId = c.ErpId, FactoryId = request.FactoryId };

                return new ReplacementAlternativeDto(
                    c.ErpId,
                    c.Name,
                    c.UnitOfMeasureUnit,
                    row.WarehouseStock,
                    row.ProductionFloorStock,
                    row.GetTotalStock(),
                    row.GetStockStatus(c.CriticalStockLevel));
            })
            .ToList();
    }
}
