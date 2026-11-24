using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Factories;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.Factories;

public class GetFactoriesQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>>
{
    public async Task<IReadOnlyCollection<FactoryDto>> Handle(GetFactoriesQuery request, CancellationToken cancellationToken)
    {
        return await db.Factories
            .AsNoTracking()
            .OrderBy(f => f.Id)
            .Select(f => new FactoryDto(f.Id, f.Name, f.CountryCode, f.IsActive))
            .ToListAsync(cancellationToken);
    }
}
