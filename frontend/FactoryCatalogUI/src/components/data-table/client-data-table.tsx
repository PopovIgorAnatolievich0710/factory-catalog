import React, { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import DataTable from "./data-table";
import ClearFiltersLink from "./clear-filters-link";
import { ClientDataTableProps, DataTableColumn, FilterValueOption } from "./types";
import { tableStyles } from "./table-styles";
import { ColumnFilter, ListFilters, TableSort } from "../../entities/list";
import { CellValue, compareCellValues, countActiveFilters, matchesFilter } from "../../utils/table-filter";

const getCellValue = <T,>(column: DataTableColumn<T>, row: T): CellValue =>
    column.getValue ? column.getValue(row) : row[column.key];

const collectFilterValues = <T,>(column: DataTableColumn<T>, rows: T[]): FilterValueOption[] => {
    const values = new Set<string>();
    rows.forEach(row => {
        const value = getCellValue(column, row);
        (Array.isArray(value) ? value : [value]).forEach(item => {
            if (item !== null && item !== undefined && item !== "") values.add(String(item));
        });
    });
    return Array.from(values)
        .sort((left, right) => left.localeCompare(right, undefined, { numeric: true }))
        .map(value => ({ value, label: value }));
};

const ClientDataTable = <T,>(props: ClientDataTableProps<T>) => {
    const { columns, rows, noun = "rows", defaultSort = null, ...tableProps } = props;

    const [sort, setSort] = useState<TableSort | null>(defaultSort);
    const [filters, setFilters] = useState<ListFilters>({});

    const columnsWithValues = useMemo(
        () =>
            columns.map(column =>
                column.filterType === "values" && !column.filterValues
                    ? { ...column, filterValues: collectFilterValues(column, rows) }
                    : column,
            ),
        [columns, rows],
    );

    const visibleRows = useMemo(() => {
        const filtered = rows.filter(row =>
            columns.every(column => !filters[column.key] || matchesFilter(getCellValue(column, row), filters[column.key])),
        );
        const sortColumn = sort ? columns.find(column => column.key === sort.sortBy) : null;
        if (!sortColumn) return filtered;

        return [...filtered].sort((left, right) =>
            compareCellValues(getCellValue(sortColumn, left), getCellValue(sortColumn, right), sort.sortDirection),
        );
    }, [rows, columns, filters, sort]);

    const onFilterChange = (columnKey: string, filter: ColumnFilter | null) => {
        const next = { ...filters };
        if (filter) {
            next[columnKey] = filter;
        } else {
            delete next[columnKey];
        }
        setFilters(next);
    };

    return (
        <>
            <DataTable
                {...tableProps}
                columns={columnsWithValues}
                rows={visibleRows}
                sort={sort}
                filters={filters}
                onSortChange={setSort}
                onFilterChange={onFilterChange}
            />
            <Box sx={tableStyles.footer}>
                <ClearFiltersLink activeCount={countActiveFilters(filters)} onClear={() => setFilters({})} />
                <span>
                    {visibleRows.length} of {rows.length} {noun}
                </span>
            </Box>
        </>
    );
};

export default ClientDataTable;
