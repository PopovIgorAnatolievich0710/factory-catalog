import React from "react";
import { useHistory } from "react-router-dom";
import Tooltip from "@mui/material/Tooltip";
import PushPinOutlinedIcon from "@mui/icons-material/PushPinOutlined";
import TuneIcon from "@mui/icons-material/Tune";
import SectionCard from "../../../../components/section-card";
import LockTag from "../../../../components/lock-tag";
import { ClientDataTable, DataTableColumn } from "../../../../components/data-table";
import { StatusChip, statusColors } from "../../../../components/status-chips";
import { SKUS_URL } from "../../../../constants/urls";
import { ComponentDetails, ComponentSkuUsage, ComponentUsages } from "../../entities/component";
import { formatQuantity } from "../../utils/format";

interface SkusUsingTabProps {
    component: ComponentDetails;
}

const ICON_SIZE = 13;

const columns: DataTableColumn<ComponentSkuUsage>[] = [
    { key: "name", label: "Product", sortable: true, filterType: "text" },
    { key: "siteSku", label: "Site SKU", sortable: true, filterType: "text", monospace: true, width: 240 },
    {
        key: "quantity",
        label: "Qty",
        sortable: true,
        filterType: "number",
        align: "right",
        width: 140,
        render: row => formatQuantity(row.quantity),
    },
    {
        key: "usage",
        label: "Usage",
        sortable: true,
        filterType: "values",
        width: 180,
        render: row =>
            row.usage === ComponentUsages.Dynamic ? (
                <Tooltip title="Used only when the order's options call for it">
                    <span>
                        <StatusChip label="Dynamic" colors={statusColors.info} icon={<TuneIcon sx={{ fontSize: ICON_SIZE }} />} />
                    </span>
                </Tooltip>
            ) : (
                <Tooltip title="Used in every unit of this SKU">
                    <span>
                        <StatusChip
                            label="Static"
                            colors={statusColors.neutral}
                            icon={<PushPinOutlinedIcon sx={{ fontSize: ICON_SIZE }} />}
                        />
                    </span>
                </Tooltip>
            ),
    },
];

const SkusUsingTab: React.FC<SkusUsingTabProps> = ({ component }) => {
    const history = useHistory();

    return (
        <SectionCard
            title="SKUs using this component"
            headerRight={<LockTag text="Read only · Source: Master catalog" />}
            noPadding>
            <ClientDataTable
                columns={columns}
                rows={component.usedInSkus}
                getRowId={row => row.skuUuid}
                noun="SKUs"
                defaultSort={{ sortBy: "siteSku", sortDirection: "asc" }}
                onRowClick={row => history.push(`${SKUS_URL}/${row.skuUuid}`)}
                emptyText="No SKUs map this component."
                fixedLayout
            />
        </SectionCard>
    );
};

export default SkusUsingTab;
