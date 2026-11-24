using Tenengroup.FactoryCatalog.Domains.Skus;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;

public record SkuFactoryShareDto(
    string FactoryId,
    bool Producible,
    decimal DefaultSharePct,
    decimal? OverrideSharePct,
    decimal EffectiveSharePct,
    SkuShareSource Source,
    string? OverrideBy,
    DateTime? OverrideAt);

public record SkuFactorySharesDto(Guid SkuUuid, IReadOnlyCollection<SkuFactoryShareDto> Factories, decimal ShareTotalPct);

public record FactoryShareRowDto(
    Guid SkuUuid,
    string SiteSku,
    string Name,
    SkuStatus Status,
    IReadOnlyCollection<SkuFactoryShareDto> Factories,
    decimal ShareTotalPct);
