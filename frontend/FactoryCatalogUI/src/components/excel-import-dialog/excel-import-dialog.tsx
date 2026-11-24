import React, { useEffect, useRef, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import LinearProgress from "@mui/material/LinearProgress";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import { PrimaryBoButton } from "../../../bo-frontend/src/components/Buttons/PrimaryBoButton";
import { SecondaryBoButton } from "../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { CancelBoButton } from "../../../bo-frontend/src/components/Buttons/CancelBoButton";
import { useErrorNotification } from "../../helpers/response-helper";
import { ImportValidationResult } from "../../entities/import";

const EXCEL_FILE_TYPES = ".xlsx";

export interface ExcelImportDialogProps {
    open: boolean;
    title: string;
    description?: string;
    onClose: () => void;
    onValidate: (file: File) => Promise<ImportValidationResult>;
    onConfirm: (file: File) => Promise<void>;
}

const ExcelImportDialog: React.FC<ExcelImportDialogProps> = ({
    open,
    title,
    description,
    onClose,
    onValidate,
    onConfirm,
}) => {
    const notifyError = useErrorNotification();
    const fileInput = useRef<HTMLInputElement>(null);

    const [file, setFile] = useState<File | null>(null);
    const [validation, setValidation] = useState<ImportValidationResult | null>(null);
    const [isBusy, setIsBusy] = useState(false);

    useEffect(() => {
        if (open) {
            setFile(null);
            setValidation(null);
        }
    }, [open]);

    const onFileSelected = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const selected = event.target.files?.[0] ?? null;
        event.target.value = "";
        if (!selected) return;

        setFile(selected);
        setValidation(null);
        setIsBusy(true);
        try {
            setValidation(await onValidate(selected));
        } catch (error) {
            setFile(null);
            notifyError(error);
        } finally {
            setIsBusy(false);
        }
    };

    const onConfirmClick = async () => {
        setIsBusy(true);
        try {
            await onConfirm(file);
            onClose();
        } catch (error) {
            notifyError(error);
        } finally {
            setIsBusy(false);
        }
    };

    const hasErrors = validation !== null && validation.errors.length > 0;
    const canConfirm = validation !== null && !hasErrors && validation.validRows > 0 && !isBusy;

    return (
        <Dialog open={open} onClose={isBusy ? undefined : onClose} maxWidth="md" fullWidth>
            <DialogTitle>{title}</DialogTitle>
            <DialogContent>
                {description && (
                    <Typography variant="body2" sx={{ color: "#4B5563", marginBottom: 2 }}>
                        {description}
                    </Typography>
                )}

                <Box sx={{ display: "flex", alignItems: "center", gap: 2, marginBottom: 2 }}>
                    <input
                        ref={fileInput}
                        type="file"
                        accept={EXCEL_FILE_TYPES}
                        hidden
                        onChange={onFileSelected}
                    />
                    <SecondaryBoButton
                        startIcon={<UploadFileIcon />}
                        disabled={isBusy}
                        onClick={() => fileInput.current?.click()}>
                        {file ? "Choose another file" : "Choose file"}
                    </SecondaryBoButton>
                    {file && <Typography variant="body2">{file.name}</Typography>}
                </Box>

                <Box sx={{ height: 4, marginBottom: 1 }}>{isBusy && <LinearProgress />}</Box>

                {validation && !hasErrors && (
                    <Alert severity="success">
                        {validation.validRows} of {validation.totalRows} rows are valid and ready to import.
                    </Alert>
                )}

                {hasErrors && (
                    <>
                        <Alert severity="error" sx={{ marginBottom: 1 }}>
                            {validation.errors.length} problem(s) found in {validation.totalRows} rows.
                            Fix the file and upload it again.
                        </Alert>
                        <TableContainer sx={{ maxHeight: 360 }}>
                            <Table size="small" stickyHeader>
                                <TableHead>
                                    <TableRow>
                                        <TableCell style={{ width: 80 }}>Row</TableCell>
                                        <TableCell style={{ width: 200 }}>Field</TableCell>
                                        <TableCell>Problem</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {validation.errors.map((error, index) => (
                                        <TableRow key={`${error.row}_${error.field}_${index}`}>
                                            <TableCell>{error.row}</TableCell>
                                            <TableCell>{error.field ?? "-"}</TableCell>
                                            <TableCell>{error.message}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </>
                )}
            </DialogContent>
            <DialogActions>
                <CancelBoButton disabled={isBusy} onClick={onClose}>
                    Cancel
                </CancelBoButton>
                <PrimaryBoButton disabled={!canConfirm} onClick={onConfirmClick}>
                    Import
                </PrimaryBoButton>
            </DialogActions>
        </Dialog>
    );
};

export default ExcelImportDialog;
