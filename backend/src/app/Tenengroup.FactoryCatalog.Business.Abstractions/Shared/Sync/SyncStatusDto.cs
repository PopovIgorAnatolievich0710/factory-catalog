using Tenengroup.FactoryCatalog.Domains.Shared;

namespace Tenengroup.FactoryCatalog.Business.Abstractions.Shared.Sync;

public record SyncStatusDto(
    string SyncType,
    SyncRunStatus? Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    DateTime? LastSuccessAt,
    int Added,
    int Updated,
    int Failed,
    string? Error);
