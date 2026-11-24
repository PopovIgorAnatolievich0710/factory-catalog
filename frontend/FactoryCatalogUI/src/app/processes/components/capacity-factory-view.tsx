import React, { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import { Link as RouterLink } from "react-router-dom";
import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import Paper from "@mui/material/Paper";
import Slider from "@mui/material/Slider";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import TextField from "@mui/material/TextField";
import { tableStyles } from "../../../components/data-table";
import LockTag from "../../../components/lock-tag";
import { CapacityChip } from "../../../components/status-chips";
import { PROCESSES_URL } from "../../../constants/urls";
import { Factory } from "../../../entities/factory";
import { formatDate } from "../../../utils/format-date";
import CommitTextField from "./commit-text-field";
import { getCapacityColor, ProcessTypeChip, TodayLine } from "./process-chips";
import { saveCapacity } from "../store/actions";
import { CAPACITY_STEP, CapacityCell, CapacityRow, FULL_CAPACITY, SetCapacityRequest } from "../entities";

const OFFLINE_ROW_TINT = "rgba(211, 47, 47, 0.035)";
const REASON_MAX_LENGTH = 1000;

const noteStyle = { fontSize: 11, color: "rgba(0, 0, 0, 0.5)" };

interface CapacitySliderProps {
    value: number;
    disabled: boolean;
    onCommit: (value: number) => void;
}

const CapacitySlider: React.FC<CapacitySliderProps> = ({ value, disabled, onCommit }) => {
    const [draft, setDraft] = useState(value);

    useEffect(() => {
        setDraft(value);
    }, [value]);

    const color = getCapacityColor(draft);

    return (
        <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
            <Slider
                size="small"
                min={0}
                max={FULL_CAPACITY}
                step={CAPACITY_STEP}
                marks
                value={draft}
                disabled={disabled}
                onChange={(_event, next) => setDraft(next as number)}
                onChangeCommitted={(_event, next) => next !== value && onCommit(next as number)}
                sx={{ flex: 1, minWidth: 200, color: disabled ? undefined : color }}
            />
            <Box component="b" sx={{ fontWeight: 500, width: 44, textAlign: "right", color }}>
                {draft}%
            </Box>
        </Box>
    );
};

interface CapacityFactoryViewProps {
    factory: Factory;
    rows: CapacityRow[];
    editable: boolean;
}

const CapacityFactoryView: React.FC<CapacityFactoryViewProps> = ({ factory, rows, editable }) => {
    const dispatch = useDispatch();

    const save = (row: CapacityRow, cell: CapacityCell, patch: Partial<SetCapacityRequest>) => {
        dispatch(
            saveCapacity(String(factory.id), row.processId, {
                capacityPct: cell.capacityPct,
                reason: cell.reason,
                expectedRecovery: cell.expectedRecovery,
                ...patch,
            }) as any,
        );
    };

    const cells = rows.map(row => row.cells[0]);
    const offlineCount = cells.filter(cell => cell.capacityPct <= 0).length;
    const reducedCount = cells.filter(cell => cell.capacityPct > 0 && cell.capacityPct < FULL_CAPACITY).length;

    return (
        <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
            <Box sx={{ padding: "12px 16px", display: "flex", alignItems: "center", gap: 1.75, flexWrap: "wrap", borderBottom: "1px solid rgba(0, 0, 0, 0.08)" }}>
                <Box component="b" sx={{ fontWeight: 500 }}>
                    {rows.length} processes run at {factory.name}
                </Box>
                <Box component="span" sx={{ fontSize: 13, color: "#c62828" }}>
                    {offlineCount} offline
                </Box>
                <Box component="span" sx={{ fontSize: 13, color: "#b45309" }}>
                    {reducedCount} reduced
                </Box>
                <Box sx={{ flex: 1 }} />
                {!editable && <LockTag text={`Only the ${factory.name} factory manager edits`} />}
            </Box>
            <TableContainer>
                <Table sx={{ tableLayout: "fixed", minWidth: 1180 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell style={{ width: 220 }} sx={tableStyles.headCell}>Process</TableCell>
                            <TableCell style={{ width: 120 }} sx={tableStyles.headCell}>Type</TableCell>
                            <TableCell style={{ width: 340 }} sx={tableStyles.headCell}>Capacity</TableCell>
                            <TableCell style={{ width: 150 }} sx={tableStyles.headCell}>Status</TableCell>
                            <TableCell sx={tableStyles.headCell}>Reason · expected recovery</TableCell>
                            <TableCell style={{ width: 220 }} sx={tableStyles.headCell}>Last changed</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.length === 0 && (
                            <TableRow>
                                <TableCell colSpan={6} align="center" sx={tableStyles.emptyCell}>
                                    No processes run at this factory. Set &quot;Runs here&quot; on the process Factories tab.
                                </TableCell>
                            </TableRow>
                        )}
                        {rows.map(row => {
                            const cell = row.cells[0];
                            const isReduced = cell.capacityPct < FULL_CAPACITY;

                            return (
                                <TableRow
                                    key={row.processId}
                                    sx={{ height: 92, background: cell.capacityPct <= 0 ? OFFLINE_ROW_TINT : undefined }}>
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
                                    <TableCell sx={tableStyles.bodyCell}>
                                        <CapacitySlider
                                            value={cell.capacityPct}
                                            disabled={!editable}
                                            onCommit={capacityPct => save(row, cell, { capacityPct })}
                                        />
                                        <TodayLine today={cell.today} maxPerDay={cell.maxPerDay} showBar />
                                    </TableCell>
                                    <TableCell sx={tableStyles.bodyCell}>
                                        <CapacityChip percent={cell.capacityPct} />
                                    </TableCell>
                                    <TableCell sx={tableStyles.bodyCell}>
                                        {!isReduced && <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.35)" }}>-</Box>}
                                        {isReduced && editable && (
                                            <Box sx={{ display: "flex", flexDirection: "column", gap: "5px" }}>
                                                <Box sx={{ display: "flex", gap: 1 }}>
                                                    <CommitTextField
                                                        placeholder="Reason (optional)"
                                                        value={cell.reason ?? ""}
                                                        onCommit={reason => save(row, cell, { reason: reason || null })}
                                                        inputProps={{ maxLength: REASON_MAX_LENGTH }}
                                                        sx={{ flex: 1 }}
                                                    />
                                                    <TextField
                                                        size="small"
                                                        type="date"
                                                        title="Expected recovery (optional)"
                                                        value={cell.expectedRecovery ?? ""}
                                                        onChange={event =>
                                                            save(row, cell, { expectedRecovery: event.target.value || null })
                                                        }
                                                        sx={{ width: 160 }}
                                                    />
                                                </Box>
                                                <span style={noteStyle}>
                                                    For information only — capacity changes only when you change it.
                                                </span>
                                            </Box>
                                        )}
                                        {isReduced && !editable && (
                                            <Box sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.7)" }}>
                                                {cell.reason || <Box component="span" sx={{ color: "rgba(0, 0, 0, 0.4)" }}>No reason given</Box>}
                                                {cell.expectedRecovery && (
                                                    <Box sx={{ color: "rgba(0, 0, 0, 0.5)" }}>
                                                        Expected recovery: {formatDate(cell.expectedRecovery)}
                                                    </Box>
                                                )}
                                            </Box>
                                        )}
                                    </TableCell>
                                    <TableCell sx={{ ...tableStyles.bodyCell, fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                                        {cell.updatedBy ? (
                                            <>
                                                Last changed by {cell.updatedBy}
                                                <br />
                                                on {formatDate(cell.updatedAt)}
                                            </>
                                        ) : (
                                            "Never changed"
                                        )}
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
        </Paper>
    );
};

export default CapacityFactoryView;
