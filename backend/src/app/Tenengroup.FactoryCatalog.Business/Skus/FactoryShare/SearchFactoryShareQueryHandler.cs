using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class SearchFactoryShareQueryHandler(FactoryShareListReader reader)
    : IRequestHandler<SearchFactoryShareQuery, ListResult<FactoryShareRowDto>>
{
    public Task<ListResult<FactoryShareRowDto>> Handle(SearchFactoryShareQuery request, CancellationToken cancellationToken)
    {
        return reader.Read(request.Request, paged: true, cancellationToken);
    }
}
