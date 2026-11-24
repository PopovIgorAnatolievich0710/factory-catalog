export type SortDirection = "asc" | "desc";

export const FilterOperators = {
    Equal: "eq",
    NotEqual: "ne",
    LessThan: "lt",
    LessThanOrEqual: "lte",
    GreaterThan: "gt",
    GreaterThanOrEqual: "gte",
    Between: "between",
    Contains: "contains",
} as const;

export type FilterOperator = (typeof FilterOperators)[keyof typeof FilterOperators];

export interface ColumnFilter {
    values?: string[];
    operator?: FilterOperator;
    value?: string;
    valueTo?: string;
}

export type ListFilters = Record<string, ColumnFilter>;

export interface TableSort {
    sortBy: string;
    sortDirection: SortDirection;
}

export interface ListRequest {
    page: number;
    pageSize: number;
    search: string;
    sortBy: string | null;
    sortDirection: SortDirection;
    filters: ListFilters;
}

export interface ListResponse<T> {
    items: T[];
    totalCount: number;
}

export interface ListSettings {
    columns: string[];
}

export interface DownloadedFile {
    blob: Blob;
    fileName: string | null;
}
