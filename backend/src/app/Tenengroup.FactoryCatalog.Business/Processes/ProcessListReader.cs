using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.Infrastructure.Common.Paging;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class ProcessListReader(IFactoryCatalogDbContext db, ICurrentUser currentUser)
{
    public const string RunningFactoriesField = "runningFactoryIds";

    private const string DefaultSortBy = "processId";

    private static readonly Dictionary<string, Expression<Func<ProcessListItemDto, object>>> SortableFields = new()
    {
        [DefaultSortBy] = p => p.ProcessId,
        ["name"] = p => p.Name,
        ["type"] = p => p.Type,
        ["measuredIn"] = p => p.MeasuredIn,
        ["status"] = p => p.Status,
        ["machineType"] = p => p.MachineType,
        [RunningFactoriesField] = p => p.RunningFactoryIds.Count,
        ["lowestCapacityPct"] = p => p.LowestCapacityPct,
        ["avgLaborCost"] = p => p.AvgLaborCost,
        ["skusUsing"] = p => p.SkusUsing
    };

    public async Task<IQueryable<ProcessListItemDto>> Read(ListRequest request, CancellationToken cancellationToken)
    {
        var canViewCost = await currentUser.HasPermission(Permission.ProcessesViewCost, cancellationToken);

        var processes = await db.Processes.AsNoTracking().ToListAsync(cancellationToken);
        var activeFactoryIds = await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);
        var runningRows = (await db.ProcessFactories
                .AsNoTracking()
                .Where(f => f.RunsHere)
                .ToListAsync(cancellationToken))
            .Where(f => activeFactoryIds.Contains(f.FactoryId))
            .OrderBy(f => f.FactoryId)
            .ToLookup(f => f.ProcessId);
        var skuCounts = (await db.SkuProcesses
                .AsNoTracking()
                .GroupBy(s => s.ProcessId)
                .Select(g => new { ProcessId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.ProcessId, s => s.Count);

        var items = processes
            .Select(p => ToListItem(p, runningRows[p.ProcessId].ToList(), skuCounts.GetValueOrDefault(p.ProcessId), canViewCost))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            items = items.Where(p =>
                p.ProcessId.Contains(search, StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var factoriesFilter = request.Filter(RunningFactoriesField)?.Values;
        if (factoriesFilter is { Length: > 0 })
        {
            items = items.Where(p => p.RunningFactoryIds.Any(id => factoriesFilter.Contains(id)));
        }

        return items
            .ApplyFilter(request, "processId", p => p.ProcessId)
            .ApplyFilter(request, "name", p => p.Name)
            .ApplyFilter(request, "type", p => p.Type)
            .ApplyFilter(request, "measuredIn", p => p.MeasuredIn)
            .ApplyFilter(request, "status", p => p.Status)
            .ApplyFilter(request, "machineType", p => p.MachineType)
            .ApplyFilter(request, "lowestCapacityPct", p => p.LowestCapacityPct)
            .ApplyFilter(request, "avgLaborCost", p => p.AvgLaborCost)
            .ApplyFilter(request, "skusUsing", p => p.SkusUsing)
            .ApplySorting(request, SortableFields, DefaultSortBy);
    }

    private static ProcessListItemDto ToListItem(Process process, List<ProcessFactory> runningRows, int skusUsing, bool canViewCost)
    {
        var machineTypes = runningRows
            .Select(r => r.MachineType)
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct()
            .ToList();
        var costRows = runningRows.Where(r => r.LaborCostAmount != null).ToList();
        var currencies = costRows.Select(r => r.LaborCostCurrency).Distinct().ToList();
        var hasAverage = canViewCost && currencies.Count == 1;

        return new ProcessListItemDto(
            process.ProcessId,
            process.Name,
            process.Type,
            process.MeasuredIn,
            process.Status,
            process.Type == ProcessType.Machine && machineTypes.Count > 0 ? string.Join(", ", machineTypes) : null,
            runningRows.Select(r => r.FactoryId).ToList(),
            runningRows.Count == 0 ? null : runningRows.Min(r => r.CapacityPct),
            hasAverage ? Math.Round(costRows.Average(r => r.LaborCostAmount.Value), 4) : null,
            hasAverage ? currencies[0] : null,
            skusUsing);
    }
}
