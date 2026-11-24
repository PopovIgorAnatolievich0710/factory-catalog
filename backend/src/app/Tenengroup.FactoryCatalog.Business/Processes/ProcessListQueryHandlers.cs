using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class SearchProcessesQueryHandler(ProcessListReader reader)
    : IRequestHandler<SearchProcessesQuery, ListResult<ProcessListItemDto>>
{
    public async Task<ListResult<ProcessListItemDto>> Handle(SearchProcessesQuery request, CancellationToken cancellationToken)
    {
        var items = await reader.Read(request.Request, cancellationToken);

        return new ListResult<ProcessListItemDto>(items.ApplyPaging(request.Request).ToList(), items.Count());
    }
}

public class ExportProcessesQueryHandler(ProcessListReader reader, TimeProvider timeProvider)
    : IRequestHandler<ExportProcessesQuery, ExcelFile>
{
    private static readonly ExcelColumn<ProcessListItemDto>[] Columns =
    [
        new("processId", "Process ID", p => p.ProcessId),
        new("name", "Process", p => p.Name),
        new("type", "Type", p => p.Type.ToString()),
        new("machineType", "Machine type", p => p.MachineType),
        new(ProcessListReader.RunningFactoriesField, "Factories running it", p => string.Join(", ", p.RunningFactoryIds)),
        new("lowestCapacityPct", "Lowest capacity today (%)", p => p.LowestCapacityPct),
        new("avgLaborCost", "Avg labor cost", p => p.AvgLaborCost),
        new("avgLaborCostCurrency", "Labor cost currency", p => p.AvgLaborCostCurrency),
        new("skusUsing", "SKUs using", p => p.SkusUsing),
        new("measuredIn", "Measured in", p => p.MeasuredIn.ToString()),
        new("status", "Status", p => p.Status.ToString())
    ];

    public async Task<ExcelFile> Handle(ExportProcessesQuery request, CancellationToken cancellationToken)
    {
        var items = await reader.Read(request.Request, cancellationToken);

        return ExcelExporter.Export("processes", items.ToList(), Columns, request.Request.Columns, timeProvider.GetUtcNow().UtcDateTime);
    }
}
