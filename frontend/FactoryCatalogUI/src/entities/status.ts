export const StockStatuses = {
    Ok: "ok",
    Low: "low",
    OutOfStock: "outOfStock",
    OosConflict: "oosConflict",
} as const;

export type StockStatus = (typeof StockStatuses)[keyof typeof StockStatuses];
