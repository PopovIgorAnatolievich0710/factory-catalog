import React from "react";
import Box from "@mui/material/Box";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import { formatPct, FULL_SHARE_PCT } from "../entities";

const OK_COLOR = "#2e7d32";
const ERROR_COLOR = "#c62828";

interface ShareTotalCheckProps {
    totalPct: number;
    emptyText?: string | null;
}

const ShareTotalCheck: React.FC<ShareTotalCheckProps> = ({ totalPct, emptyText = null }) => {
    const isEmpty = emptyText !== null;
    const isOk = !isEmpty && totalPct === FULL_SHARE_PCT;

    return (
        <Box
            component="span"
            sx={{
                display: "inline-flex",
                alignItems: "center",
                gap: "4px",
                fontSize: 12,
                fontWeight: isOk ? 400 : 500,
                color: isOk ? OK_COLOR : ERROR_COLOR,
                whiteSpace: "nowrap",
            }}>
            {isOk ? <CheckCircleIcon sx={{ fontSize: 15 }} /> : <ErrorIcon sx={{ fontSize: 15 }} />}
            {isEmpty ? emptyText : formatPct(totalPct)}
        </Box>
    );
};

export default ShareTotalCheck;
