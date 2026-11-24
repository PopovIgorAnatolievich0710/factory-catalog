using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.Business.LegacyBalancer;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.CompositionRoot.Modules;

public static class LegacyBalancerModule
{
    public static IServiceCollection AddLegacyBalancerModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IRequestHandler<CheckLegacyEligibilityCommand, LegacyEligibilityDto>, CheckLegacyEligibilityCommandHandler>();
        services.AddScoped<IRequestHandler<SearchSkipLogQuery, ListResult<SkipLogEntryDto>>, SearchSkipLogQueryHandler>();
        services.AddScoped<IRequestHandler<ExportSkipLogQuery, ExcelFile>, ExportSkipLogQueryHandler>();
        services.AddScoped<IRequestHandler<GetCountrySkuListsQuery, IReadOnlyList<CountrySkuListDto>>, GetCountrySkuListsQueryHandler>();

        return services;
    }
}
