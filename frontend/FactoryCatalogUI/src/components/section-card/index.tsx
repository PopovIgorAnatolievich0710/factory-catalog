import React from "react";
import Box from "@mui/material/Box";
import Paper from "@mui/material/Paper";
import Typography from "@mui/material/Typography";

export interface SectionCardProps {
    title?: string;
    subtitle?: string;
    headerRight?: React.ReactNode;
    noPadding?: boolean;
    children: React.ReactNode;
}

const SectionCard: React.FC<SectionCardProps> = ({ title, subtitle, headerRight, noPadding = false, children }) => (
    <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
        {title && (
            <Box
                sx={{
                    padding: "14px 18px",
                    borderBottom: "1px solid rgba(0, 0, 0, 0.08)",
                    display: "flex",
                    alignItems: "center",
                    gap: 1.5,
                    flexWrap: "wrap",
                }}>
                <Box sx={{ flex: 1, minWidth: 200 }}>
                    <Typography sx={{ fontSize: 16, fontWeight: 500 }}>{title}</Typography>
                    {subtitle && (
                        <Typography sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.6)", marginTop: "2px" }}>
                            {subtitle}
                        </Typography>
                    )}
                </Box>
                {headerRight}
            </Box>
        )}
        <Box sx={{ padding: noPadding ? 0 : "18px" }}>{children}</Box>
    </Paper>
);

export default SectionCard;
