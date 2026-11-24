using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Processes.Import;

public class ValidateProcessImportQueryHandler(ProcessImportPlanner planner)
    : IRequestHandler<ValidateProcessImportQuery, ProcessImportValidationDto>
{
    public async Task<ProcessImportValidationDto> Handle(ValidateProcessImportQuery request, CancellationToken cancellationToken)
    {
        return (await planner.Build(request.Content, cancellationToken)).ToValidation();
    }
}

public class ImportProcessesCommandHandler(
    ProcessImportPlanner planner,
    IFactoryCatalogDbContext db,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<ImportProcessesCommand, ProcessImportResultDto>
{
    private const string FileField = "file";

    public async Task<ProcessImportResultDto> Handle(ImportProcessesCommand request, CancellationToken cancellationToken)
    {
        var plan = await planner.Build(request.Content, cancellationToken);

        if (plan.Errors.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, List<string>>
            {
                [FileField] = plan.Errors.Select(e => $"Row {e.Row}{(e.Field == null ? null : $" ({e.Field})")}: {e.Message}").ToList()
            });
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var processes = plan.ExistingProcesses.ToDictionary(p => p.ProcessId);
        var factoryRows = await db.ProcessFactories.ToListAsync(cancellationToken);
        var created = 0;
        var updated = 0;
        var factoryRowsChanged = 0;

        foreach (var item in plan.Processes)
        {
            if (processes.TryGetValue(item.ProcessId, out var process))
            {
                if (ProcessChanges.ApplySpecification(process, factoryRows, item.Specification, historyWriter, HistorySource.Import, now))
                {
                    updated++;
                }

                continue;
            }

            db.Processes.Add(new Process
            {
                ProcessId = item.ProcessId,
                Name = item.Specification.Name,
                Type = item.Specification.Type,
                MeasuredIn = item.Specification.MeasuredIn,
                Description = item.Specification.Description,
                Status = item.Specification.Status,
                CreatedAt = now,
                UpdatedAt = now
            });
            historyWriter.Record(
                HistoryEntityTypes.Process, item.ProcessId, ProcessChanges.CreatedField, null, ProcessChanges.CreatedValue, HistorySource.Import);
            created++;
        }

        foreach (var item in plan.Factories)
        {
            var row = factoryRows.FirstOrDefault(f => f.ProcessId == item.ProcessId && f.FactoryId == item.FactoryId);
            var isNew = row == null;
            row ??= ProcessChanges.NewFactoryRow(item.ProcessId, item.FactoryId, now);

            if (!ProcessChanges.ApplyFactorySetup(row, item.Setup, historyWriter, HistorySource.Import, now))
            {
                continue;
            }

            if (isNew)
            {
                db.ProcessFactories.Add(row);
                factoryRows.Add(row);
            }

            factoryRowsChanged++;
        }

        if (created + updated + factoryRowsChanged > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        return new ProcessImportResultDto(plan.TotalRows, created, updated, factoryRowsChanged);
    }
}
