import React from "react";
import SyncProblemIcon from "@mui/icons-material/SyncProblem";
import StatusChip, { StatusChipColors } from "./status-chip";
import { statusColors } from "./status-colors";
import { StockStatus, StockStatuses } from "../../entities/status";

const stockStatusView: Record<StockStatus, { label: string; colors: StatusChipColors }> = {
    [StockStatuses.Ok]: { label: "OK", colors: statusColors.success },
    [StockStatuses.Low]: { label: "Low", colors: statusColors.warning },
    [StockStatuses.OutOfStock]: { label: "OOS", colors: statusColors.danger },
    [StockStatuses.OosConflict]: { label: "OOS conflict", colors: statusColors.conflict },
};

export interface StockStatusChipProps {
    status: StockStatus;
    small?: boolean;
}

const StockStatusChip: React.FC<StockStatusChipProps> = ({ status, small }) => {
    const view = stockStatusView[status];
    return (
        <StatusChip
            label={view.label}
            colors={view.colors}
            small={small}
            icon={status === StockStatuses.OosConflict ? <SyncProblemIcon sx={{ fontSize: 13 }} /> : undefined}
        />
    );
};

export default StockStatusChip;
