import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useHistory, useLocation } from "react-router-dom";
import FileSaver from "file-saver";
import dayjs from "dayjs";
import Box from "@mui/material/Box";
import InputAdornment from "@mui/material/InputAdornment";
import Paper from "@mui/material/Paper";
import TablePagination from "@mui/material/TablePagination";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import FileDownloadOutlinedIcon from "@mui/icons-material/FileDownloadOutlined";
import SearchIcon from "@mui/icons-material/Search";
import { SecondaryBoButton } from "../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import DataTable, { ClearFiltersLink } from "../data-table";
import ColumnPicker from "./column-picker";
import { CatalogListContext, CatalogListPageProps } from "./types";
import listService from "../../services/list-service";
import listSettingsService from "../../services/list-settings-service";
import { catalogApi } from "../../helpers/api-helper";
import { useErrorNotification } from "../../helpers/response-helper";
import { buildListQuery, ListQueryDefaults, parseListQuery } from "../../utils/list-query";
import { countActiveFilters } from "../../utils/table-filter";
import { ColumnFilter, ListRequest, TableSort } from "../../entities/list";
import { CONSTANTS } from "../../core/CONSTANTS";

const SEARCH_DEBOUNCE_MS = 400;

const CatalogListPage = <T,>(props: CatalogListPageProps<T>) => {
    const {
        listKey,
        title,
        subtitle,
        resource,
        api = catalogApi,
        columns,
        getRowId,
        getRowUrl,
        defaultSort,
        searchPlaceholder = "Search...",
        headerInfo,
        headerActions,
        summary,
        toolbarActions,
        isActionRequired,
        renderRowTags,
        selectable = false,
        isRowSelectable,
        bulkActions,
    } = props;

    const history = useHistory();
    const location = useLocation();
    const notifyError = useErrorNotification();

    const defaults = useMemo<ListQueryDefaults>(
        () => ({ sortBy: defaultSort?.sortBy ?? null, sortDirection: defaultSort?.sortDirection ?? "asc" }),
        [defaultSort?.sortBy, defaultSort?.sortDirection],
    );
    const request = useMemo(() => parseListQuery(location.search, defaults), [location.search, defaults]);

    const [items, setItems] = useState<T[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [isLoading, setIsLoading] = useState(false);
    const [isExporting, setIsExporting] = useState(false);
    const [reloadToken, setReloadToken] = useState(0);
    const [searchText, setSearchText] = useState(request.search);
    const [optionalColumnKeys, setOptionalColumnKeys] = useState<string[]>([]);
    const [selectedIds, setSelectedIds] = useState<string[]>([]);

    const optionalColumns = useMemo(() => columns.filter(column => !column.alwaysVisible), [columns]);
    const visibleColumns = useMemo(
        () => columns.filter(column => column.alwaysVisible || optionalColumnKeys.includes(column.key)),
        [columns, optionalColumnKeys],
    );
    const visibleColumnKeys = useMemo(() => visibleColumns.map(column => column.key), [visibleColumns]);

    const updateRequest = useCallback(
        (patch: Partial<ListRequest>) => {
            history.replace({
                pathname: location.pathname,
                search: buildListQuery({ ...request, ...patch }, defaults, location.search),
            });
        },
        [history, location.pathname, location.search, request, defaults],
    );

    const reload = useCallback(() => setReloadToken(token => token + 1), []);
    const clearSelection = useCallback(() => setSelectedIds([]), []);

    useEffect(() => {
        let cancelled = false;
        listSettingsService
            .fetch(listKey)
            .then(settings => {
                if (cancelled) return;
                const knownKeys = optionalColumns.map(column => column.key);
                setOptionalColumnKeys(
                    settings.columns
                        .filter(key => knownKeys.includes(key))
                        .slice(0, CONSTANTS.LIST.MAX_OPTIONAL_COLUMNS),
                );
            })
            .catch(notifyError);

        return () => {
            cancelled = true;
        };
    }, [listKey]);

    useEffect(() => {
        let cancelled = false;
        setIsLoading(true);
        listService
            .search<T>(resource, request, api)
            .then(response => {
                if (cancelled) return;
                setItems(response.items);
                setTotalCount(response.totalCount);
            })
            .catch(error => {
                if (!cancelled) notifyError(error);
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [resource, api, request, reloadToken]);

    useEffect(() => {
        setSearchText(request.search);
    }, [request.search]);

    useEffect(() => {
        if (searchText === request.search) return undefined;
        const timer = window.setTimeout(() => updateRequest({ search: searchText, page: 1 }), SEARCH_DEBOUNCE_MS);
        return () => window.clearTimeout(timer);
    }, [searchText]);

    const filtersKey = JSON.stringify(request.filters);
    useEffect(() => {
        setSelectedIds([]);
    }, [request.search, filtersKey]);

    const onOptionalColumnsChange = (keys: string[]) => {
        const orderedKeys = optionalColumns.map(column => column.key).filter(key => keys.includes(key));
        setOptionalColumnKeys(orderedKeys);
        listSettingsService.save(listKey, orderedKeys).catch(notifyError);
    };

    const onSortChange = (sort: TableSort | null) => {
        updateRequest({
            sortBy: sort?.sortBy ?? defaults.sortBy,
            sortDirection: sort?.sortDirection ?? defaults.sortDirection,
            page: 1,
        });
    };

    const onFilterChange = (columnKey: string, filter: ColumnFilter | null) => {
        const filters = { ...request.filters };
        if (filter) {
            filters[columnKey] = filter;
        } else {
            delete filters[columnKey];
        }
        updateRequest({ filters, page: 1 });
    };

    const onExportClick = async () => {
        setIsExporting(true);
        try {
            const file = await listService.export(resource, request, visibleColumnKeys, api);
            FileSaver.saveAs(file.blob, file.fileName ?? `${listKey}_${dayjs().format("YYYYMMDD_HHmmss")}.xlsx`);
        } catch (error) {
            notifyError(error);
        } finally {
            setIsExporting(false);
        }
    };

    const onRowClick = (row: T, event: React.MouseEvent) => {
        if (event.ctrlKey || event.metaKey) {
            window.open(getRowUrl(row), "_blank");
            return;
        }
        history.push(getRowUrl(row));
    };

    const selectableIds = useMemo(
        () => items.filter(row => !isRowSelectable || isRowSelectable(row)).map(getRowId),
        [items, isRowSelectable, getRowId],
    );

    const onToggleAll = () => {
        const allSelected = selectableIds.every(id => selectedIds.includes(id));
        setSelectedIds(
            allSelected
                ? selectedIds.filter(id => !selectableIds.includes(id))
                : [...selectedIds, ...selectableIds.filter(id => !selectedIds.includes(id))],
        );
    };

    const onToggle = (id: string) => {
        setSelectedIds(selectedIds.includes(id) ? selectedIds.filter(x => x !== id) : [...selectedIds, id]);
    };

    const context: CatalogListContext = { request, visibleColumnKeys, totalCount, reload };

    return (
        <Box sx={{ padding: "24px", display: "flex", flexDirection: "column", gap: 2 }}>
            <Box sx={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 2, flexWrap: "wrap" }}>
                <div>
                    <Typography component="h1" sx={{ fontSize: 22, fontWeight: 500, color: "rgba(0, 0, 0, 0.87)" }}>
                        {title}
                    </Typography>
                    {subtitle && (
                        <Typography sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)", marginTop: "2px" }}>
                            {subtitle}
                        </Typography>
                    )}
                </div>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flexWrap: "wrap" }}>
                    {headerInfo}
                    {headerActions}
                </Box>
            </Box>

            {summary && (
                <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: 2 }}>
                    {summary}
                </Box>
            )}

            <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
                <Box sx={{ padding: "12px 16px", display: "flex", alignItems: "center", gap: 1, flexWrap: "wrap" }}>
                    <TextField
                        size="small"
                        placeholder={searchPlaceholder}
                        value={searchText}
                        onChange={event => setSearchText(event.target.value)}
                        sx={{ flex: 1, minWidth: 220, maxWidth: 360 }}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <SearchIcon sx={{ fontSize: 20, color: "rgba(0, 0, 0, 0.54)" }} />
                                </InputAdornment>
                            ),
                        }}
                    />
                    <Box sx={{ flex: 1 }} />
                    {toolbarActions?.(context)}
                    {optionalColumns.length > 0 && (
                        <ColumnPicker
                            options={optionalColumns}
                            selectedKeys={optionalColumnKeys}
                            onChange={onOptionalColumnsChange}
                        />
                    )}
                    <SecondaryBoButton
                        startIcon={<FileDownloadOutlinedIcon />}
                        disabled={isExporting}
                        onClick={onExportClick}>
                        Export to Excel
                    </SecondaryBoButton>
                </Box>

                {selectable && selectedIds.length > 0 && (
                    <Box
                        sx={{
                            padding: "8px 16px",
                            display: "flex",
                            alignItems: "center",
                            gap: 1,
                            background: "rgba(25, 118, 210, 0.08)",
                        }}>
                        <Typography sx={{ fontSize: 13, fontWeight: 500 }}>{selectedIds.length} selected</Typography>
                        {bulkActions?.(selectedIds, { ...context, clearSelection })}
                        <SecondaryBoButton size="small" onClick={clearSelection}>
                            Clear selection
                        </SecondaryBoButton>
                    </Box>
                )}

                <DataTable
                    columns={visibleColumns}
                    rows={items}
                    getRowId={getRowId}
                    sort={request.sortBy ? { sortBy: request.sortBy, sortDirection: request.sortDirection } : null}
                    filters={request.filters}
                    onSortChange={onSortChange}
                    onFilterChange={onFilterChange}
                    onRowClick={getRowUrl ? onRowClick : undefined}
                    isActionRequired={isActionRequired}
                    renderRowTags={renderRowTags}
                    selection={selectable ? { selectedIds, selectableIds, onToggle, onToggleAll } : undefined}
                    isLoading={isLoading}
                />

                <Box sx={{ display: "flex", alignItems: "center", justifyContent: "flex-end", gap: "14px", paddingLeft: 2 }}>
                    <ClearFiltersLink
                        activeCount={countActiveFilters(request.filters)}
                        onClear={() => updateRequest({ filters: {}, page: 1 })}
                    />
                    <TablePagination
                        component="div"
                        rowsPerPageOptions={CONSTANTS.PAGING.PAGE_SIZE_OPTIONS}
                        count={totalCount}
                        rowsPerPage={request.pageSize}
                        page={totalCount === 0 ? 0 : request.page - 1}
                        onPageChange={(_event, page) => updateRequest({ page: page + 1 })}
                        onRowsPerPageChange={event =>
                            updateRequest({ pageSize: Number.parseInt(event.target.value, 10), page: 1 })
                        }
                    />
                </Box>
            </Paper>
        </Box>
    );
};

export default CatalogListPage;
