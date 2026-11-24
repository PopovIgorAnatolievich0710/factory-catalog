using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Factories;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.Business.Shared.Factories;
using Tenengroup.FactoryCatalog.Business.Shared.FeatureToggles;
using Tenengroup.FactoryCatalog.Business.Shared.History;
using Tenengroup.FactoryCatalog.Business.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Shared.Users;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddScoped<FeatureToggleService>();
        services.AddScoped<IFeatureToggleService>(sp => sp.GetRequiredService<FeatureToggleService>());
        services.AddScoped<IPermissionEnforcementToggle>(sp => sp.GetRequiredService<FeatureToggleService>());

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IHistoryWriter, HistoryWriter>();
        services.AddScoped<ISyncRunTracker, SyncRunTracker>();

        services.AddScoped<IRequestHandler<GetCurrentUserQuery, CurrentUserDto>, GetCurrentUserQueryHandler>();
        services.AddScoped<IRequestHandler<GetListSettingsQuery, ListSettingsDto>, GetListSettingsQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateListSettingsCommand, ListSettingsDto>, UpdateListSettingsCommandHandler>();
        services.AddScoped<IRequestHandler<GetFactoriesQuery, IReadOnlyCollection<FactoryDto>>, GetFactoriesQueryHandler>();
        services.AddScoped<IRequestHandler<GetHistoryQuery, ListResult<HistoryEntryDto>>, GetHistoryQueryHandler>();
        services.AddScoped<IRequestHandler<GetSyncStatusQuery, SyncStatusDto>, GetSyncStatusQueryHandler>();
        services.AddScoped<IRequestHandler<GetFeatureTogglesQuery, IReadOnlyCollection<FeatureToggle>>, GetFeatureTogglesQueryHandler>();

        return services;
    }
}
