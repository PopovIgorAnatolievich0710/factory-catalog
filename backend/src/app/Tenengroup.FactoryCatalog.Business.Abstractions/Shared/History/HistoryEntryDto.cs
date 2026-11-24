using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.History;

public record HistoryEntryDto(
    string ChangedBy,
    DateTime ChangedAt,
    HistorySource Source,
    string? FactoryId,
    string Field,
    string? OldValue,
    string? NewValue);
