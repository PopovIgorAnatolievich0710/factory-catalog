import React, { useEffect, useState } from "react";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import TextField from "@mui/material/TextField";
import AddIcon from "@mui/icons-material/Add";
import { SecondaryBoButton } from "../../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { useErrorNotification } from "../../../../helpers/response-helper";
import componentsService from "../../services/components-service";
import { ComponentListItem } from "../../entities/component";

const SEARCH_DEBOUNCE_MS = 300;

interface AlternativePickerProps {
    excludedErpIds: string[];
    disabled: boolean;
    onAdd: (alternativeErpId: string) => Promise<boolean>;
}

const AlternativePicker: React.FC<AlternativePickerProps> = ({ excludedErpIds, disabled, onAdd }) => {
    const notifyError = useErrorNotification();
    const [searchText, setSearchText] = useState("");
    const [options, setOptions] = useState<ComponentListItem[]>([]);
    const [selected, setSelected] = useState<ComponentListItem | null>(null);
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        if (disabled) return undefined;

        let cancelled = false;
        const timer = window.setTimeout(() => {
            setIsLoading(true);
            componentsService
                .searchActive(searchText)
                .then(items => {
                    if (!cancelled) setOptions(items);
                })
                .catch(error => {
                    if (!cancelled) notifyError(error);
                })
                .finally(() => {
                    if (!cancelled) setIsLoading(false);
                });
        }, SEARCH_DEBOUNCE_MS);

        return () => {
            cancelled = true;
            window.clearTimeout(timer);
        };
    }, [searchText, disabled]);

    const onAddClick = async () => {
        if (await onAdd(selected.erpId)) {
            setSelected(null);
            setSearchText("");
        }
    };

    return (
        <Box sx={{ display: "flex", gap: 1.25, padding: "12px 16px", alignItems: "center", flexWrap: "wrap" }}>
            <Autocomplete
                size="small"
                sx={{ width: 420, maxWidth: "100%" }}
                disabled={disabled}
                loading={isLoading}
                options={options.filter(option => !excludedErpIds.includes(option.erpId))}
                value={selected}
                inputValue={searchText}
                filterOptions={items => items}
                getOptionLabel={option => `${option.erpId} · ${option.name}`}
                isOptionEqualToValue={(option, value) => option.erpId === value.erpId}
                onChange={(_event, value) => setSelected(value)}
                onInputChange={(_event, value) => setSearchText(value)}
                renderInput={params => <TextField {...params} placeholder="Choose a component to add" />}
            />
            <SecondaryBoButton size="small" startIcon={<AddIcon />} disabled={disabled || !selected} onClick={onAddClick}>
                Add alternative
            </SecondaryBoButton>
        </Box>
    );
};

export default AlternativePicker;
