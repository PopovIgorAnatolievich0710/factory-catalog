using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.LegacyBalancer;

public class SearchSkipLogQueryHandler(IFactoryCatalogDbContext db)
    : IRequestHandler<SearchSkipLogQuery, ListResult<SkipLogEntryDto>>
{
    public Task<ListResult<SkipLogEntryDto>> Handle(SearchSkipLogQuery request, CancellationToken cancellationToken)
    {
        return SkipLogList.Query(db, request.Request).ToListResult(request.Request, cancellationToken);
    }
}
