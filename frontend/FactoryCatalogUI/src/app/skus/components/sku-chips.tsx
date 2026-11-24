import React from "react";
import Box from "@mui/material/Box";
import PushPinOutlinedIcon from "@mui/icons-material/PushPinOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import { StatusChip, statusColors } from "../../../components/status-chips";
import {
    ComponentUsage,
    SkuProductType,
    SkuProductTypes,
    SkuStatus,
    SkuStatuses,
    SkuValidationStatus,
    SkuValidationStatuses,
} from "../entities";

export const SkuTypeChip: React.FC<{ type: SkuProductType }> = ({ type }) => (
    <StatusChip label={type} colors={type === SkuProductTypes.Dropshipper ? statusColors.info : statusColors.neutral} />
);

export const SkuStatusChip: React.FC<{ status: SkuStatus }> = ({ status }) => (
    <StatusChip label={status} colors={status === SkuStatuses.Active ? statusColors.success : statusColors.neutral} />
);

export const SkuValidationChip: React.FC<{ status: SkuValidationStatus }> = ({ status }) => (
    <StatusChip label={status} colors={status === SkuValidationStatuses.Valid ? statusColors.success : statusColors.danger} />
);

export const ProductionConflictChip: React.FC = () => (
    <StatusChip
        label="Production conflict"
        colors={statusColors.warning}
        icon={<WarningAmberOutlinedIcon sx={{ fontSize: 14 }} />}
    />
);

export const UsageChip: React.FC<{ usage: ComponentUsage }> = ({ usage }) => (
    <StatusChip
        label={usage}
        colors={usage === "Static" ? statusColors.neutral : statusColors.info}
        icon={usage === "Static" ? <PushPinOutlinedIcon sx={{ fontSize: 13 }} /> : <TuneOutlinedIcon sx={{ fontSize: 13 }} />}
    />
);

export const MutedText: React.FC<{ children: React.ReactNode; danger?: boolean }> = ({ children, danger = false }) => (
    <Box component="span" sx={{ fontSize: 12, color: danger ? "#c62828" : "rgba(0, 0, 0, 0.45)" }}>
        {children}
    </Box>
);
