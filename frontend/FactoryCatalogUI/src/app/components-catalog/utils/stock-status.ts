import { StockStatus, StockStatuses } from "../../../entities/status";
import { ApiStockStatus, ApiStockStatuses } from "../entities/component";

const kitStatuses: Record<ApiStockStatus, StockStatus> = {
    [ApiStockStatuses.Ok]: StockStatuses.Ok,
    [ApiStockStatuses.Low]: StockStatuses.Low,
    [ApiStockStatuses.OutOfStock]: StockStatuses.OutOfStock,
    [ApiStockStatuses.OosConflict]: StockStatuses.OosConflict,
};

export const toKitStockStatus = (status: ApiStockStatus): StockStatus => kitStatuses[status] ?? StockStatuses.Ok;

export const stockStatusFilterValues = [
    { value: ApiStockStatuses.Ok, label: "OK" },
    { value: ApiStockStatuses.Low, label: "Low" },
    { value: ApiStockStatuses.OutOfStock, label: "OOS" },
    { value: ApiStockStatuses.OosConflict, label: "OOS conflict" },
];

export const OOS_CONFLICT_EXPLANATION = "Stock is above the critical level but the manual OOS flag is still on";
