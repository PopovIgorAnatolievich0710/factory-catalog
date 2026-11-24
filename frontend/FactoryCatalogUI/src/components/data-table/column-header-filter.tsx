import React, { useEffect, useState } from "react";
import Box from "@mui/material/Box";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import Link from "@mui/material/Link";
import MenuItem from "@mui/material/MenuItem";
import Popover from "@mui/material/Popover";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import { ColumnFilterType, FilterValueOption } from "./types";
import { ColumnFilter, FilterOperator, FilterOperators, SortDirection } from "../../entities/list";
import { isFilterActive } from "../../utils/table-filter";

const SEARCH_THRESHOLD = 6;
const APPLY_DEBOUNCE_MS = 400;

const numberOperators: { value: FilterOperator; label: string }[] = [
    { value: FilterOperators.Equal, label: "=" },
    { value: FilterOperators.NotEqual, label: "≠" },
    { value: FilterOperators.LessThan, label: "<" },
    { value: FilterOperators.LessThanOrEqual, label: "≤" },
    { value: FilterOperators.GreaterThan, label: ">" },
    { value: FilterOperators.GreaterThanOrEqual, label: "≥" },
    { value: FilterOperators.Between, label: "Between" },
];

interface ColumnHeaderFilterProps {
    anchorEl: HTMLElement;
    label: string;
    sortable: boolean;
    sortDirection: SortDirection | null;
    filterType?: ColumnFilterType;
    filterValues: FilterValueOption[];
    filter: ColumnFilter | undefined;
    onSortChange: (direction: SortDirection | null) => void;
    onFilterChange: (filter: ColumnFilter | null) => void;
    onClose: () => void;
}

const ColumnHeaderFilter: React.FC<ColumnHeaderFilterProps> = ({
    anchorEl,
    label,
    sortable,
    sortDirection,
    filterType,
    filterValues,
    filter,
    onSortChange,
    onFilterChange,
    onClose,
}) => {
    const [optionSearch, setOptionSearch] = useState("");
    const [draft, setDraft] = useState<ColumnFilter>(
        filter ?? { operator: filterType === "number" ? FilterOperators.GreaterThanOrEqual : FilterOperators.Contains, value: "" },
    );
    const [isDraftDirty, setIsDraftDirty] = useState(false);

    useEffect(() => {
        if (!isDraftDirty) return undefined;
        const timer = window.setTimeout(() => {
            onFilterChange(isFilterActive(draft) ? draft : null);
            setIsDraftDirty(false);
        }, APPLY_DEBOUNCE_MS);
        return () => window.clearTimeout(timer);
    }, [draft, isDraftDirty]);

    const updateDraft = (patch: Partial<ColumnFilter>) => {
        setDraft({ ...draft, ...patch });
        setIsDraftDirty(true);
    };

    const selectedValues = filter?.values ?? [];
    const toggleValue = (value: string) => {
        const values = selectedValues.includes(value)
            ? selectedValues.filter(x => x !== value)
            : [...selectedValues, value];
        onFilterChange(values.length > 0 ? { values } : null);
    };

    const visibleOptions = filterValues.filter(
        option => !optionSearch || option.label.toLowerCase().includes(optionSearch.toLowerCase()),
    );

    return (
        <Popover
            open
            anchorEl={anchorEl}
            onClose={onClose}
            anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
            transformOrigin={{ vertical: "top", horizontal: "left" }}
            slotProps={{ paper: { sx: { width: 250, padding: "12px", borderRadius: "8px" } } }}>
            <Typography sx={{ fontWeight: 500, fontSize: 14, marginBottom: 1 }}>Filter by {label}</Typography>

            {sortable && (
                <ToggleButtonGroup
                    exclusive
                    fullWidth
                    size="small"
                    color="primary"
                    value={sortDirection}
                    onChange={(_event, direction: SortDirection | null) => onSortChange(direction)}
                    sx={{ marginBottom: 1.25 }}>
                    <ToggleButton value="asc" sx={{ textTransform: "none", fontSize: 12, gap: 0.5 }}>
                        <ArrowUpwardIcon sx={{ fontSize: 14 }} />
                        Ascending
                    </ToggleButton>
                    <ToggleButton value="desc" sx={{ textTransform: "none", fontSize: 12, gap: 0.5 }}>
                        <ArrowDownwardIcon sx={{ fontSize: 14 }} />
                        Descending
                    </ToggleButton>
                </ToggleButtonGroup>
            )}

            {filterType === "values" && (
                <>
                    {filterValues.length > SEARCH_THRESHOLD && (
                        <TextField
                            autoFocus
                            fullWidth
                            size="small"
                            placeholder={`Search ${label.toLowerCase()}…`}
                            value={optionSearch}
                            onChange={event => setOptionSearch(event.target.value)}
                            sx={{ marginBottom: 0.75 }}
                        />
                    )}
                    <Box sx={{ maxHeight: 240, overflowY: "auto", display: "flex", flexDirection: "column" }}>
                        {visibleOptions.length === 0 && (
                            <Typography variant="body2" color="text.secondary">
                                No values
                            </Typography>
                        )}
                        {visibleOptions.map(option => (
                            <FormControlLabel
                                key={option.value}
                                label={option.label}
                                sx={{ margin: 0, "& .MuiFormControlLabel-label": { fontSize: 13 } }}
                                control={
                                    <Checkbox
                                        size="small"
                                        checked={selectedValues.includes(option.value)}
                                        onChange={() => toggleValue(option.value)}
                                    />
                                }
                            />
                        ))}
                    </Box>
                </>
            )}

            {filterType === "number" && (
                <Box sx={{ display: "flex", flexDirection: "column", gap: 0.75 }}>
                    <TextField
                        select
                        fullWidth
                        size="small"
                        value={draft.operator}
                        onChange={event => updateDraft({ operator: event.target.value as FilterOperator })}>
                        {numberOperators.map(operator => (
                            <MenuItem key={operator.value} value={operator.value}>
                                {operator.label}
                            </MenuItem>
                        ))}
                    </TextField>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 0.75 }}>
                        <TextField
                            autoFocus
                            size="small"
                            type="number"
                            placeholder="Value"
                            value={draft.value ?? ""}
                            onChange={event => updateDraft({ value: event.target.value })}
                        />
                        {draft.operator === FilterOperators.Between && (
                            <>
                                <span>–</span>
                                <TextField
                                    size="small"
                                    type="number"
                                    placeholder="Max"
                                    value={draft.valueTo ?? ""}
                                    onChange={event => updateDraft({ valueTo: event.target.value })}
                                />
                            </>
                        )}
                    </Box>
                </Box>
            )}

            {filterType === "text" && (
                <TextField
                    autoFocus
                    fullWidth
                    size="small"
                    placeholder="Contains…"
                    value={draft.value ?? ""}
                    onChange={event => updateDraft({ operator: FilterOperators.Contains, value: event.target.value })}
                />
            )}

            {isFilterActive(filter) && (
                <Box sx={{ borderTop: "1px solid rgba(0, 0, 0, 0.08)", marginTop: 1.25, paddingTop: 1 }}>
                    <Link
                        component="button"
                        underline="none"
                        sx={{ fontSize: 12 }}
                        onClick={() => {
                            onFilterChange(null);
                            onClose();
                        }}>
                        Clear filter
                    </Link>
                </Box>
            )}
        </Popover>
    );
};

export default ColumnHeaderFilter;
