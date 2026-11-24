using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Details;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.List;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Details;
using Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;
using Tenengroup.FactoryCatalog.Business.Skus.Import;
using Tenengroup.FactoryCatalog.Business.Skus.List;
using Tenengroup.FactoryCatalog.Business.Skus.ProcessMappings;
using Tenengroup.FactoryCatalog.Business.Skus.ProductionConflicts;
using Tenengroup.FactoryCatalog.Business.Skus.Sync;
using Tenengroup.FactoryCatalog.Business.Skus.Validation;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class SkusModule
{
    public static IServiceCollection AddSkusModule(this IServiceCollection services, IConfiguration configuration)
    {
        var masterCatalog = configuration.GetSection(MasterCatalogOptions.SectionName).Get<MasterCatalogOptions>() ?? new MasterCatalogOptions();
        services.AddHttpClient<IMasterCatalogClient, MasterCatalogClient>(client =>
        {
            if (!string.IsNullOrWhiteSpace(masterCatalog.BaseUrl))
            {
                client.BaseAddress = new Uri(masterCatalog.BaseUrl.TrimEnd('/') + "/");
            }

            if (!string.IsNullOrEmpty(masterCatalog.Username))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{masterCatalog.Username}:{masterCatalog.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        });

        services.AddScoped<SkuFactoryShareService>();
        services.AddScoped<FactoryShareListReader>();
        services.AddScoped<SkuListReader>();
        services.AddScoped<ProductionConflictReader>();
        services.AddScoped<SkuIdentifierResolver>();
        services.AddScoped<ProducibleImportParser>();
        services.AddScoped<ProcessMappingImportParser>();
        services.AddScoped<SkuMappingValidator>();
        services.AddScoped<MasterCatalogSkuImporter>();
        services.AddScoped<IMasterCatalogSyncJob, MasterCatalogSyncJob>();

        services.AddScoped<IRequestHandler<TriggerMasterCatalogSyncCommand, SyncStatusDto>, TriggerMasterCatalogSyncCommandHandler>();
        services.AddScoped<IRequestHandler<SearchSkusQuery, ListResult<SkuListItemDto>>, SearchSkusQueryHandler>();
        services.AddScoped<IRequestHandler<ExportSkusQuery, ExcelFile>, ExportSkusQueryHandler>();
        services.AddScoped<IRequestHandler<GetSkusSummaryQuery, SkusSummaryDto>, GetSkusSummaryQueryHandler>();
        services.AddScoped<IRequestHandler<GetSkuQuery, SkuDetailsDto>, GetSkuQueryHandler>();
        services.AddScoped<IRequestHandler<SearchFactoryShareQuery, ListResult<FactoryShareRowDto>>, SearchFactoryShareQueryHandler>();
        services.AddScoped<IRequestHandler<ExportFactoryShareQuery, ExcelFile>, ExportFactoryShareQueryHandler>();
        services.AddScoped<IRequestHandler<GetGlobalFactoryShareQuery, GlobalFactoryShareDto>, GetGlobalFactoryShareQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateGlobalFactoryShareCommand, GlobalFactoryShareDto>, UpdateGlobalFactoryShareCommandHandler>();
        services.AddScoped<IRequestHandler<SetSkuProducibleCommand, SkuFactorySharesDto>, SetSkuProducibleCommandHandler>();
        services.AddScoped<IRequestHandler<BulkSetSkuProducibleCommand, BulkSetSkuProducibleResultDto>, BulkSetSkuProducibleCommandHandler>();
        services.AddScoped<IRequestHandler<ValidateProducibleImportQuery, ImportValidationResultDto>, ValidateProducibleImportQueryHandler>();
        services.AddScoped<IRequestHandler<ImportProducibleCommand, ImportValidationResultDto>, ImportProducibleCommandHandler>();
        services.AddScoped<IRequestHandler<SetSkuShareOverrideCommand, SkuFactorySharesDto>, SetSkuShareOverrideCommandHandler>();
        services.AddScoped<IRequestHandler<ResetSkuShareOverrideCommand, SkuFactorySharesDto>, ResetSkuShareOverrideCommandHandler>();
        services.AddScoped<IRequestHandler<ValidateProcessMappingImportQuery, ImportValidationResultDto>, ValidateProcessMappingImportQueryHandler>();
        services.AddScoped<IRequestHandler<ImportProcessMappingsCommand, ImportValidationResultDto>, ImportProcessMappingsCommandHandler>();

        return services;
    }
}
