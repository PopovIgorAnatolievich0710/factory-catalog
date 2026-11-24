export const ProcessTypes = {
    Machine: "Machine",
    Work: "Work",
} as const;

export type ProcessType = (typeof ProcessTypes)[keyof typeof ProcessTypes];

export const ProcessStatuses = {
    Active: "Active",
    Inactive: "Inactive",
} as const;

export type ProcessStatus = (typeof ProcessStatuses)[keyof typeof ProcessStatuses];

export type ProcessUnit = "Minutes" | "Seconds" | "Hours" | "Units" | "Pieces" | "Cm" | "Mm" | "Cm2" | "Mm2";

export const processUnits: { value: ProcessUnit; label: string; short: string }[] = [
    { value: "Minutes", label: "minutes", short: "min" },
    { value: "Seconds", label: "seconds", short: "sec" },
    { value: "Hours", label: "hours", short: "hr" },
    { value: "Units", label: "units", short: "unit" },
    { value: "Pieces", label: "pieces", short: "piece" },
    { value: "Cm", label: "cm", short: "cm" },
    { value: "Mm", label: "mm", short: "mm" },
    { value: "Cm2", label: "cm²", short: "cm²" },
    { value: "Mm2", label: "mm²", short: "mm²" },
];

export const getUnitLabel = (unit: ProcessUnit): string => processUnits.find(x => x.value === unit)?.label ?? unit;

export const getUnitShortLabel = (unit: ProcessUnit): string => processUnits.find(x => x.value === unit)?.short ?? unit;

export type CapacityStatus = "Offline" | "Reduced" | "Full";

export const FULL_CAPACITY = 100;
export const CAPACITY_STEP = 10;

export interface ProcessToday {
    available: number;
    used: number;
    remainingPct: number;
}

export interface ProcessListItem {
    processId: string;
    name: string;
    type: ProcessType;
    measuredIn: ProcessUnit;
    status: ProcessStatus;
    machineType: string | null;
    runningFactoryIds: string[];
    lowestCapacityPct: number | null;
    avgLaborCost: number | null;
    avgLaborCostCurrency: string | null;
    skusUsing: number;
}

export interface ProcessFactory {
    factoryId: string;
    factoryName: string;
    runsHere: boolean;
    machineType: string | null;
    capacityPct: number;
    capacityStatus: CapacityStatus;
    capacityNote: { reason: string | null; expectedRecovery: string | null } | null;
    capacityUpdatedBy: string | null;
    capacityUpdatedAt: string | null;
    laborCost: { amountPerUnit: number; currency: string | null; effectiveFrom: string | null } | null;
    maxPerDay: number | null;
    today: ProcessToday | null;
}

export interface ProcessSku {
    skuUuid: string;
    name: string;
    siteSku: string;
    quantity: number;
}

export interface ProcessDetails {
    processId: string;
    name: string;
    type: ProcessType;
    measuredIn: ProcessUnit;
    description: string | null;
    status: ProcessStatus;
    lastUpdate: string;
    factories: ProcessFactory[];
    skus: ProcessSku[];
}

export interface ProcessSpecification {
    name: string;
    type: ProcessType;
    measuredIn: ProcessUnit;
    status: ProcessStatus;
    description: string | null;
}

export interface CreateProcessRequest {
    processId: string | null;
    name: string;
    type: ProcessType;
    measuredIn: ProcessUnit;
    description: string | null;
}

export interface ProcessFactorySetup {
    runsHere: boolean;
    machineType: string | null;
    laborCostAmount: number | null;
    laborCostCurrency: string | null;
    maxPerDay: number | null;
}

export interface CapacityCell {
    factoryId: string;
    runsHere: boolean;
    capacityPct: number;
    status: CapacityStatus;
    reason: string | null;
    expectedRecovery: string | null;
    updatedBy: string | null;
    updatedAt: string | null;
    maxPerDay: number | null;
    today: ProcessToday | null;
}

export interface CapacityRow {
    processId: string;
    name: string;
    type: ProcessType;
    cells: CapacityCell[];
}

export interface SetCapacityRequest {
    capacityPct: number;
    reason: string | null;
    expectedRecovery: string | null;
}

export interface ProcessImportResult {
    totalRows: number;
    processesCreated: number;
    processesUpdated: number;
    factoryRowsChanged: number;
}
