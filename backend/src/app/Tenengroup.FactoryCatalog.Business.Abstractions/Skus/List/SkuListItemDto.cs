using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;

public record SkuListItemDto(
    Guid SkuUuid,
    string SiteSku,
    IReadOnlyCollection<string> FactorySkus,
    string Name,
    SkuProductType ProductType,
    SkuStatus Status,
    IReadOnlyCollection<string> ProducibleAt,
    SkuValidationStatus ValidationStatus,
    IReadOnlyCollection<string> ProductionConflictFactoryIds,
    string? DropshipperName,
    DateTime? LastMasterSyncAt);
