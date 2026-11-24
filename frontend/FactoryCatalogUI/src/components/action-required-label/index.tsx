import React from "react";
import Box from "@mui/material/Box";
import ErrorIcon from "@mui/icons-material/Error";

const ActionRequiredLabel: React.FC = () => (
    <Box
        component="span"
        sx={{
            display: "inline-flex",
            alignItems: "center",
            gap: "3px",
            fontSize: 11,
            fontWeight: 500,
            color: "#c62828",
            whiteSpace: "nowrap",
        }}>
        <ErrorIcon sx={{ fontSize: 14 }} />
        Action required
    </Box>
);

export default ActionRequiredLabel;
