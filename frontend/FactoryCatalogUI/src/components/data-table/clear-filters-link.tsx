import React from "react";
import Link from "@mui/material/Link";
import FilterAltOffOutlinedIcon from "@mui/icons-material/FilterAltOffOutlined";

interface ClearFiltersLinkProps {
    activeCount: number;
    onClear: () => void;
}

const ClearFiltersLink: React.FC<ClearFiltersLinkProps> = ({ activeCount, onClear }) => {
    if (activeCount === 0) return null;

    return (
        <Link
            component="button"
            underline="none"
            onClick={onClear}
            sx={{ display: "inline-flex", alignItems: "center", gap: "4px", fontSize: 13 }}>
            <FilterAltOffOutlinedIcon sx={{ fontSize: 15 }} />
            Clear {activeCount} filter{activeCount > 1 ? "s" : ""}
        </Link>
    );
};

export default ClearFiltersLink;
