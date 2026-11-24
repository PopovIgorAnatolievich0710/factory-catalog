using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class ProcessCapacityReader(IFactoryCatalogDbContext db, IProcessUsageReader usageReader)
{
    public async Task<List<ProcessCapacityRowDto>> Read(List<string> factoryIds, CancellationToken cancellationToken)
    {
        var processes = await db.Processes
            .AsNoTracking()
            .Where(p => p.Status == ProcessStatus.Active)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
        var rows = (await db.ProcessFactories
                .AsNoTracking()
                .Where(f => factoryIds.Contains(f.FactoryId))
                .ToListAsync(cancellationToken))
            .ToDictionary(f => new ProcessFactoryKey(f.ProcessId, f.FactoryId));
        var used = await usageReader.GetUsedToday(cancellationToken);

        return processes
            .Select(p => new ProcessCapacityRowDto(
                p.ProcessId,
                p.Name,
                p.Type,
                factoryIds
                    .Select(factoryId => new ProcessFactoryKey(p.ProcessId, factoryId))
                    .Select(key => ProcessMapper.ToCapacityCell(key.FactoryId, rows.GetValueOrDefault(key), used.GetValueOrDefault(key)))
                    .ToList()))
            .ToList();
    }
}

public class GetProcessCapacityOverviewQueryHandler(IFactoryCatalogDbContext db, ProcessCapacityReader reader)
    : IRequestHandler<GetProcessCapacityOverviewQuery, IReadOnlyCollection<ProcessCapacityRowDto>>
{
    public async Task<IReadOnlyCollection<ProcessCapacityRowDto>> Handle(
        GetProcessCapacityOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var factoryIds = await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        return await reader.Read(factoryIds, cancellationToken);
    }
}

public class GetFactoryProcessCapacityQueryHandler(IFactoryCatalogDbContext db, ProcessCapacityReader reader)
    : IRequestHandler<GetFactoryProcessCapacityQuery, IReadOnlyCollection<ProcessCapacityRowDto>>
{
    public async Task<IReadOnlyCollection<ProcessCapacityRowDto>> Handle(
        GetFactoryProcessCapacityQuery request,
        CancellationToken cancellationToken)
    {
        if (!await db.Factories.AnyAsync(f => f.Id == request.FactoryId, cancellationToken))
        {
            throw new EntityNotFoundException("Factory", request.FactoryId);
        }

        return (await reader.Read(new List<string> { request.FactoryId }, cancellationToken))
            .Where(p => p.Cells.Single().RunsHere)
            .ToList();
    }
}
