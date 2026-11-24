import React, { useEffect, useMemo } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import Tooltip from "@mui/material/Tooltip";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import SyncStatusBadge from "../../../components/sync-status-badge";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { tableStyles } from "../../../components/data-table";
import { COMPONENTS_URL } from "../../../constants/urls";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { loadFilterOptions } from "../store/actions";
import ComponentStockStatus from "./component-stock-status";
import { ApiStockStatuses, ComponentListItem, ComponentStatuses } from "../entities/component";
import {
    COMPONENTS_LIST_KEY,
    COMPONENTS_RESOURCE,
    FACTORY_STOCK_COLUMN_PREFIX,
    SAP_COMPONENTS_SYNC_TYPE,
} from "../constants";
import { stockStatusFilterValues } from "../utils/stock-status";
import { formatQuantity, getFactoryCode } from "../utils/format";

const toFilterValues = (values: string[] | undefined) => (values ?? []).map(value => ({ value, label: value }));

const ComponentsListPage: React.FC = () => {
    const dispatch = useDispatch();
    const { can } = usePermissions();
    const { factories, isLoading: isFactoriesLoading } = useFactories();
    const filterOptions = useRootStateSelector(state => state.componentsCatalog.filterOptions);
    const canViewCost = can(Permissions.Components.ViewCost);

    useEffect(() => {
        dispatch(loadFilterOptions() as any);
    }, [dispatch]);

    const columns = useMemo<CatalogColumn<ComponentListItem>[]>(() => {
        const factoryColumns = factories
            .filter(factory => factory.isActive)
            .map<CatalogColumn<ComponentListItem>>(factory => ({
                key: `${FACTORY_STOCK_COLUMN_PREFIX}${factory.id}`,
                label: `${getFactoryCode(factories, factory.id)} stock`,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => (
                    <Box
                        component="span"
                        sx={{
                            ...tableStyles.monospace,
                            color: row.statusFactoryIds.includes(factory.id) ? "#c62828" : "inherit",
                        }}>
                        {formatQuantity(row.stockByFactory[factory.id])}
                    </Box>
                ),
            }));

        const unitCostColumn: CatalogColumn<ComponentListItem> = {
            key: "unitCost",
            label: "Unit cost",
            sortable: true,
            filterType: "number",
            align: "right",
            render: row =>
                row.unitCost === null ? "-" : `${formatQuantity(row.unitCost)} ${row.unitCostCurrency ?? ""}`.trim(),
        };

        return [
            { key: "erpId", label: "ERP ID", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
            { key: "name", label: "Name", alwaysVisible: true, sortable: true, filterType: "text" },
            {
                key: "family",
                label: "Family",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: toFilterValues(filterOptions?.families),
            },
            {
                key: "status",
                label: "Status",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: stockStatusFilterValues,
                render: row => (
                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px", flexWrap: "wrap" }}>
                        <ComponentStockStatus status={row.status} small />
                        {row.status !== ApiStockStatuses.Ok && (
                            <Box component="span" sx={{ fontSize: 11, color: "rgba(0, 0, 0, 0.55)" }}>
                                {row.statusFactoryIds.map(id => getFactoryCode(factories, id)).join(" · ")}
                            </Box>
                        )}
                    </Box>
                ),
            },
            {
                key: "totalStock",
                label: "Total stock",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => formatQuantity(row.totalStock),
            },
            {
                key: "criticalLevel",
                label: "Critical level",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => (
                    <>
                        {formatQuantity(row.criticalLevel)}
                        {row.hasCriticalLevelOverrides && (
                            <Tooltip title="Has factory overrides">
                                <Box component="span" sx={{ color: "#1976d2", marginLeft: "4px" }}>
                                    *
                                </Box>
                            </Tooltip>
                        )}
                    </>
                ),
            },
            {
                key: "skusUsing",
                label: "SKUs using",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
            },
            {
                key: "material",
                label: "Material",
                sortable: true,
                filterType: "values",
                filterValues: toFilterValues(filterOptions?.materials),
            },
            ...(canViewCost ? [unitCostColumn] : []),
            ...factoryColumns,
        ];
    }, [factories, filterOptions, canViewCost]);

    if (isFactoriesLoading) return <LinearProgress />;

    return (
        <CatalogListPage<ComponentListItem>
            listKey={COMPONENTS_LIST_KEY}
            title="Components"
            subtitle="Components are created in SAP only."
            resource={COMPONENTS_RESOURCE}
            columns={columns}
            getRowId={row => row.erpId}
            getRowUrl={row => `${COMPONENTS_URL}/${encodeURIComponent(row.erpId)}`}
            defaultSort={{ sortBy: "erpId", sortDirection: "asc" }}
            searchPlaceholder="Search ERP ID or name"
            headerInfo={<SyncStatusBadge syncType={SAP_COMPONENTS_SYNC_TYPE} label="Last SAP sync" />}
            isActionRequired={row => row.actionRequired}
            renderRowTags={row =>
                row.componentStatus === ComponentStatuses.Inactive && (
                    <StatusChip label="Inactive in SAP" colors={statusColors.neutral} small />
                )
            }
        />
    );
};

export default ComponentsListPage;
