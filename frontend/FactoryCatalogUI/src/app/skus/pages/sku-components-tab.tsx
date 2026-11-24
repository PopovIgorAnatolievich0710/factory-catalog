import React, { useMemo } from "react";
import { useHistory } from "react-router-dom";
import { ClientDataTable, DataTableColumn } from "../../../components/data-table";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { StockStatusChip } from "../../../components/status-chips";
import { COMPONENTS_URL } from "../../../constants/urls";
import { StockStatus, StockStatuses } from "../../../entities/status";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { toKitStockStatus } from "../../components-catalog/utils/stock-status";
import { MutedText, UsageChip } from "../components/sku-chips";
import { SkuComponent, SkuComponentFactoryStock, SkuDetails } from "../entities";

const NOT_FOUND_TEXT = "Not found in SAP";
const NO_VALUE = "-";

const stockStatusLabels: Record<StockStatus, string> = {
    [StockStatuses.Ok]: "OK",
    [StockStatuses.Low]: "Low",
    [StockStatuses.OutOfStock]: "OOS",
    [StockStatuses.OosConflict]: "OOS conflict",
};

const toStockStatus = (stock: SkuComponentFactoryStock): StockStatus => toKitStockStatus(stock.stockStatus);

const SkuComponentsTab: React.FC<{ sku: SkuDetails }> = ({ sku }) => {
    const history = useHistory();
    const { factories } = useFactories();
    const factoryIds = useMemo(
        () => factories.filter(factory => factory.isActive).map(factory => factory.id),
        [factories],
    );

    const columns = useMemo<DataTableColumn<SkuComponent>[]>(
        () => [
            { key: "erpId", label: "ERP ID", sortable: true, filterType: "text", monospace: true, width: 200 },
            {
                key: "name",
                label: "Component",
                sortable: true,
                filterType: "text",
                getValue: row => row.name ?? NOT_FOUND_TEXT,
                render: row => row.name ?? <MutedText danger>{NOT_FOUND_TEXT}</MutedText>,
            },
            { key: "quantity", label: "Qty", sortable: true, filterType: "number", align: "right", width: 100 },
            {
                key: "usage",
                label: "Usage",
                sortable: true,
                filterType: "values",
                width: 140,
                render: row => <UsageChip usage={row.usage} />,
            },
            ...factoryIds.map<DataTableColumn<SkuComponent>>(factoryId => {
                const find = (row: SkuComponent) => row.stock.find(stock => stock.factoryId === factoryId);
                return {
                    key: `stock.${factoryId}`,
                    label: factoryId,
                    sortable: true,
                    filterType: "values",
                    align: "center",
                    width: 140,
                    getValue: row => {
                        const stock = find(row);
                        return stock ? stockStatusLabels[toStockStatus(stock)] : NO_VALUE;
                    },
                    render: row => {
                        const stock = find(row);
                        return stock ? <StockStatusChip status={toStockStatus(stock)} small /> : NO_VALUE;
                    },
                };
            }),
        ],
        [factoryIds],
    );

    return (
        <SectionCard title="Mapped components" headerRight={<LockTag text="Read only · Source: Master catalog" />} noPadding>
            <ClientDataTable
                columns={columns}
                rows={sku.components}
                getRowId={row => row.erpId}
                noun="components"
                isActionRequired={row => row.name === null}
                onRowClick={row => {
                    if (row.name !== null) history.push(`${COMPONENTS_URL}/${encodeURIComponent(row.erpId)}`);
                }}
                fixedLayout
            />
        </SectionCard>
    );
};

export default SkuComponentsTab;
