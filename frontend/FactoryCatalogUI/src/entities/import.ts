export interface ImportRowError {
    row: number;
    field: string | null;
    message: string;
}

export interface ImportValidationResult {
    totalRows: number;
    validRows: number;
    errors: ImportRowError[];
}
