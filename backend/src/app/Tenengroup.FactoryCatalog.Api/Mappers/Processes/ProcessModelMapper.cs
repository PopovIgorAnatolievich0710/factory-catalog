using Tenengroup.FactoryCatalog.Api.Models.Processes;
using Tenengroup.FactoryCatalog.Business.Abstractions.Processes;
using Tenengroup.Infrastructure.Common.Exceptions;

namespace Tenengroup.FactoryCatalog.Api.Mappers.Processes;

public static class ProcessModelMapper
{
    public static CreateProcessCommand ToCommand(this CreateProcessModel model)
    {
        return new CreateProcessCommand(model.ProcessId, model.Name, model.Type!.Value, model.MeasuredIn!.Value, model.Description);
    }

    public static UpdateProcessCommand ToCommand(this UpdateProcessModel model, string processId)
    {
        return new UpdateProcessCommand(
            processId, model.Name, model.Type!.Value, model.MeasuredIn!.Value, model.Status!.Value, model.Description);
    }

    public static UpdateProcessFactorySetupCommand ToCommand(this UpdateProcessFactorySetupModel model, string processId, string factoryId)
    {
        return new UpdateProcessFactorySetupCommand(
            processId,
            factoryId,
            model.RunsHere!.Value,
            model.MachineType,
            model.LaborCostAmount,
            model.LaborCostCurrency,
            model.MaxPerDay);
    }

    public static SetProcessCapacityCommand ToCommand(this SetProcessCapacityModel model, string processId, string factoryId)
    {
        return new SetProcessCapacityCommand(processId, factoryId, model.CapacityPct!.Value, model.Reason, model.ExpectedRecovery);
    }

    public static async Task<byte[]> ToContent(this IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException("file", "An .xlsx file is required.");
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);

        return stream.ToArray();
    }
}
