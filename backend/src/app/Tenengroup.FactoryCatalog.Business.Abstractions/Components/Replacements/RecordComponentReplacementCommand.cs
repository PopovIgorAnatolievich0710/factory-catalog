using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;

public record RecordComponentReplacementCommand(
    string OrderId,
    string OrderItemId,
    string FactoryId,
    string OriginalErpId,
    string ReplacementErpId,
    decimal? Quantity,
    string ReplacedBy,
    DateTime? ReplacedAt) : IRequest<ComponentReplacementDto>;
