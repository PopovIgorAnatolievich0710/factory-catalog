import React from "react";
import Box from "@mui/material/Box";

export interface StatusChipColors {
    background: string;
    text: string;
    border?: string;
}

export interface StatusChipProps {
    label: string;
    colors: StatusChipColors;
    icon?: React.ReactNode;
    small?: boolean;
}

const StatusChip: React.FC<StatusChipProps> = ({ label, colors, icon, small = false }) => (
    <Box
        component="span"
        sx={{
            display: "inline-flex",
            alignItems: "center",
            gap: "4px",
            height: small ? 20 : 24,
            padding: small ? "0 7px" : "0 10px",
            borderRadius: "12px",
            fontSize: small ? 11 : 12,
            fontWeight: 500,
            whiteSpace: "nowrap",
            background: colors.background,
            color: colors.text,
            border: colors.border ?? "none",
        }}>
        {icon}
        {label}
    </Box>
);

export default StatusChip;
