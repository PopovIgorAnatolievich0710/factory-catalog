using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Skus.List;

public class SearchSkusQueryHandler(SkuListReader reader) : IRequestHandler<SearchSkusQuery, ListResult<SkuListItemDto>>
{
    public Task<ListResult<SkuListItemDto>> Handle(SearchSkusQuery request, CancellationToken cancellationToken)
    {
        return reader.Read(request.Request, paged: true, cancellationToken);
    }
}
