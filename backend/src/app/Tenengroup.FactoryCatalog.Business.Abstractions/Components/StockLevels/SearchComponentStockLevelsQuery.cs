using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;

public record SearchComponentStockLevelsQuery(ListRequest Request) : IRequest<ListResult<ComponentStockLevelDto>>;
