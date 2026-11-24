import React from "react";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import Tooltip from "@mui/material/Tooltip";

export interface GatedCheckboxProps {
    allowed: boolean;
    checked: boolean;
    onChange: (checked: boolean) => void;
    label?: string;
    deniedMessage?: string;
}

const GatedCheckbox: React.FC<GatedCheckboxProps> = ({
    allowed,
    checked,
    onChange,
    label,
    deniedMessage = "Read only for your role",
}) => {
    const checkbox = (
        <Checkbox
            size="small"
            checked={checked}
            disabled={!allowed}
            onChange={event => onChange(event.target.checked)}
            sx={{ padding: "4px" }}
        />
    );
    const control = label ? (
        <FormControlLabel
            control={checkbox}
            label={label}
            disabled={!allowed}
            sx={{ margin: 0, "& .MuiFormControlLabel-label": { fontSize: 13 } }}
        />
    ) : (
        checkbox
    );

    if (allowed) return control;

    return (
        <Tooltip title={deniedMessage}>
            <span style={{ display: "inline-flex" }}>{control}</span>
        </Tooltip>
    );
};

export default GatedCheckbox;
