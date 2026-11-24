using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;

public interface IHistoryWriter
{
    void Record(
        string entityType,
        string entityId,
        string field,
        object? oldValue,
        object? newValue,
        HistorySource source = HistorySource.User,
        string? factoryId = null,
        string? changedBy = null);
}
