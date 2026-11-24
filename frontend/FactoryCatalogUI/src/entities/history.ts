export type HistorySource = "User" | "Sync" | "Import" | "System";

export interface HistoryEntry {
    changedBy: string;
    changedAt: string;
    source: HistorySource;
    factoryId: string | null;
    field: string;
    oldValue: string | null;
    newValue: string | null;
}
