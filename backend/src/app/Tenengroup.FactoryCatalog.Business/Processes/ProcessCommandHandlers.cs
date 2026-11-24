using Microsoft.EntityFrameworkCore;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;
using Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Users;
using Tenengroup.FactoryCatalog.DataAccess.Abstractions;
using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.FactoryCatalog.Domains.Shared;
using Tenengroup.Infrastructure.Common.Exceptions;
using Tenengroup.Infrastructure.Common.Mediators;

namespace Tenengroup.FactoryCatalog.Business.Processes;

public class CreateProcessCommandHandler(IFactoryCatalogDbContext db, IHistoryWriter historyWriter, TimeProvider timeProvider)
    : IRequestHandler<CreateProcessCommand, string>
{
    public async Task<string> Handle(CreateProcessCommand request, CancellationToken cancellationToken)
    {
        var specification = ProcessSpecification.Create(
            request.Name, request.Type, request.MeasuredIn, ProcessStatus.Active, request.Description);
        var processId = string.IsNullOrWhiteSpace(request.ProcessId)
            ? ProcessRules.DeriveProcessId(specification.Name)
            : ProcessRules.NormalizeProcessId(request.ProcessId);

        var errors = ProcessRules.SpecificationErrors(processId, specification);
        if (specification.Name.Length > 0)
        {
            errors.AddRange(ProcessRules.ProcessIdErrors(processId));
        }

        ProcessRules.ThrowIfAny(errors);

        if (await db.Processes.AnyAsync(p => p.ProcessId == processId, cancellationToken))
        {
            throw new ValidationException(ProcessRules.ProcessIdField, $"Process '{processId}' already exists.");
        }

        var name = specification.Name.ToLower();
        if (await db.Processes.AnyAsync(p => p.Name.ToLower() == name, cancellationToken))
        {
            throw new ValidationException(ProcessRules.NameField, $"A process named '{specification.Name}' already exists.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.Processes.Add(new Process
        {
            ProcessId = processId,
            Name = specification.Name,
            Type = specification.Type,
            MeasuredIn = specification.MeasuredIn,
            Description = specification.Description,
            Status = specification.Status,
            CreatedAt = now,
            UpdatedAt = now
        });
        historyWriter.Record(HistoryEntityTypes.Process, processId, ProcessChanges.CreatedField, null, ProcessChanges.CreatedValue);

        await db.SaveChangesAsync(cancellationToken);

        return processId;
    }
}

public class UpdateProcessCommandHandler(IFactoryCatalogDbContext db, IHistoryWriter historyWriter, TimeProvider timeProvider)
    : IRequestHandler<UpdateProcessCommand>
{
    public async Task Handle(UpdateProcessCommand request, CancellationToken cancellationToken)
    {
        var process = await db.Processes.FirstOrDefaultAsync(p => p.ProcessId == request.ProcessId, cancellationToken)
                      ?? throw new EntityNotFoundException("Process", request.ProcessId);

        var specification = ProcessSpecification.Create(
            request.Name, request.Type, request.MeasuredIn, request.Status, request.Description);
        ProcessRules.ThrowIfAny(ProcessRules.SpecificationErrors(process.ProcessId, specification));

        var name = specification.Name.ToLower();
        if (await db.Processes.AnyAsync(p => p.ProcessId != process.ProcessId && p.Name.ToLower() == name, cancellationToken))
        {
            throw new ValidationException(ProcessRules.NameField, $"A process named '{specification.Name}' already exists.");
        }

        var factoryRows = await db.ProcessFactories
            .Where(f => f.ProcessId == process.ProcessId)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (ProcessChanges.ApplySpecification(process, factoryRows, specification, historyWriter, HistorySource.User, now))
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public class UpdateProcessFactorySetupCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<UpdateProcessFactorySetupCommand>
{
    public async Task Handle(UpdateProcessFactorySetupCommand request, CancellationToken cancellationToken)
    {
        await currentUser.EnsureCanEditFactory(request.FactoryId, cancellationToken);

        var process = await db.Processes
                          .AsNoTracking()
                          .FirstOrDefaultAsync(p => p.ProcessId == request.ProcessId, cancellationToken)
                      ?? throw new EntityNotFoundException("Process", request.ProcessId);

        if (!await db.Factories.AnyAsync(f => f.Id == request.FactoryId, cancellationToken))
        {
            throw new EntityNotFoundException("Factory", request.FactoryId);
        }

        var setup = ProcessFactorySetup.Create(
            request.RunsHere, request.MachineType, request.LaborCostAmount, request.LaborCostCurrency, request.MaxPerDay);
        ProcessRules.ThrowIfAny(ProcessRules.FactorySetupErrors(process.Type, setup));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var row = await db.ProcessFactories.FirstOrDefaultAsync(
            f => f.ProcessId == process.ProcessId && f.FactoryId == request.FactoryId, cancellationToken);

        if (row == null)
        {
            row = ProcessChanges.NewFactoryRow(process.ProcessId, request.FactoryId, now);
            db.ProcessFactories.Add(row);
        }

        if (ProcessChanges.ApplyFactorySetup(row, setup, historyWriter, HistorySource.User, now))
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public class SetProcessCapacityCommandHandler(
    IFactoryCatalogDbContext db,
    ICurrentUser currentUser,
    IHistoryWriter historyWriter,
    TimeProvider timeProvider) : IRequestHandler<SetProcessCapacityCommand>
{
    public async Task Handle(SetProcessCapacityCommand request, CancellationToken cancellationToken)
    {
        await currentUser.EnsureCanEditFactory(request.FactoryId, cancellationToken);

        if (!ProcessCapacityCalculator.IsValidCapacity(request.CapacityPct))
        {
            throw new ValidationException(
                ProcessRules.CapacityPctField,
                $"Capacity must be between 0 and {ProcessFactory.FullCapacityPct} in steps of {ProcessFactory.CapacityStepPct}.");
        }

        var isReduced = request.CapacityPct < ProcessFactory.FullCapacityPct;
        var reason = isReduced ? ProcessRules.NullIfBlank(request.Reason) : null;
        var expectedRecovery = isReduced ? request.ExpectedRecovery : null;

        if (reason?.Length > ProcessRules.CapacityReasonMaxLength)
        {
            throw new ValidationException(
                "reason", $"Reason must be at most {ProcessRules.CapacityReasonMaxLength} characters.");
        }

        if (!await db.Processes.AnyAsync(p => p.ProcessId == request.ProcessId, cancellationToken))
        {
            throw new EntityNotFoundException("Process", request.ProcessId);
        }

        var row = await db.ProcessFactories.FirstOrDefaultAsync(
            f => f.ProcessId == request.ProcessId && f.FactoryId == request.FactoryId, cancellationToken);

        if (row is not { RunsHere: true })
        {
            throw new ValidationException(
                ProcessRules.FactoryIdField,
                $"Process '{request.ProcessId}' does not run at factory '{request.FactoryId}'.");
        }

        if (row.CapacityPct == request.CapacityPct && row.CapacityReason == reason && row.ExpectedRecovery == expectedRecovery)
        {
            return;
        }

        if (row.CapacityPct != request.CapacityPct)
        {
            historyWriter.Record(
                HistoryEntityTypes.Process,
                row.ProcessId,
                ProcessRules.CapacityPctField,
                $"{row.CapacityPct}%",
                reason == null ? $"{request.CapacityPct}%" : $"{request.CapacityPct}% (reason: {reason})",
                factoryId: row.FactoryId);
        }

        historyWriter.Record(
            HistoryEntityTypes.Process, row.ProcessId, ProcessRules.CapacityReasonField, row.CapacityReason, reason, factoryId: row.FactoryId);
        historyWriter.Record(
            HistoryEntityTypes.Process, row.ProcessId, ProcessRules.ExpectedRecoveryField, row.ExpectedRecovery, expectedRecovery, factoryId: row.FactoryId);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        row.CapacityPct = request.CapacityPct;
        row.CapacityReason = reason;
        row.ExpectedRecovery = expectedRecovery;
        row.CapacityUpdatedBy = string.IsNullOrEmpty(currentUser.Email) ? currentUser.Name : currentUser.Email;
        row.CapacityUpdatedAt = now;
        row.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }
}
