using Tenengroup.FactoryCatalog.Business.Abstractions.Skus.FactoryShare;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Skus.FactoryShare;

public class ExportFactoryShareQueryHandler(
    FactoryShareListReader reader,
    SkuFactoryShareService shareService,
    TimeProvider timeProvider)
    : IRequestHandler<ExportFactoryShareQuery, ExcelFile>
{
    public const string PageName = "factory-share";

    public async Task<ExcelFile> Handle(ExportFactoryShareQuery request, CancellationToken cancellationToken)
    {
        var rows = await reader.Read(request.Request, paged: false, cancellationToken);
        var columns = new List<ExcelColumn<FactoryShareRowDto>>
        {
            new("siteSku", "Site SKU", r => r.SiteSku),
            new("name", "SKU name", r => r.Name),
            new("status", "Status", r => r.Status.ToString())
        };
        foreach (var factoryId in await shareService.GetActiveFactoryIds(cancellationToken))
        {
            columns.Add(new ExcelColumn<FactoryShareRowDto>(
                $"producible.{factoryId}", $"{factoryId} producible", r => r.Factories.First(f => f.FactoryId == factoryId).Producible ? "Yes" : "No"));
            columns.Add(new ExcelColumn<FactoryShareRowDto>(
                $"share.{factoryId}", $"{factoryId} share %", r => r.Factories.First(f => f.FactoryId == factoryId).EffectiveSharePct));
            columns.Add(new ExcelColumn<FactoryShareRowDto>(
                $"source.{factoryId}", $"{factoryId} source", r => r.Factories.First(f => f.FactoryId == factoryId).Source.ToString()));
        }

        columns.Add(new ExcelColumn<FactoryShareRowDto>("total", "Total %", r => r.ShareTotalPct));

        return ExcelExporter.Export(PageName, rows.Items, columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
