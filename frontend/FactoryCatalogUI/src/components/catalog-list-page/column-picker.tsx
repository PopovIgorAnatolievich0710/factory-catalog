import React, { useState } from "react";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import Popover from "@mui/material/Popover";
import Typography from "@mui/material/Typography";
import Box from "@mui/material/Box";
import ViewColumnOutlinedIcon from "@mui/icons-material/ViewColumnOutlined";
import { SecondaryBoButton } from "../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { CONSTANTS } from "../../core/CONSTANTS";

interface ColumnPickerOption {
    key: string;
    label: string;
}

interface ColumnPickerProps {
    options: ColumnPickerOption[];
    selectedKeys: string[];
    onChange: (selectedKeys: string[]) => void;
}

const ColumnPicker: React.FC<ColumnPickerProps> = ({ options, selectedKeys, onChange }) => {
    const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
    const limitReached = selectedKeys.length >= CONSTANTS.LIST.MAX_OPTIONAL_COLUMNS;

    const toggle = (key: string) => {
        onChange(selectedKeys.includes(key) ? selectedKeys.filter(x => x !== key) : [...selectedKeys, key]);
    };

    return (
        <>
            <SecondaryBoButton
                startIcon={<ViewColumnOutlinedIcon />}
                onClick={event => setAnchorEl(event.currentTarget)}>
                Columns
            </SecondaryBoButton>
            <Popover
                open={Boolean(anchorEl)}
                anchorEl={anchorEl}
                onClose={() => setAnchorEl(null)}
                anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
                transformOrigin={{ vertical: "top", horizontal: "right" }}>
                <Box sx={{ display: "flex", flexDirection: "column", padding: "12px 16px", minWidth: 240, maxHeight: 420 }}>
                    <Typography variant="body2" color="text.secondary" sx={{ paddingBottom: "4px" }}>
                        Extra columns: {selectedKeys.length} of {CONSTANTS.LIST.MAX_OPTIONAL_COLUMNS}
                    </Typography>
                    {options.map(option => {
                        const checked = selectedKeys.includes(option.key);
                        return (
                            <FormControlLabel
                                key={option.key}
                                label={option.label}
                                sx={{ margin: 0 }}
                                control={
                                    <Checkbox
                                        size="small"
                                        checked={checked}
                                        disabled={!checked && limitReached}
                                        onChange={() => toggle(option.key)}
                                    />
                                }
                            />
                        );
                    })}
                </Box>
            </Popover>
        </>
    );
};

export default ColumnPicker;
