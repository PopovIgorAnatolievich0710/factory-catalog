using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Replacements;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.StockLevels;
using Tenengroup.FactoryCatalog.Business.Abstractions.Components.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Components;
using Tenengroup.FactoryCatalog.Business.Components.Alternatives;
using Tenengroup.FactoryCatalog.Business.Components.Replacements;
using Tenengroup.FactoryCatalog.Business.Components.StockLevels;
using Tenengroup.FactoryCatalog.Business.Components.Sync;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class ComponentsModule
{
    public static IServiceCollection AddComponentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SapExportOptions>(configuration.GetSection(SapExportOptions.SectionName));

        services.AddScoped<ISapExportReader, SapCsvExportReader>();
        services.AddScoped<ISapComponentsSyncService, SapComponentsSyncService>();
        services.AddScoped<ISapComponentsSyncJob, SapComponentsSyncJob>();
        services.AddScoped<IRequestHandler<RunSapComponentsSyncCommand, SyncStatusDto>, RunSapComponentsSyncCommandHandler>();

        services.AddScoped<ComponentListReader>();
        services.AddScoped<IRequestHandler<SearchComponentsQuery, ListResult<ComponentListItemDto>>, SearchComponentsQueryHandler>();
        services.AddScoped<IRequestHandler<ExportComponentsQuery, ExcelFile>, ExportComponentsQueryHandler>();
        services.AddScoped<IRequestHandler<GetComponentFilterOptionsQuery, ComponentFilterOptionsDto>, GetComponentFilterOptionsQueryHandler>();
        services.AddScoped<IRequestHandler<GetComponentQuery, ComponentDetailsDto>, GetComponentQueryHandler>();

        services.AddScoped<IRequestHandler<SetManualOosCommand>, SetManualOosCommandHandler>();
        services.AddScoped<IRequestHandler<SetCriticalLevelCommand>, SetCriticalLevelCommandHandler>();
        services.AddScoped<IRequestHandler<SetCriticalLevelOverrideCommand>, SetCriticalLevelOverrideCommandHandler>();
        services.AddScoped<IRequestHandler<AddAlternativeCommand>, AddAlternativeCommandHandler>();
        services.AddScoped<IRequestHandler<SetAlternativeUseCommand>, SetAlternativeUseCommandHandler>();
        services.AddScoped<IRequestHandler<RemoveAlternativeCommand>, RemoveAlternativeCommandHandler>();

        services.AddScoped<ComponentStockLevelReader>();
        services.AddScoped<IRequestHandler<SearchComponentStockLevelsQuery, ListResult<ComponentStockLevelDto>>, SearchComponentStockLevelsQueryHandler>();
        services.AddScoped<IRequestHandler<ExportComponentStockLevelsQuery, ExcelFile>, ExportComponentStockLevelsQueryHandler>();

        services.AddScoped<IRequestHandler<GetReplacementAlternativesQuery, IReadOnlyCollection<ReplacementAlternativeDto>>, GetReplacementAlternativesQueryHandler>();
        services.AddScoped<IRequestHandler<RecordComponentReplacementCommand, ComponentReplacementDto>, RecordComponentReplacementCommandHandler>();

        return services;
    }
}
