import React, { useEffect, useState } from "react";
import Box from "@mui/material/Box";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { PrimaryBoButton } from "../../../../bo-frontend/src/components/Buttons/PrimaryBoButton";
import { CancelBoButton } from "../../../../bo-frontend/src/components/Buttons/CancelBoButton";
import { useErrorNotification } from "../../../helpers/response-helper";
import processService from "../services/process-service";
import { ProcessDetails, ProcessType, ProcessTypes, ProcessUnit, processUnits } from "../entities";

interface NewProcessDialogProps {
    open: boolean;
    onClose: () => void;
    onCreated: (process: ProcessDetails) => void;
}

const NewProcessDialog: React.FC<NewProcessDialogProps> = ({ open, onClose, onCreated }) => {
    const notifyError = useErrorNotification();

    const [name, setName] = useState("");
    const [processId, setProcessId] = useState("");
    const [type, setType] = useState<ProcessType>(ProcessTypes.Work);
    const [measuredIn, setMeasuredIn] = useState<ProcessUnit>("Minutes");
    const [description, setDescription] = useState("");
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        if (open) {
            setName("");
            setProcessId("");
            setType(ProcessTypes.Work);
            setMeasuredIn("Minutes");
            setDescription("");
        }
    }, [open]);

    const onCreateClick = async () => {
        setIsSaving(true);
        try {
            onCreated(
                await processService.create({
                    processId: processId.trim() || null,
                    name: name.trim(),
                    type,
                    measuredIn,
                    description: description.trim() || null,
                }),
            );
        } catch (error) {
            notifyError(error);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <Dialog open={open} onClose={isSaving ? undefined : onClose} maxWidth="sm" fullWidth>
            <DialogTitle>New process</DialogTitle>
            <DialogContent>
                <Box sx={{ display: "flex", flexDirection: "column", gap: 2, paddingTop: 1 }}>
                    <TextField
                        autoFocus
                        size="small"
                        label="Process name"
                        value={name}
                        onChange={event => setName(event.target.value)}
                        inputProps={{ maxLength: 200 }}
                    />
                    <TextField
                        size="small"
                        label="Process ID"
                        helperText="Leave empty to build it from the name, e.g. ENGRAVING"
                        value={processId}
                        onChange={event => setProcessId(event.target.value.toUpperCase())}
                        inputProps={{ maxLength: 50 }}
                    />
                    <Box sx={{ display: "flex", gap: 2 }}>
                        <TextField
                            select
                            size="small"
                            label="Type"
                            value={type}
                            onChange={event => setType(event.target.value as ProcessType)}
                            sx={{ flex: 1 }}>
                            <MenuItem value={ProcessTypes.Work}>Work</MenuItem>
                            <MenuItem value={ProcessTypes.Machine}>Machine</MenuItem>
                        </TextField>
                        <TextField
                            select
                            size="small"
                            label="Measured in"
                            value={measuredIn}
                            onChange={event => setMeasuredIn(event.target.value as ProcessUnit)}
                            sx={{ flex: 1 }}>
                            {processUnits.map(unit => (
                                <MenuItem key={unit.value} value={unit.value}>
                                    {unit.label}
                                </MenuItem>
                            ))}
                        </TextField>
                    </Box>
                    <TextField
                        size="small"
                        label="Description"
                        multiline
                        minRows={3}
                        value={description}
                        onChange={event => setDescription(event.target.value)}
                        inputProps={{ maxLength: 1000 }}
                    />
                </Box>
            </DialogContent>
            <DialogActions>
                <CancelBoButton disabled={isSaving} onClick={onClose}>
                    Cancel
                </CancelBoButton>
                <PrimaryBoButton disabled={isSaving || name.trim().length === 0} onClick={onCreateClick}>
                    Create
                </PrimaryBoButton>
            </DialogActions>
        </Dialog>
    );
};

export default NewProcessDialog;
