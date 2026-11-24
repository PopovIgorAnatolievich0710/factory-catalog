import React, { useEffect, useState } from "react";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import TextField from "@mui/material/TextField";
import CheckIcon from "@mui/icons-material/Check";
import { ComponentManualOos } from "../../entities/component";
import { formatDate } from "../../../../utils/format-date";

const MAX_NOTE_LENGTH = 1000;

interface OosNoteCellProps {
    manualOos: ComponentManualOos | null;
    canEdit: boolean;
    isEditing: boolean;
    onStartEditing: () => void;
    onSave: (note: string | null) => void;
}

const singleLine = { whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" } as const;

const OosNoteCell: React.FC<OosNoteCellProps> = ({ manualOos, canEdit, isEditing, onStartEditing, onSave }) => {
    const [note, setNote] = useState(manualOos?.note ?? "");

    useEffect(() => {
        setNote(manualOos?.note ?? "");
    }, [manualOos?.note, isEditing]);

    if (!manualOos?.active) {
        return (
            <Box component="span" sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.3)" }}>
                —
            </Box>
        );
    }

    const save = () => onSave(note.trim() || null);

    if (canEdit && isEditing) {
        return (
            <Box sx={{ display: "flex", alignItems: "center", gap: "6px" }}>
                <TextField
                    autoFocus
                    size="small"
                    placeholder="Optional note"
                    value={note}
                    onChange={event => setNote(event.target.value)}
                    onKeyDown={event => {
                        if (event.key === "Enter") save();
                    }}
                    inputProps={{ maxLength: MAX_NOTE_LENGTH }}
                    sx={{ flex: 1, minWidth: 0 }}
                />
                <IconButton
                    size="small"
                    title="Save note"
                    onClick={save}
                    sx={{
                        borderRadius: "4px",
                        background: "#2e7d32",
                        color: "#ffffff",
                        "&:hover": { background: "#1b5e20" },
                    }}>
                    <CheckIcon sx={{ fontSize: 18 }} />
                </IconButton>
            </Box>
        );
    }

    return (
        <Box
            onClick={canEdit ? onStartEditing : undefined}
            title={canEdit ? "Click to edit the note" : manualOos.note ?? ""}
            sx={{ display: "flex", flexDirection: "column", gap: "2px", minWidth: 0, cursor: canEdit ? "text" : "default" }}>
            <Box
                component="span"
                sx={{ ...singleLine, fontSize: 13, color: manualOos.note ? "rgba(0, 0, 0, 0.85)" : "rgba(0, 0, 0, 0.4)" }}>
                {manualOos.note ? `“${manualOos.note}”` : "No note"}
            </Box>
            <Box component="span" sx={{ ...singleLine, fontSize: 11.5, color: "rgba(0, 0, 0, 0.5)" }}>
                Set by {manualOos.setBy ?? "-"} · {formatDate(manualOos.setAt)}
            </Box>
        </Box>
    );
};

export default OosNoteCell;
