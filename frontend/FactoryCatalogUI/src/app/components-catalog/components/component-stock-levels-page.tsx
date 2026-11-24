import React, { useMemo } from "react";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import SyncStatusBadge from "../../../components/sync-status-badge";
import { FactoryTag } from "../../../components/status-chips";
import { COMPONENTS_URL } from "../../../constants/urls";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import ComponentStockStatus from "./component-stock-status";
import LevelSourceTag from "./level-source-tag";
import { ApiStockStatuses, ComponentStockLevel } from "../entities/component";
import {
    COMPONENT_STOCK_LEVELS_LIST_KEY,
    COMPONENT_STOCK_LEVELS_RESOURCE,
    SAP_COMPONENTS_SYNC_TYPE,
    STOCK_TAB_KEY,
} from "../constants";
import { stockStatusFilterValues } from "../utils/stock-status";
import { formatQuantity, getFactoryCode } from "../utils/format";
import { formatDate } from "../../../utils/format-date";

const ROW_ID_SEPARATOR = "|";

const ComponentStockLevelsPage: React.FC = () => {
    const { factories, isLoading: isFactoriesLoading } = useFactories();

    const columns = useMemo<CatalogColumn<ComponentStockLevel>[]>(
        () => [
            { key: "erpId", label: "ERP ID", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
            { key: "name", label: "Name", alwaysVisible: true, sortable: true, filterType: "text" },
            {
                key: "factoryId",
                label: "Factory",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: factories.map(factory => ({
                    value: factory.id,
                    label: `${factory.name} (${getFactoryCode(factories, factory.id)})`,
                })),
                render: row => <FactoryTag code={getFactoryCode(factories, row.factoryId)} />,
            },
            {
                key: "warehouseStock",
                label: "Warehouse",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => formatQuantity(row.warehouseStock),
            },
            {
                key: "productionFloorStock",
                label: "Production floor",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => formatQuantity(row.productionFloorStock),
            },
            {
                key: "totalStock",
                label: "Total",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                align: "right",
                render: row => <b>{formatQuantity(row.totalStock)}</b>,
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
                        {row.criticalLevel !== null && <LevelSourceTag isOverride={row.isCriticalLevelOverride} />}
                    </>
                ),
            },
            {
                key: "status",
                label: "Status",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: stockStatusFilterValues.filter(option => option.value !== ApiStockStatuses.Ok),
                render: row => <ComponentStockStatus status={row.status} small />,
            },
            {
                key: "manualOosSetBy",
                label: "Manual flag",
                alwaysVisible: true,
                render: row =>
                    row.manualOosSetBy ? (
                        <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }} title={row.manualOosNote ?? ""}>
                            {row.manualOosSetBy} · {formatDate(row.manualOosSetAt)}
                        </Box>
                    ) : (
                        "-"
                    ),
            },
        ],
        [factories],
    );

    if (isFactoriesLoading) return <LinearProgress />;

    return (
        <CatalogListPage<ComponentStockLevel>
            listKey={COMPONENT_STOCK_LEVELS_LIST_KEY}
            title="Component stock levels"
            subtitle="Components below the critical level or out of stock, per factory."
            resource={COMPONENT_STOCK_LEVELS_RESOURCE}
            columns={columns}
            getRowId={row => `${row.erpId}${ROW_ID_SEPARATOR}${row.factoryId}`}
            getRowUrl={row => `${COMPONENTS_URL}/${encodeURIComponent(row.erpId)}?tab=${STOCK_TAB_KEY}`}
            defaultSort={{ sortBy: "status", sortDirection: "desc" }}
            searchPlaceholder="Search ERP ID or name"
            headerInfo={<SyncStatusBadge syncType={SAP_COMPONENTS_SYNC_TYPE} label="Last SAP sync" />}
        />
    );
};

export default ComponentStockLevelsPage;
