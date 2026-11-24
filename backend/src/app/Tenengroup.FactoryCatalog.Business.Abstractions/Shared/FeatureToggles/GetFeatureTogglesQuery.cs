using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;

public record GetFeatureTogglesQuery : IRequest<IReadOnlyCollection<FeatureToggle>>;
