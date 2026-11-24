import React, { useState } from "react";
import Box from "@mui/material/Box";
import Checkbox from "@mui/material/Checkbox";
import LinearProgress from "@mui/material/LinearProgress";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import CloseIcon from "@mui/icons-material/Close";
import UnfoldMoreIcon from "@mui/icons-material/UnfoldMore";
import ColumnHeaderFilter from "./column-header-filter";
import { DataTableColumn, DataTableProps } from "./types";
import ActionRequiredLabel from "../action-required-label";
import { isFilterActive } from "../../utils/table-filter";
import { tableStyles } from "./table-styles";

const getSortIcon = (direction: "asc" | "desc" | null) => {
    if (direction === "asc") return ArrowUpwardIcon;
    if (direction === "desc") return ArrowDownwardIcon;
    return UnfoldMoreIcon;
};

const renderCell = <T,>(column: DataTableColumn<T>, row: T): React.ReactNode => {
    if (column.render) return column.render(row);
    const value = column.getValue ? column.getValue(row) : row[column.key];
    if (value === null || value === undefined || value === "") return "-";
    return Array.isArray(value) ? value.join(", ") : String(value);
};

const DataTable = <T,>(props: DataTableProps<T>) => {
    const {
        columns,
        rows,
        getRowId,
        sort,
        filters,
        onSortChange,
        onFilterChange,
        onRowClick,
        isActionRequired,
        renderRowTags,
        selection,
        fixedLayout = false,
        isLoading = false,
        emptyText = "No records found",
    } = props;

    const [filterAnchor, setFilterAnchor] = useState<{ columnKey: string; el: HTMLElement } | null>(null);

    const activeColumn = filterAnchor ? columns.find(column => column.key === filterAnchor.columnKey) : null;
    const columnCount = columns.length + (selection ? 1 : 0) + (isActionRequired ? 1 : 0);
    const allSelected =
        selection !== undefined &&
        selection.selectableIds.length > 0 &&
        selection.selectableIds.every(id => selection.selectedIds.includes(id));

    return (
        <>
            <Box sx={{ height: 4 }}>{isLoading && <LinearProgress />}</Box>
            <TableContainer>
                <Table sx={{ tableLayout: fixedLayout ? "fixed" : "auto" }}>
                    <TableHead>
                        <TableRow>
                            {selection && (
                                <TableCell padding="checkbox" sx={tableStyles.headCell}>
                                    <Checkbox
                                        size="small"
                                        checked={allSelected}
                                        disabled={selection.selectableIds.length === 0}
                                        onChange={selection.onToggleAll}
                                    />
                                </TableCell>
                            )}
                            {columns.map(column => {
                                const hasPopover = Boolean(column.sortable || column.filterType);
                                const filterIsActive = isFilterActive(filters[column.key]);
                                const sortDirection = sort?.sortBy === column.key ? sort.sortDirection : null;
                                const SortIcon = getSortIcon(sortDirection);
                                const openPopover = (event: React.MouseEvent<HTMLElement>) =>
                                    setFilterAnchor({ columnKey: column.key, el: event.currentTarget });

                                return (
                                    <TableCell
                                        key={column.key}
                                        align={column.align}
                                        style={{ width: column.width }}
                                        sx={tableStyles.headCell}>
                                        <Box sx={{ display: "inline-flex", alignItems: "center", gap: "3px" }}>
                                            <Box
                                                component="span"
                                                title={hasPopover ? "Filter and sort" : undefined}
                                                onClick={hasPopover ? openPopover : undefined}
                                                sx={{
                                                    display: "inline-flex",
                                                    alignItems: "center",
                                                    gap: "3px",
                                                    cursor: hasPopover ? "pointer" : "default",
                                                    color: filterIsActive ? "primary.main" : undefined,
                                                }}>
                                                {column.label}
                                                {hasPopover && (
                                                    <SortIcon
                                                        sx={{
                                                            fontSize: 15,
                                                            color: filterIsActive || sortDirection ? "primary.main" : "rgba(0, 0, 0, 0.4)",
                                                        }}
                                                    />
                                                )}
                                            </Box>
                                            {filterIsActive && (
                                                <CloseIcon
                                                    titleAccess="Clear filter"
                                                    onClick={() => onFilterChange(column.key, null)}
                                                    sx={{ fontSize: 13, cursor: "pointer", color: "rgba(0, 0, 0, 0.5)" }}
                                                />
                                            )}
                                        </Box>
                                    </TableCell>
                                );
                            })}
                            {isActionRequired && <TableCell sx={tableStyles.headCell} style={{ width: 150 }} />}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.length === 0 && !isLoading && (
                            <TableRow>
                                <TableCell colSpan={columnCount} align="center" sx={tableStyles.emptyCell}>
                                    {emptyText}
                                </TableCell>
                            </TableRow>
                        )}
                        {rows.map(row => {
                            const id = getRowId(row);
                            const actionRequired = isActionRequired?.(row) ?? false;
                            return (
                                <TableRow
                                    key={id}
                                    hover={Boolean(onRowClick)}
                                    selected={selection?.selectedIds.includes(id)}
                                    sx={{ height: tableStyles.rowHeight, cursor: onRowClick ? "pointer" : "default" }}>
                                    {selection && (
                                        <TableCell padding="checkbox" sx={tableStyles.bodyCell}>
                                            <Checkbox
                                                size="small"
                                                checked={selection.selectedIds.includes(id)}
                                                disabled={!selection.selectableIds.includes(id)}
                                                onChange={() => selection.onToggle(id)}
                                            />
                                        </TableCell>
                                    )}
                                    {columns.map((column, index) => (
                                        <TableCell
                                            key={column.key}
                                            align={column.align}
                                            onClick={onRowClick ? event => onRowClick(row, event) : undefined}
                                            sx={{
                                                ...tableStyles.bodyCell,
                                                ...(column.monospace ? tableStyles.monospace : {}),
                                                ...(index === 0 && actionRequired ? tableStyles.actionRequiredCell : {}),
                                            }}>
                                            {index === 0 && renderRowTags ? (
                                                <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
                                                    {renderCell(column, row)}
                                                    {renderRowTags(row)}
                                                </Box>
                                            ) : (
                                                renderCell(column, row)
                                            )}
                                        </TableCell>
                                    ))}
                                    {isActionRequired && (
                                        <TableCell align="right" sx={tableStyles.bodyCell}>
                                            {actionRequired && <ActionRequiredLabel />}
                                        </TableCell>
                                    )}
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>

            {activeColumn && (
                <ColumnHeaderFilter
                    key={activeColumn.key}
                    anchorEl={filterAnchor.el}
                    label={activeColumn.label}
                    sortable={Boolean(activeColumn.sortable)}
                    sortDirection={sort?.sortBy === activeColumn.key ? sort.sortDirection : null}
                    filterType={activeColumn.filterType}
                    filterValues={activeColumn.filterValues ?? []}
                    filter={filters[activeColumn.key]}
                    onSortChange={direction =>
                        onSortChange(direction ? { sortBy: activeColumn.key, sortDirection: direction } : null)
                    }
                    onFilterChange={filter => onFilterChange(activeColumn.key, filter)}
                    onClose={() => setFilterAnchor(null)}
                />
            )}
        </>
    );
};

export default DataTable;
