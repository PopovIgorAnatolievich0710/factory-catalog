import React from "react";
import { ColumnFilter, ListFilters, TableSort } from "../../entities/list";
import { CellValue } from "../../utils/table-filter";

export type ColumnFilterType = "values" | "number" | "text";

export interface FilterValueOption {
    value: string;
    label: string;
}

export interface DataTableColumn<T> {
    key: string;
    label: string;
    sortable?: boolean;
    filterType?: ColumnFilterType;
    filterValues?: FilterValueOption[];
    getValue?: (row: T) => CellValue;
    render?: (row: T) => React.ReactNode;
    align?: "left" | "center" | "right";
    width?: number | string;
    monospace?: boolean;
}

export interface DataTableSelection {
    selectedIds: string[];
    selectableIds: string[];
    onToggle: (id: string) => void;
    onToggleAll: () => void;
}

export interface DataTableProps<T> {
    columns: DataTableColumn<T>[];
    rows: T[];
    getRowId: (row: T) => string;
    sort: TableSort | null;
    filters: ListFilters;
    onSortChange: (sort: TableSort | null) => void;
    onFilterChange: (columnKey: string, filter: ColumnFilter | null) => void;
    onRowClick?: (row: T, event: React.MouseEvent) => void;
    isActionRequired?: (row: T) => boolean;
    renderRowTags?: (row: T) => React.ReactNode;
    selection?: DataTableSelection;
    fixedLayout?: boolean;
    isLoading?: boolean;
    emptyText?: string;
}

export interface ClientDataTableProps<T> {
    columns: DataTableColumn<T>[];
    rows: T[];
    getRowId: (row: T) => string;
    noun?: string;
    defaultSort?: TableSort;
    onRowClick?: (row: T, event: React.MouseEvent) => void;
    isActionRequired?: (row: T) => boolean;
    renderRowTags?: (row: T) => React.ReactNode;
    fixedLayout?: boolean;
    isLoading?: boolean;
    emptyText?: string;
}
