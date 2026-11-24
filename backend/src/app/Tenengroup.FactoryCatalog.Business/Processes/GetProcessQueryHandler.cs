using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Permissions;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class GetProcessQueryHandler(IFactoryCatalogDbContext db, IProcessUsageReader usageReader, ICurrentUser currentUser)
    : IRequestHandler<GetProcessQuery, ProcessDto>
{
    public async Task<ProcessDto> Handle(GetProcessQuery request, CancellationToken cancellationToken)
    {
        var process = await db.Processes
                          .AsNoTracking()
                          .FirstOrDefaultAsync(p => p.ProcessId == request.ProcessId, cancellationToken)
                      ?? throw new EntityNotFoundException("Process", request.ProcessId);

        var canViewCost = await currentUser.HasPermission(Permission.ProcessesViewCost, cancellationToken);
        var factories = await db.Factories
            .AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Id)
            .ToListAsync(cancellationToken);
        var rows = (await db.ProcessFactories
                .AsNoTracking()
                .Where(f => f.ProcessId == process.ProcessId)
                .ToListAsync(cancellationToken))
            .ToDictionary(f => f.FactoryId);
        var used = await usageReader.GetUsedToday(cancellationToken);

        var quantities = (await db.SkuProcesses
                .AsNoTracking()
                .Where(s => s.ProcessId == process.ProcessId)
                .Select(s => new { s.SkuUuid, s.Quantity })
                .ToListAsync(cancellationToken))
            .ToDictionary(s => s.SkuUuid, s => s.Quantity);
        var skuUuids = quantities.Keys.ToList();
        var skus = await db.Skus
            .AsNoTracking()
            .Where(s => skuUuids.Contains(s.SkuUuid))
            .OrderBy(s => s.SiteSku)
            .Select(s => new { s.SkuUuid, s.Name, s.SiteSku })
            .ToListAsync(cancellationToken);

        return new ProcessDto(
            process.ProcessId,
            process.Name,
            process.Type,
            process.MeasuredIn,
            process.Description,
            process.Status,
            rows.Values.Select(r => r.UpdatedAt).Append(process.UpdatedAt).Max(),
            factories
                .Select(f => ProcessMapper.ToFactoryDto(
                    f,
                    rows.GetValueOrDefault(f.Id),
                    used.GetValueOrDefault(new ProcessFactoryKey(process.ProcessId, f.Id)),
                    canViewCost))
                .ToList(),
            skus.Select(s => new ProcessSkuDto(s.SkuUuid, s.Name, s.SiteSku, quantities[s.SkuUuid])).ToList());
    }
}
