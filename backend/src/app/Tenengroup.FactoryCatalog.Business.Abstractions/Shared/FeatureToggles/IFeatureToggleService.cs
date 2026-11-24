using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;

public interface IFeatureToggleService
{
    Task<IReadOnlyCollection<FeatureToggle>> GetAll(CancellationToken cancellationToken = default);

    Task<bool> IsEnabled(string name, CancellationToken cancellationToken = default);
}
