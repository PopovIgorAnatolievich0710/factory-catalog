export const SyncRunStatuses = {
    Running: "Running",
    Succeeded: "Succeeded",
    Failed: "Failed",
} as const;

export type SyncRunStatus = (typeof SyncRunStatuses)[keyof typeof SyncRunStatuses];

export interface SyncStatus {
    syncType: string;
    status: SyncRunStatus | null;
    startedAt: string | null;
    finishedAt: string | null;
    lastSuccessAt: string | null;
    added: number;
    updated: number;
    failed: number;
    error: string | null;
}
