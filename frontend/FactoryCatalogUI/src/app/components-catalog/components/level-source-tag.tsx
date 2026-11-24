import React from "react";
import Box from "@mui/material/Box";

interface LevelSourceTagProps {
    isOverride: boolean;
}

const LevelSourceTag: React.FC<LevelSourceTagProps> = ({ isOverride }) => (
    <Box
        component="span"
        sx={{
            fontSize: 11,
            padding: "1px 6px",
            borderRadius: "3px",
            marginLeft: "6px",
            background: isOverride ? "rgba(25, 118, 210, 0.1)" : "rgba(0, 0, 0, 0.06)",
            color: isOverride ? "#1565c0" : "rgba(0, 0, 0, 0.55)",
        }}>
        {isOverride ? "override" : "global"}
    </Box>
);

export default LevelSourceTag;
