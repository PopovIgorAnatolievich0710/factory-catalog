using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Components;

public class GetComponentFilterOptionsQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetComponentFilterOptionsQuery, ComponentFilterOptionsDto>
{
    public async Task<ComponentFilterOptionsDto> Handle(GetComponentFilterOptionsQuery request, CancellationToken cancellationToken)
    {
        var families = await db.Components.AsNoTracking()
            .Where(c => c.Family != null && c.Family != "")
            .Select(c => c.Family)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync(cancellationToken);
        var materials = await db.Components.AsNoTracking()
            .Where(c => c.Material != null && c.Material != "")
            .Select(c => c.Material)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync(cancellationToken);

        return new ComponentFilterOptionsDto(families, materials);
    }
}
