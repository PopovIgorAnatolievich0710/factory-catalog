namespace Tenengroup.FactoryCatalog.Business.Abstractions.Processes;

public interface IProcessUsageReader
{
    Task<IReadOnlyDictionary<ProcessFactoryKey, int>> GetUsedToday(CancellationToken cancellationToken);
}

public readonly record struct ProcessFactoryKey(string ProcessId, string FactoryId);
