using Tenengroup.FactoryCatalog.Domains.Processes;
using Tenengroup.Infrastructure.Common.Excel;
using Tenengroup.Infrastructure.Common.Mediators;
using Tenengroup.Infrastructure.Common.Paging;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public record SearchProcessesQuery(ListRequest Request) : IRequest<ListResult<ProcessListItemDto>>;

public record ExportProcessesQuery(ExportRequest Request) : IRequest<ExcelFile>;

public record GetProcessQuery(string ProcessId) : IRequest<ProcessDto>;

public record CreateProcessCommand(
    string? ProcessId,
    string Name,
    ProcessType Type,
    ProcessUnit MeasuredIn,
    string? Description) : IRequest<string>;

public record UpdateProcessCommand(
    string ProcessId,
    string Name,
    ProcessType Type,
    ProcessUnit MeasuredIn,
    ProcessStatus Status,
    string? Description) : IRequest;

public record UpdateProcessFactorySetupCommand(
    string ProcessId,
    string FactoryId,
    bool RunsHere,
    string? MachineType,
    decimal? LaborCostAmount,
    string? LaborCostCurrency,
    decimal? MaxPerDay) : IRequest;

public record SetProcessCapacityCommand(
    string ProcessId,
    string FactoryId,
    int CapacityPct,
    string? Reason,
    DateOnly? ExpectedRecovery) : IRequest;

public record GetProcessCapacityOverviewQuery : IRequest<IReadOnlyCollection<ProcessCapacityRowDto>>;

public record GetFactoryProcessCapacityQuery(string FactoryId) : IRequest<IReadOnlyCollection<ProcessCapacityRowDto>>;

public record ValidateProcessImportQuery(byte[] Content) : IRequest<ProcessImportValidationDto>;

public record ImportProcessesCommand(byte[] Content) : IRequest<ProcessImportResultDto>;
