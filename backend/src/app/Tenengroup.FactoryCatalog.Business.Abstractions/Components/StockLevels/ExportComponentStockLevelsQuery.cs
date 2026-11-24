using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;

public record ExportComponentStockLevelsQuery(ExportRequest Request) : IRequest<ExcelFile>;
