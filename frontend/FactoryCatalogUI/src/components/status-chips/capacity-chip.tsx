import React from "react";
import StatusChip from "./status-chip";
import { statusColors } from "./status-colors";

const FULL_CAPACITY = 100;

export interface CapacityChipProps {
    percent: number;
    small?: boolean;
}

const CapacityChip: React.FC<CapacityChipProps> = ({ percent, small }) => {
    if (percent <= 0) {
        return <StatusChip label="Offline · 0%" colors={statusColors.dangerSolid} small={small} />;
    }
    if (percent >= FULL_CAPACITY) {
        return <StatusChip label="Full · 100%" colors={statusColors.success} small={small} />;
    }
    return <StatusChip label={`Reduced · ${percent}%`} colors={statusColors.warning} small={small} />;
};

export default CapacityChip;
