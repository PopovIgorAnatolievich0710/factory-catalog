export const ApiStockStatuses = {
    Ok: "Ok",
    Low: "Low",
    OutOfStock: "OutOfStock",
    OosConflict: "OosConflict",
} as const;

export type ApiStockStatus = (typeof ApiStockStatuses)[keyof typeof ApiStockStatuses];

export const ComponentStatuses = {
    Active: "Active",
    Inactive: "Inactive",
} as const;

export type ComponentStatus = (typeof ComponentStatuses)[keyof typeof ComponentStatuses];

export const ComponentUsages = {
    Static: "Static",
    Dynamic: "Dynamic",
} as const;

export type ComponentUsage = (typeof ComponentUsages)[keyof typeof ComponentUsages];

export interface ComponentListItem {
    erpId: string;
    name: string;
    family: string | null;
    material: string | null;
    componentStatus: ComponentStatus;
    status: ApiStockStatus;
    statusFactoryIds: string[];
    actionRequired: boolean;
    totalStock: number;
    criticalLevel: number | null;
    hasCriticalLevelOverrides: boolean;
    unitCost: number | null;
    unitCostCurrency: string | null;
    skusUsing: number;
    stockByFactory: Record<string, number>;
    lastSapSyncAt: string | null;
}

export interface ComponentManualOos {
    active: boolean;
    setBy: string | null;
    setAt: string | null;
    note: string | null;
}

export interface ComponentFactoryStock {
    factoryId: string;
    warehouseStock: number;
    productionFloorStock: number;
    totalStock: number;
    criticalLevel: number | null;
    criticalLevelOverride: number | null;
    stockStatus: ApiStockStatus;
    oosConflict: boolean;
    status: ApiStockStatus;
    manualOos: ComponentManualOos;
    stockSyncedAt: string | null;
}

export interface ComponentAlternativeStock {
    factoryId: string;
    totalStock: number;
    status: ApiStockStatus;
}

export interface ComponentAlternative {
    erpId: string;
    name: string;
    status: ComponentStatus;
    forBalancer: boolean;
    forPlanner: boolean;
    factories: ComponentAlternativeStock[];
}

export interface ComponentSkuUsage {
    skuUuid: string;
    name: string;
    siteSku: string;
    quantity: number;
    usage: ComponentUsage;
}

export interface ComponentDetails {
    erpId: string;
    name: string;
    status: ComponentStatus;
    family: string | null;
    material: string | null;
    measure: { measuredBy: string | null; value: number | null; unit: string | null };
    unitOfMeasure: { type: string | null; quantity: number | null; unit: string | null };
    countryOfOrigin: string | null;
    hsCode: string | null;
    unitCost: { amount: number | null; currency: string | null; source: string | null } | null;
    criticalStockLevel: number | null;
    stockStatus: ApiStockStatus;
    stockStatusFactoryIds: string[];
    actionRequired: boolean;
    factories: ComponentFactoryStock[];
    alternatives: ComponentAlternative[];
    usedInSkus: ComponentSkuUsage[];
    lastSapSyncAt: string | null;
}

export interface ComponentFilterOptions {
    families: string[];
    materials: string[];
}

export interface ComponentStockLevel {
    erpId: string;
    name: string;
    family: string | null;
    factoryId: string;
    warehouseStock: number;
    productionFloorStock: number;
    totalStock: number;
    criticalLevel: number | null;
    isCriticalLevelOverride: boolean;
    status: ApiStockStatus;
    manualOosSetBy: string | null;
    manualOosSetAt: string | null;
    manualOosNote: string | null;
}
