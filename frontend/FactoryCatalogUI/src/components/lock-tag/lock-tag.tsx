import React from "react";
import Box from "@mui/material/Box";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";

export interface LockTagProps {
    text?: string;
}

const LockTag: React.FC<LockTagProps> = ({ text = "Read only for your role" }) => (
    <Box
        component="span"
        sx={{
            display: "inline-flex",
            alignItems: "center",
            gap: "5px",
            padding: "4px 10px",
            borderRadius: "4px",
            background: "rgba(0, 0, 0, 0.04)",
            color: "rgba(0, 0, 0, 0.55)",
            fontSize: 12,
            whiteSpace: "nowrap",
        }}>
        <LockOutlinedIcon sx={{ fontSize: 14 }} />
        {text}
    </Box>
);

export default LockTag;
