import React from "react";
import Box from "@mui/material/Box";
import Tooltip from "@mui/material/Tooltip";
import BackHandOutlinedIcon from "@mui/icons-material/BackHandOutlined";
import PrecisionManufacturingOutlinedIcon from "@mui/icons-material/PrecisionManufacturingOutlined";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { FULL_CAPACITY, ProcessStatus, ProcessStatuses, ProcessToday, ProcessType, ProcessTypes } from "../entities";

const workTypeColors = { background: "rgba(109, 76, 65, 0.1)", text: "#5d4037" };
const typeIconStyle = { fontSize: 14 };

export const capacityColors = {
    offline: "#d32f2f",
    reduced: "#ed6c02",
    full: "#2e7d32",
};

export const getCapacityColor = (capacityPct: number): string => {
    if (capacityPct <= 0) return capacityColors.offline;
    return capacityPct < FULL_CAPACITY ? capacityColors.reduced : capacityColors.full;
};

export const formatQuantity = (value: number): string => Number(value).toLocaleString("en-US", { maximumFractionDigits: 4 });

export const ProcessTypeChip: React.FC<{ type: ProcessType }> = ({ type }) =>
    type === ProcessTypes.Machine ? (
        <StatusChip label="Machine" colors={statusColors.info} icon={<PrecisionManufacturingOutlinedIcon sx={typeIconStyle} />} />
    ) : (
        <StatusChip label="Work" colors={workTypeColors} icon={<BackHandOutlinedIcon sx={typeIconStyle} />} />
    );

export const ProcessStatusChip: React.FC<{ status: ProcessStatus }> = ({ status }) => (
    <StatusChip label={status} colors={status === ProcessStatuses.Active ? statusColors.success : statusColors.neutral} />
);

export const CapacityPercent: React.FC<{ capacityPct: number }> = ({ capacityPct }) => (
    <Box component="b" sx={{ fontWeight: 500, color: getCapacityColor(capacityPct) }}>
        {capacityPct}%
    </Box>
);

interface TodayLineProps {
    today: ProcessToday | null;
    maxPerDay: number | null;
    showBar?: boolean;
}

export const TodayLine: React.FC<TodayLineProps> = ({ today, maxPerDay, showBar = false }) => {
    if (!today) {
        return (
            <Box sx={{ fontSize: 11.5, color: "rgba(0, 0, 0, 0.45)", marginTop: "3px" }}>
                Maximum daily output is not set
            </Box>
        );
    }

    const usedShare = Math.min(100, (today.used / maxPerDay) * 100);
    const availableShare = Math.min(100, (today.available / maxPerDay) * 100);

    return (
        <Tooltip title={`100% = ${formatQuantity(maxPerDay)} order items per day`} placement="bottom-start">
            <Box sx={{ marginTop: "4px", maxWidth: 360 }}>
                {showBar && (
                    <Box sx={{ position: "relative", height: 6, borderRadius: "3px", background: "rgba(0, 0, 0, 0.06)", overflow: "hidden" }}>
                        <Box sx={{ position: "absolute", left: 0, top: 0, bottom: 0, width: `${availableShare}%`, background: "rgba(46, 125, 50, 0.22)" }} />
                        <Box sx={{ position: "absolute", left: 0, top: 0, bottom: 0, width: `${usedShare}%`, background: "rgba(0, 0, 0, 0.45)" }} />
                    </Box>
                )}
                <Box sx={{ fontSize: 11.5, color: "rgba(0, 0, 0, 0.6)", marginTop: "3px", whiteSpace: "nowrap" }}>
                    {formatQuantity(today.used)} of {formatQuantity(today.available)} today ·{" "}
                    <Box
                        component="b"
                        sx={{ fontWeight: 500, color: today.remainingPct === 0 ? "#c62828" : "rgba(0, 0, 0, 0.8)" }}>
                        {today.remainingPct}% left
                    </Box>
                </Box>
            </Box>
        </Tooltip>
    );
};
