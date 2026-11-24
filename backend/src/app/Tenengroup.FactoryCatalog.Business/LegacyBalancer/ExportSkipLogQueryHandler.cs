using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.LegacyBalancer;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.LegacyBalancer;

public class ExportSkipLogQueryHandler(IFactoryCatalogDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ExportSkipLogQuery, ExcelFile>
{
    private const string PageName = "balancer_skip_log";

    private static readonly ExcelColumn<SkipLogEntryDto>[] Columns =
    [
        new("createdAt", "Logged at", l => l.CreatedAt),
        new("orderId", "Order ID", l => l.OrderId),
        new("orderItemId", "Order item ID", l => l.OrderItemId),
        new("skuUuid", "SKU UUID", l => l.SkuUuid?.ToString()),
        new("factoryId", "Factory", l => l.FactoryId),
        new("reason", "Reason", l => l.Reason.ToString()),
        new("componentErpId", "Component ERP ID", l => l.ComponentErpId),
        new("processId", "Process ID", l => l.ProcessId)
    ];

    public async Task<ExcelFile> Handle(ExportSkipLogQuery request, CancellationToken cancellationToken)
    {
        var rows = await SkipLogList.Query(db, request.Request).ToListAsync(cancellationToken);

        return ExcelExporter.Export(PageName, rows, Columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
