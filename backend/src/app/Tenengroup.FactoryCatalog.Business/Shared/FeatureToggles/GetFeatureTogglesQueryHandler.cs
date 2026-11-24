using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Shared.FeatureToggles;

public class GetFeatureTogglesQueryHandler(IFeatureToggleService featureToggleService)
    : IRequestHandler<GetFeatureTogglesQuery, IReadOnlyCollection<FeatureToggle>>
{
    public Task<IReadOnlyCollection<FeatureToggle>> Handle(GetFeatureTogglesQuery request, CancellationToken cancellationToken)
    {
        return featureToggleService.GetAll(cancellationToken);
    }
}
