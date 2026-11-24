using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Components.StockLevels;

public class SearchComponentStockLevelsQueryHandler(ComponentStockLevelReader reader)
    : IRequestHandler<SearchComponentStockLevelsQuery, ListResult<ComponentStockLevelDto>>
{
    public async Task<ListResult<ComponentStockLevelDto>> Handle(
        SearchComponentStockLevelsQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, cancellationToken);

        return new ListResult<ComponentStockLevelDto>(rows.AsQueryable().ApplyPaging(request.Request).ToList(), rows.Count);
    }
}
