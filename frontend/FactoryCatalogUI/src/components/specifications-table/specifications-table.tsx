import React from "react";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableRow from "@mui/material/TableRow";
import { tableStyles } from "../data-table";

const LABEL_COLUMN_WIDTH = 260;

export interface SpecificationRow {
    label: string;
    value: React.ReactNode;
}

export interface SpecificationsTableProps {
    rows: SpecificationRow[];
}

const SpecificationsTable: React.FC<SpecificationsTableProps> = ({ rows }) => (
    <Table sx={{ tableLayout: "fixed" }}>
        <TableBody>
            {rows.map(row => (
                <TableRow key={row.label} sx={{ height: tableStyles.rowHeight }}>
                    <TableCell
                        style={{ width: LABEL_COLUMN_WIDTH }}
                        sx={{ ...tableStyles.bodyCell, color: "rgba(0, 0, 0, 0.6)" }}>
                        {row.label}
                    </TableCell>
                    <TableCell sx={{ ...tableStyles.bodyCell, fontWeight: 500 }}>{row.value ?? "-"}</TableCell>
                </TableRow>
            ))}
        </TableBody>
    </Table>
);

export default SpecificationsTable;
