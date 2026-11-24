using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;

public record SearchSkipLogQuery(ListRequest Request) : IRequest<ListResult<SkipLogEntryDto>>;

public record ExportSkipLogQuery(ExportRequest Request) : IRequest<ExcelFile>;

public record SkipLogEntryDto(
    long Id,
    string OrderId,
    string? OrderItemId,
    Guid? SkuUuid,
    string FactoryId,
    SkipReason Reason,
    string? ComponentErpId,
    string? ProcessId,
    DateTime CreatedAt);
