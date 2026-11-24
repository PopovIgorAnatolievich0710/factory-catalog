using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;

public record SearchFactoryShareQuery(ListRequest Request) : IRequest<ListResult<FactoryShareRowDto>>;

public record ExportFactoryShareQuery(ExportRequest Request) : IRequest<ExcelFile>;

public record GetGlobalFactoryShareQuery : IRequest<GlobalFactoryShareDto>;

public record UpdateGlobalFactoryShareCommand(IReadOnlyCollection<FactorySharePctDto> FactoryShares) : IRequest<GlobalFactoryShareDto>;

public record FactorySharePctDto(string FactoryId, decimal Pct);

public record GlobalFactoryShareDto(
    IReadOnlyCollection<FactorySharePctDto> FactoryShares,
    decimal TotalPct,
    string? UpdatedBy,
    DateTime? UpdatedAt);

public record SetSkuProducibleCommand(Guid SkuUuid, string FactoryId, bool Producible) : IRequest<SkuFactorySharesDto>;

public record BulkSetSkuProducibleCommand(string FactoryId, IReadOnlyCollection<Guid> SkuUuids, bool Producible)
    : IRequest<BulkSetSkuProducibleResultDto>;

public record BulkSetSkuProducibleResultDto(int Updated);

public record ValidateProducibleImportQuery(byte[] File) : IRequest<ImportValidationResultDto>;

public record ImportProducibleCommand(byte[] File) : IRequest<ImportValidationResultDto>;

public record SetSkuShareOverrideCommand(Guid SkuUuid, string FactoryId, decimal SharePct) : IRequest<SkuFactorySharesDto>;

public record ResetSkuShareOverrideCommand(Guid SkuUuid, string FactoryId) : IRequest<SkuFactorySharesDto>;
