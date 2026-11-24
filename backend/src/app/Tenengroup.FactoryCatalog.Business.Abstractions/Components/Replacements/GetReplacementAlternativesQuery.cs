using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;

public record GetReplacementAlternativesQuery(string ErpId, string FactoryId)
    : IRequest<IReadOnlyCollection<ReplacementAlternativeDto>>;
