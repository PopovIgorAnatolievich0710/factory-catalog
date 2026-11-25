using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenengroup.Balancer.Business.Abstractions;
using Tenengroup.Balancer.Business.Abstractions.Allocations;
using Tenengroup.Balancer.Business.Abstractions.Catalog;
using Tenengroup.Balancer.Business.Abstractions.DailyPlan;
using Tenengroup.Balancer.Business.Abstractions.Decisions;
using Tenengroup.Balancer.Business.Abstractions.Production;
using Tenengroup.Balancer.Business.Abstractions.Reports;
using Tenengroup.Balancer.Business.Abstractions.Settings;
using Tenengroup.Balancer.Business.Abstractions.Shipping;
using Tenengroup.Balancer.Business.Abstractions.Transfers;
using Tenengroup.Balancer.Business.Abstractions.Users;
using Tenengroup.Balancer.Business.Allocations;
using Tenengroup.Balancer.Business.Catalog;
using Tenengroup.Balancer.Business.DailyPlan;
using Tenengroup.Balancer.Business.Decisions;
using Tenengroup.Balancer.Business.Production;
using Tenengroup.Balancer.Business.Reports;
using Tenengroup.Balancer.Business.Settings;
using Tenengroup.Balancer.Business.Shipping;
using Tenengroup.Balancer.Business.Transfers;
using Tenengroup.Balancer.Business.Users;
using Tenengroup.Balancer.CompositionRoot.Jobs;
using Tenengroup.Balancer.DataAccess;
using Tenengroup.Balancer.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Aws;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.Balancer.CompositionRoot;

public static class Root
{
    public static IServiceCollection RegisterRoot(this IServiceCollection services, IConfiguration configuration, string connectionString)
    {
        var options = configuration.GetSection(BalancerOptions.SectionName).Get<BalancerOptions>() ?? new BalancerOptions();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(options);
        services.AddSingleton(configuration.GetSection(OrderItemsOptions.SectionName).Get<OrderItemsOptions>() ?? new OrderItemsOptions());
        services.AddSingleton(configuration.GetSection(SupplierEventsOptions.SectionName).Get<SupplierEventsOptions>() ?? new SupplierEventsOptions());
        services.AddSingleton(configuration.GetSection(BalancerEventsOptions.SectionName).Get<BalancerEventsOptions>() ?? new BalancerEventsOptions());
        services.AddSingleton(configuration.GetSection(MasterCatalogOptions.SectionName).Get<MasterCatalogOptions>() ?? new MasterCatalogOptions());
        services.AddMemoryCache();
        services.AddScoped<IRequestRunner, HandlerBasedRequestRunner>();

        services.AddDbContext<BalancerDbContext>(builder => BalancerDbContextOptions.Configure(builder, connectionString));
        services.AddScoped<IBalancerDbContext>(sp => sp.GetRequiredService<BalancerDbContext>());

        services.AddAwsInfrastructure(configuration);
        services.AddHangfireModule(connectionString);
        services.AddExternalClients(configuration);

        services.AddHostedService<RecurringJobsHostedService>();
        services.AddHostedService<OrderQueueConsumerService>();
        services.AddHostedService<SupplierQueueConsumerService>();

        services.AddScoped<IPermissionEnforcementToggle, CatalogPermissionEnforcementToggle>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IBalancerSettingsProvider, BalancerSettingsProvider>();
        services.AddScoped<IRoutingDecisionEngine, RoutingDecisionEngine>();
        services.AddScoped<IDecisionContextFactory, DecisionContextFactory>();
        services.AddScoped<ITransferPlanner, TransferPlanner>();
        services.AddScoped<IAllocationOutputQueue, HangfireAllocationOutputQueue>();
        services.AddScoped<IAllocationOutputJob, AllocationOutputJob>();
        services.AddScoped<IShippingRouteRefreshJob, ShippingRouteRefreshJob>();
        services.AddScoped<IOrderQueueHandler, OrderQueueHandler>();
        services.AddScoped<IOcsPaidOrderMapper, OcsPaidOrderMapper>();
        services.AddScoped<ISupplierQueueHandler, SupplierQueueHandler>();

        services.AddScoped<IRequestHandler<AllocateOrderCommand, IReadOnlyCollection<AllocationDto>>, AllocateOrderCommandHandler>();
        services.AddScoped<IRequestHandler<SearchAllocationsQuery, ListResult<AllocationDto>>, SearchAllocationsQueryHandler>();
        services.AddScoped<IRequestHandler<GetAllocationQuery, AllocationDto>, GetAllocationQueryHandler>();
        services.AddScoped<IRequestHandler<EstimateAllocationQuery, EstimateDto>, EstimateAllocationQueryHandler>();
        services.AddScoped<IRequestHandler<RecordLegacyDecisionCommand>, RecordLegacyDecisionCommandHandler>();
        services.AddScoped<IRequestHandler<RecordProductionCommand, bool>, RecordProductionCommandHandler>();
        services.AddScoped<IRequestHandler<MarkReproductionRequestedCommand, bool>, MarkReproductionRequestedCommandHandler>();

        services.AddScoped<IRequestHandler<GetQuoteSetQuery, QuoteSet>, GetQuoteSetQueryHandler>();
        services.AddScoped<IRequestHandler<SearchQuotesQuery, QuoteSearchResult>, SearchQuotesQueryHandler>();

        services.AddScoped<IRequestHandler<SearchOpenItemsQuery, ListResult<OpenItemDto>>, SearchOpenItemsQueryHandler>();
        services.AddScoped<IRequestHandler<GetOpenItemIdsQuery, IReadOnlyCollection<Guid>>, GetOpenItemIdsQueryHandler>();
        services.AddScoped<IRequestHandler<GetProductionPagesQuery, IReadOnlyCollection<string>>, GetProductionPagesQueryHandler>();
        services.AddScoped<IRequestHandler<PreviewTransferQuery, IReadOnlyCollection<TransferItemDto>>, PreviewTransferQueryHandler>();
        services.AddScoped<IRequestHandler<TransferItemsCommand, TransferResultDto>, TransferItemsCommandHandler>();
        services.AddScoped<IRequestHandler<SearchTransfersQuery, ListResult<TransferLogDto>>, SearchTransfersQueryHandler>();

        services.AddScoped<IRequestHandler<GetRoutingDashboardQuery, RoutingDashboardDto>, GetRoutingDashboardQueryHandler>();
        services.AddScoped<IRequestHandler<GetShadowComparisonQuery, ShadowComparisonDto>, GetShadowComparisonQueryHandler>();
        services.AddScoped<IRequestHandler<SearchDimCostQuery, ListResult<DimCostRowDto>>, SearchDimCostQueryHandler>();
        services.AddScoped<IRequestHandler<ExportDimCostQuery, ExcelFile>, ExportDimCostQueryHandler>();
        services.AddScoped<IRequestHandler<GetDimCostDetailsQuery, DimCostDetailsDto>, GetDimCostDetailsQueryHandler>();
        services.AddScoped<IRequestHandler<GetDailyPlanQuery, DailyPlanDto>, GetDailyPlanQueryHandler>();

        services.AddScoped<IRequestHandler<GetSettingsQuery, BalancerSettingsDto>, GetSettingsQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateSettingsCommand, BalancerSettingsDto>, UpdateSettingsCommandHandler>();
        services.AddScoped<IRequestHandler<GetCapacityReservationsQuery, IReadOnlyCollection<CapacityReservationDto>>, GetCapacityReservationsQueryHandler>();
        services.AddScoped<IRequestHandler<SaveCapacityReservationCommand, CapacityReservationDto>, SaveCapacityReservationCommandHandler>();
        services.AddScoped<IRequestHandler<DeleteCapacityReservationCommand>, DeleteCapacityReservationCommandHandler>();

        return services;
    }
}
