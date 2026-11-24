using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;

public record ExportSkusQuery(ExportRequest Request) : IRequest<ExcelFile>;
