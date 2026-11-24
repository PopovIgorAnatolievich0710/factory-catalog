using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Processes;
using Tenengroup.FactoryCatalog.Business.Processes.Import;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class ProcessesModule
{
    public static IServiceCollection AddProcessesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProcessUsageReader, ProcessUsageReader>();
        services.AddScoped<ProcessListReader>();
        services.AddScoped<ProcessCapacityReader>();
        services.AddScoped<ProcessImportPlanner>();

        services.AddScoped<IRequestHandler<SearchProcessesQuery, ListResult<ProcessListItemDto>>, SearchProcessesQueryHandler>();
        services.AddScoped<IRequestHandler<ExportProcessesQuery, ExcelFile>, ExportProcessesQueryHandler>();
        services.AddScoped<IRequestHandler<GetProcessQuery, ProcessDto>, GetProcessQueryHandler>();
        services.AddScoped<IRequestHandler<CreateProcessCommand, string>, CreateProcessCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateProcessCommand>, UpdateProcessCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateProcessFactorySetupCommand>, UpdateProcessFactorySetupCommandHandler>();
        services.AddScoped<IRequestHandler<SetProcessCapacityCommand>, SetProcessCapacityCommandHandler>();
        services.AddScoped<IRequestHandler<GetProcessCapacityOverviewQuery, IReadOnlyCollection<ProcessCapacityRowDto>>, GetProcessCapacityOverviewQueryHandler>();
        services.AddScoped<IRequestHandler<GetFactoryProcessCapacityQuery, IReadOnlyCollection<ProcessCapacityRowDto>>, GetFactoryProcessCapacityQueryHandler>();
        services.AddScoped<IRequestHandler<ValidateProcessImportQuery, ProcessImportValidationDto>, ValidateProcessImportQueryHandler>();
        services.AddScoped<IRequestHandler<ImportProcessesCommand, ProcessImportResultDto>, ImportProcessesCommandHandler>();

        return services;
    }
}
