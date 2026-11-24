using Tenengroup.FactoryCatalog.Domains.LegacyBalancer;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;

public record CheckLegacyEligibilityCommand(
    string OrderId,
    string? OrderItemId,
    string? Sku,
    IReadOnlyCollection<string> CbdErpIds,
    IReadOnlyList<string> FactoryIds) : IRequest<LegacyEligibilityDto>;

public record LegacyEligibilityDto(
    string OrderId,
    string? OrderItemId,
    Guid? SkuUuid,
    bool AnyEligible,
    string? FirstEligibleFactoryId,
    string? Message,
    IReadOnlyList<LegacyFactoryEligibilityDto> Factories,
    IReadOnlyList<string> UnknownComponents);

public record LegacyFactoryEligibilityDto(string FactoryId, bool Eligible, IReadOnlyList<LegacySkipDto> Skips);

public record LegacySkipDto(SkipReason Reason, string? ComponentErpId, string? ProcessId);
