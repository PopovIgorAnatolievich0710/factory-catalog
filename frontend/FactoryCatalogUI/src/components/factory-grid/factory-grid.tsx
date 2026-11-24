import React from "react";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import { tableStyles } from "../data-table";
import { useFactories } from "../../store/selectors/factoriesSelector";
import { Factory } from "../../entities/factory";

const ROW_HEIGHT = 56;
const FACTORY_COLUMN_WIDTH = 200;
const HIGHLIGHT_BACKGROUND = "rgba(25, 118, 210, 0.06)";
const DISABLED_OPACITY = 0.5;

export interface FactoryGridColumn {
    key: string;
    label: string;
    width?: number | string;
    align?: "left" | "center" | "right";
    render: (factory: Factory) => React.ReactNode;
}

export interface FactoryGridProps {
    columns: FactoryGridColumn[];
    factories?: Factory[];
    isRowHighlighted?: (factory: Factory) => boolean;
    isRowDisabled?: (factory: Factory) => boolean;
}

const FactoryGrid: React.FC<FactoryGridProps> = ({ columns, factories, isRowHighlighted, isRowDisabled }) => {
    const { factories: allFactories } = useFactories();
    const rows = factories ?? allFactories.filter(factory => factory.isActive);

    return (
        <TableContainer>
            <Table sx={{ tableLayout: "fixed" }}>
                <TableHead>
                    <TableRow>
                        <TableCell style={{ width: FACTORY_COLUMN_WIDTH }} sx={tableStyles.headCell}>
                            Factory
                        </TableCell>
                        {columns.map(column => (
                            <TableCell
                                key={column.key}
                                align={column.align}
                                style={{ width: column.width }}
                                sx={tableStyles.headCell}>
                                {column.label}
                            </TableCell>
                        ))}
                    </TableRow>
                </TableHead>
                <TableBody>
                    {rows.length === 0 && (
                        <TableRow sx={{ height: ROW_HEIGHT }}>
                            <TableCell colSpan={columns.length + 1} align="center" sx={tableStyles.emptyCell}>
                                No factories
                            </TableCell>
                        </TableRow>
                    )}
                    {rows.map(factory => {
                        const disabled = isRowDisabled?.(factory) ?? false;
                        return (
                            <TableRow
                                key={factory.id}
                                sx={{
                                    height: ROW_HEIGHT,
                                    background: isRowHighlighted?.(factory) ? HIGHLIGHT_BACKGROUND : undefined,
                                }}>
                                <TableCell
                                    sx={{ ...tableStyles.bodyCell, fontWeight: 500, opacity: disabled ? DISABLED_OPACITY : 1 }}>
                                    {factory.name} ({factory.countryCode})
                                </TableCell>
                                {columns.map(column => (
                                    <TableCell
                                        key={column.key}
                                        align={column.align}
                                        sx={{ ...tableStyles.bodyCell, opacity: disabled ? DISABLED_OPACITY : 1 }}>
                                        {column.render(factory)}
                                    </TableCell>
                                ))}
                            </TableRow>
                        );
                    })}
                </TableBody>
            </Table>
        </TableContainer>
    );
};

export default FactoryGrid;
