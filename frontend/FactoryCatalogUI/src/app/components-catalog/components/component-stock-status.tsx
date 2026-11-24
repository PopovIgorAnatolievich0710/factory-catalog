import React from "react";
import Tooltip from "@mui/material/Tooltip";
import { StockStatusChip } from "../../../components/status-chips";
import { ApiStockStatus, ApiStockStatuses } from "../entities/component";
import { OOS_CONFLICT_EXPLANATION, toKitStockStatus } from "../utils/stock-status";

interface ComponentStockStatusProps {
    status: ApiStockStatus;
    small?: boolean;
}

const ComponentStockStatus: React.FC<ComponentStockStatusProps> = ({ status, small }) => (
    <Tooltip title={status === ApiStockStatuses.OosConflict ? OOS_CONFLICT_EXPLANATION : ""}>
        <span style={{ display: "inline-flex" }}>
            <StockStatusChip status={toKitStockStatus(status)} small={small} />
        </span>
    </Tooltip>
);

export default ComponentStockStatus;
