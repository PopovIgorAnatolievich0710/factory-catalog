import React from "react";
import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";

const DEFAULT_COLOR = "#1976d2";

export interface SummaryTileProps {
    icon: React.ReactNode;
    label: string;
    value: React.ReactNode;
    hint?: string;
    color?: string;
}

const SummaryTile: React.FC<SummaryTileProps> = ({ icon, label, value, hint, color = DEFAULT_COLOR }) => (
    <Paper
        variant="outlined"
        sx={{ borderRadius: "8px", padding: 2, display: "flex", alignItems: "center", gap: 1.75, minWidth: 0 }}>
        <Box
            sx={{
                width: 40,
                height: 40,
                borderRadius: "8px",
                background: `${color}14`,
                color,
                display: "inline-flex",
                alignItems: "center",
                justifyContent: "center",
                flexShrink: 0,
            }}>
            {icon}
        </Box>
        <Box sx={{ minWidth: 0 }}>
            <Box sx={{ fontSize: 11, letterSpacing: "0.08em", textTransform: "uppercase", color: "rgba(0, 0, 0, 0.6)" }}>
                {label}
            </Box>
            <Box sx={{ display: "flex", alignItems: "baseline", gap: 1, flexWrap: "wrap" }}>
                <Box component="span" sx={{ fontWeight: 300, fontSize: 28, lineHeight: 1.15 }}>
                    {value}
                </Box>
                {hint && (
                    <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                        {hint}
                    </Box>
                )}
            </Box>
        </Box>
    </Paper>
);

export default SummaryTile;
