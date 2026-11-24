import React, { useEffect, useState } from "react";
import TextField, { TextFieldProps } from "@mui/material/TextField";

type CommitTextFieldProps = Omit<TextFieldProps, "value" | "onChange" | "onBlur"> & {
    value: string;
    onCommit: (value: string) => void;
};

const CommitTextField: React.FC<CommitTextFieldProps> = ({ value, onCommit, multiline, ...props }) => {
    const [draft, setDraft] = useState(value);

    useEffect(() => {
        setDraft(value);
    }, [value]);

    const commit = () => {
        const next = draft.trim();
        if (next !== value) {
            onCommit(next);
        } else {
            setDraft(value);
        }
    };

    return (
        <TextField
            size="small"
            {...props}
            multiline={multiline}
            value={draft}
            onChange={event => setDraft(event.target.value)}
            onBlur={commit}
            onKeyDown={event => {
                if (event.key === "Enter" && !multiline) (event.target as HTMLElement).blur();
            }}
        />
    );
};

export default CommitTextField;
