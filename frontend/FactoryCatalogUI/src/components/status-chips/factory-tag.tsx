import React from "react";
import Box from "@mui/material/Box";

export interface FactoryTagProps {
    code: string;
    dimmed?: boolean;
}

const FactoryTag: React.FC<FactoryTagProps> = ({ code, dimmed = false }) => (
    <Box
        component="span"
        sx={{
            display: "inline-flex",
            alignItems: "center",
            gap: "5px",
            height: 22,
            padding: "0 8px",
            borderRadius: "4px",
            fontSize: 11,
            fontWeight: 600,
            letterSpacing: "0.03em",
            background: dimmed ? "rgba(0, 0, 0, 0.04)" : "rgba(25, 118, 210, 0.08)",
            color: dimmed ? "rgba(0, 0, 0, 0.38)" : "#1565c0",
            border: `1px solid ${dimmed ? "rgba(0, 0, 0, 0.08)" : "rgba(25, 118, 210, 0.25)"}`,
        }}>
        <Box
            component="span"
            sx={{ width: 6, height: 6, borderRadius: "50%", background: dimmed ? "rgba(0, 0, 0, 0.25)" : "#1976d2" }}
        />
        {code}
    </Box>
);

export default FactoryTag;
