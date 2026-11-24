import React from "react";
import { Link as RouterLink } from "react-router-dom";
import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Tooltip from "@mui/material/Tooltip";
import { tableStyles } from "../../../components/data-table";
import { FactoryTag } from "../../../components/status-chips";
import { PROCESSES_URL } from "../../../constants/urls";
import { Factory } from "../../../entities/factory";
import { formatDate } from "../../../utils/format-date";
import { getCapacityColor, ProcessTypeChip } from "./process-chips";
import { CapacityCell, CapacityRow, FULL_CAPACITY } from "../entities";

const ROW_HEIGHT = 56;
const PROCESS_COLUMN_WIDTH = 240;
const TYPE_COLUMN_WIDTH = 130;
const OFFLINE_TINT = "rgba(211, 47, 47, 0.06)";
const REDUCED_TINT = "rgba(237, 108, 2, 0.05)";

const legend = [
    { label: "0% offline", background: "#d32f2f" },
    { label: "Reduced", background: "rgba(237, 108, 2, 0.16)" },
    { label: "100%", background: "#ffffff" },
    { label: "Doesn't run", background: "rgba(0, 0, 0, 0.04)" },
];

const getCellTooltip = (cell: CapacityCell): string => {
    if (cell.capacityPct >= FULL_CAPACITY) return "";
    const recovery = cell.expectedRecovery ? ` · expected recovery ${formatDate(cell.expectedRecovery)}` : "";
    return `${cell.reason || "No reason given"}${recovery}`;
};

const GridCell: React.FC<{ cell: CapacityCell | undefined }> = ({ cell }) => {
    if (!cell?.runsHere) {
        return (
            <TableCell sx={{ ...tableStyles.bodyCell, color: "rgba(0, 0, 0, 0.3)", fontSize: 12 }}>
                Doesn&apos;t run here
            </TableCell>
        );
    }

    const color = getCapacityColor(cell.capacityPct);
    const isFull = cell.capacityPct >= FULL_CAPACITY;
    const isOffline = cell.capacityPct <= 0;

    return (
        <Tooltip title={getCellTooltip(cell)}>
            <TableCell
                sx={{
                    ...tableStyles.bodyCell,
                    background: isOffline ? OFFLINE_TINT : isFull ? undefined : REDUCED_TINT,
                }}>
                <Box sx={{ display: "flex", alignItems: "center", gap: "10px" }}>
                    <Box sx={{ flex: 1, height: 6, borderRadius: "3px", background: "rgba(0, 0, 0, 0.08)", overflow: "hidden" }}>
                        <Box sx={{ width: `${cell.capacityPct}%`, height: "100%", background: color }} />
                    </Box>
                    <Box
                        component="b"
                        sx={{
                            width: 38,
                            textAlign: "right",
                            fontSize: 13,
                            fontWeight: isFull ? 400 : 600,
                            color: isFull ? "rgba(0, 0, 0, 0.6)" : color,
                        }}>
                        {cell.capacityPct}%
                    </Box>
                </Box>
                {!isFull && (
                    <Box sx={{ fontSize: 11, color: isOffline ? "#c62828" : "#b45309", marginTop: "3px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                        {isOffline ? "Offline" : "Reduced"}
                        {cell.expectedRecovery ? ` · until ${formatDate(cell.expectedRecovery)}` : ""}
                    </Box>
                )}
                {!isOffline && cell.today && (
                    <Box sx={{ fontSize: 11, color: "rgba(0, 0, 0, 0.5)", marginTop: "2px", whiteSpace: "nowrap" }}>
                        {cell.today.remainingPct}% left today
                    </Box>
                )}
            </TableCell>
        </Tooltip>
    );
};

interface CapacityGridProps {
    rows: CapacityRow[];
    factories: Factory[];
}

const CapacityGrid: React.FC<CapacityGridProps> = ({ rows, factories }) => {
    const belowFull = rows.reduce(
        (count, row) => count + row.cells.filter(cell => cell.runsHere && cell.capacityPct < FULL_CAPACITY).length,
        0,
    );

    return (
        <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
            <Box sx={{ padding: "12px 16px", display: "flex", alignItems: "center", gap: 2, flexWrap: "wrap", fontSize: 13, borderBottom: "1px solid rgba(0, 0, 0, 0.08)" }}>
                <Box component="b" sx={{ fontWeight: 500, fontSize: 14 }}>
                    All factories · current capacity
                </Box>
                <Box component="span" sx={{ color: "rgba(0, 0, 0, 0.6)" }}>
                    {belowFull} process–factory pairs below 100%
                </Box>
                <Box sx={{ flex: 1 }} />
                {legend.map(item => (
                    <Box key={item.label} component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "6px", fontSize: 12 }}>
                        <Box component="span" sx={{ width: 16, height: 12, borderRadius: "3px", background: item.background, border: "1px solid rgba(0, 0, 0, 0.12)" }} />
                        {item.label}
                    </Box>
                ))}
            </Box>
            <TableContainer>
                <Table sx={{ tableLayout: "fixed", minWidth: PROCESS_COLUMN_WIDTH + TYPE_COLUMN_WIDTH + factories.length * 170 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell style={{ width: PROCESS_COLUMN_WIDTH }} sx={tableStyles.headCell}>
                                Process
                            </TableCell>
                            <TableCell style={{ width: TYPE_COLUMN_WIDTH }} sx={tableStyles.headCell}>
                                Type
                            </TableCell>
                            {factories.map(factory => (
                                <TableCell key={factory.id} sx={tableStyles.headCell}>
                                    <Tooltip title={factory.name}>
                                        <span>
                                            <FactoryTag code={factory.countryCode ?? factory.id} />
                                        </span>
                                    </Tooltip>
                                </TableCell>
                            ))}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.length === 0 && (
                            <TableRow sx={{ height: ROW_HEIGHT }}>
                                <TableCell colSpan={factories.length + 2} align="center" sx={tableStyles.emptyCell}>
                                    No processes
                                </TableCell>
                            </TableRow>
                        )}
                        {rows.map(row => (
                            <TableRow key={row.processId} sx={{ height: ROW_HEIGHT }}>
                                <TableCell sx={{ ...tableStyles.bodyCell, fontWeight: 500 }}>
                                    <Link
                                        component={RouterLink}
                                        to={`${PROCESSES_URL}/${encodeURIComponent(row.processId)}`}
                                        underline="hover"
                                        color="inherit">
                                        {row.name}
                                    </Link>
                                </TableCell>
                                <TableCell sx={tableStyles.bodyCell}>
                                    <ProcessTypeChip type={row.type} />
                                </TableCell>
                                {factories.map(factory => (
                                    <GridCell
                                        key={factory.id}
                                        cell={row.cells.find(cell => cell.factoryId === String(factory.id))}
                                    />
                                ))}
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Paper>
    );
};

export default CapacityGrid;
