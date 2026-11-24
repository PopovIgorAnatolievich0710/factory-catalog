export type SkuProductType = "Manufactured" | "Dropshipper";
export type SkuStatus = "Active" | "Retired";
export type SkuValidationStatus = "Valid" | "Invalid";
export type ComponentUsage = "Static" | "Dynamic";
export type SkuShareSource = "NotProducible" | "Default" | "Override" | "Rebalanced";
export type SkuComponentStockStatus = "Ok" | "Low" | "OutOfStock";
export type ProcessType = "Machine" | "Work";
export type ProductionConflictKind = "ProcessNotListed" | "ProcessNotUsed" | "MachineMismatch";

export const SkuProductTypes = { Manufactured: "Manufactured", Dropshipper: "Dropshipper" } as const;
export const SkuStatuses = { Active: "Active", Retired: "Retired" } as const;
export const SkuValidationStatuses = { Valid: "Valid", Invalid: "Invalid" } as const;
export const SkuShareSources = {
    NotProducible: "NotProducible",
    Default: "Default",
    Override: "Override",
    Rebalanced: "Rebalanced",
} as const;

export interface SkuListItem {
    skuUuid: string;
    siteSku: string;
    factorySkus: string[];
    name: string;
    productType: SkuProductType;
    status: SkuStatus;
    producibleAt: string[];
    validationStatus: SkuValidationStatus;
    productionConflictFactoryIds: string[];
    dropshipperName: string | null;
    lastMasterSyncAt: string | null;
}

export interface SkusSummary {
    total: number;
    dropshipper: number;
    invalid: number;
}

export interface SkuFactoryShare {
    factoryId: string;
    producible: boolean;
    defaultSharePct: number;
    overrideSharePct: number | null;
    effectiveSharePct: number;
    source: SkuShareSource;
    overrideBy: string | null;
    overrideAt: string | null;
}

export interface SkuFactoryShares {
    skuUuid: string;
    factories: SkuFactoryShare[];
    shareTotalPct: number;
}

export interface FactoryShareRow extends SkuFactoryShares {
    siteSku: string;
    name: string;
    status: SkuStatus;
}

export interface SkuAlias {
    id: string;
    type: string;
    deprecated: boolean;
}

export interface SkuComponentFactoryStock {
    factoryId: string;
    totalStock: number;
    stockStatus: SkuComponentStockStatus;
    oosConflict: boolean;
}

export interface SkuComponent {
    erpId: string;
    name: string | null;
    componentStatus: string | null;
    quantity: number;
    usage: ComponentUsage;
    stock: SkuComponentFactoryStock[];
}

export interface SkuProcessFactoryCapacity {
    factoryId: string;
    runsHere: boolean;
    capacityPct: number | null;
    machineType: string | null;
}

export interface SkuProcess {
    processId: string;
    name: string | null;
    type: ProcessType | null;
    measuredIn: string | null;
    quantity: number;
    capacity: SkuProcessFactoryCapacity[];
}

export interface SkuProductionConflictIssue {
    factoryId: string;
    kind: ProductionConflictKind;
    processId: string;
    processName: string | null;
    processType: ProcessType | null;
    machineType: string | null;
    expected: string;
    actual: string;
    orderId: string;
    orderItemId: string;
    seenAt: string;
}

export interface SkuDetails extends SkuFactoryShares {
    siteSku: string;
    name: string;
    productType: SkuProductType;
    status: SkuStatus;
    aliases: SkuAlias[];
    dropshipper: {
        dropshipperId: string | null;
        name: string | null;
        totalCost: { amount: number; currency: string | null } | null;
    } | null;
    validation: { status: SkuValidationStatus; issues: string[] };
    lastMasterSyncAt: string | null;
    components: SkuComponent[];
    processes: SkuProcess[];
    productionConflict: { status: "None" | "Conflict"; issues: SkuProductionConflictIssue[] };
}

export interface FactorySharePct {
    factoryId: string;
    pct: number;
}

export interface GlobalFactoryShare {
    factoryShares: FactorySharePct[];
    totalPct: number;
    updatedBy: string | null;
    updatedAt: string | null;
}

export const FULL_SHARE_PCT = 100;

export const formatPct = (value: number): string => `${Number(value.toFixed(2))}%`;
