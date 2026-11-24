import React, { useEffect, useState } from "react";
import TextField from "@mui/material/TextField";
import { parseLevel } from "../../utils/format";

interface LevelInputProps {
    value: number | null;
    disabled: boolean;
    placeholder?: string;
    onCommit: (level: number | null) => void;
}

const toText = (value: number | null) => (value === null ? "" : String(value));

const LevelInput: React.FC<LevelInputProps> = ({ value, disabled, placeholder, onCommit }) => {
    const [text, setText] = useState(toText(value));
    const level = parseLevel(text);

    useEffect(() => {
        setText(toText(value));
    }, [value]);

    const commit = () => {
        if (level === undefined) {
            setText(toText(value));
            return;
        }
        if (level !== value) onCommit(level);
    };

    return (
        <TextField
            size="small"
            type="number"
            disabled={disabled}
            placeholder={placeholder}
            value={text}
            error={level === undefined}
            onChange={event => setText(event.target.value)}
            onBlur={commit}
            onKeyDown={event => {
                if (event.key === "Enter") commit();
            }}
            inputProps={{ min: 0, step: "any" }}
            sx={{ width: 140 }}
        />
    );
};

export default LevelInput;
