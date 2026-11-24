import { ColumnFilter, FilterOperators, ListFilters, SortDirection } from "../entities/list";

export type CellValue = string | number | boolean | string[] | null | undefined;

export const isFilterActive = (filter: ColumnFilter | undefined): boolean => {
    if (!filter) return false;
    if (filter.values && filter.values.length > 0) return true;
    return Boolean(filter.operator) && filter.value !== undefined && filter.value !== "";
};

export const countActiveFilters = (filters: ListFilters): number =>
    Object.values(filters).filter(isFilterActive).length;

const matchesNumber = (value: CellValue, filter: ColumnFilter): boolean => {
    const actual = typeof value === "number" ? value : Number.parseFloat(String(value));
    const from = Number.parseFloat(filter.value);
    const to = Number.parseFloat(filter.valueTo ?? "");
    if (Number.isNaN(actual) || Number.isNaN(from)) return false;

    switch (filter.operator) {
        case FilterOperators.Equal: return actual === from;
        case FilterOperators.NotEqual: return actual !== from;
        case FilterOperators.LessThan: return actual < from;
        case FilterOperators.LessThanOrEqual: return actual <= from;
        case FilterOperators.GreaterThan: return actual > from;
        case FilterOperators.GreaterThanOrEqual: return actual >= from;
        case FilterOperators.Between: return Number.isNaN(to) ? actual >= from : actual >= from && actual <= to;
        default: return true;
    }
};

export const matchesFilter = (value: CellValue, filter: ColumnFilter): boolean => {
    if (!isFilterActive(filter)) return true;

    if (filter.values && filter.values.length > 0) {
        const actualValues = Array.isArray(value) ? value : [value];
        return actualValues.some(actual => actual != null && filter.values.includes(String(actual)));
    }
    if (filter.operator === FilterOperators.Contains) {
        const text = Array.isArray(value) ? value.join(", ") : String(value ?? "");
        return text.toLowerCase().includes(filter.value.toLowerCase());
    }
    return matchesNumber(value, filter);
};

export const compareCellValues = (left: CellValue, right: CellValue, direction: SortDirection): number => {
    if (left == null && right == null) return 0;
    if (left == null) return 1;
    if (right == null) return -1;

    const factor = direction === "asc" ? 1 : -1;
    if (typeof left === "number" && typeof right === "number") {
        return (left - right) * factor;
    }
    const leftText = Array.isArray(left) ? left.join(",") : String(left);
    const rightText = Array.isArray(right) ? right.join(",") : String(right);
    return leftText.localeCompare(rightText, undefined, { numeric: true, sensitivity: "base" }) * factor;
};
