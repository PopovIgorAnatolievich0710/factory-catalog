import React, { useEffect, useState } from "react";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TablePagination from "@mui/material/TablePagination";
import TableRow from "@mui/material/TableRow";
import CloudSyncOutlinedIcon from "@mui/icons-material/CloudSyncOutlined";
import PersonOutlineIcon from "@mui/icons-material/PersonOutline";
import { tableStyles } from "../data-table";
import historyService from "../../services/history-service";
import { Api, catalogApi } from "../../helpers/api-helper";
import { useErrorNotification } from "../../helpers/response-helper";
import { useFactories } from "../../store/selectors/factoriesSelector";
import { HistoryEntry } from "../../entities/history";
import { CONSTANTS } from "../../core/CONSTANTS";
import { formatDateTime } from "../../utils/format-date";

export interface HistoryTabProps {
    entityType: string;
    entityId: string;
    api?: Api;
}

const HistoryTab: React.FC<HistoryTabProps> = ({ entityType, entityId, api = catalogApi }) => {
    const notifyError = useErrorNotification();
    const { factories } = useFactories();

    const [items, setItems] = useState<HistoryEntry[]>([]);
    const [totalCount, setTotalCount] = useState(0);
    const [page, setPage] = useState(CONSTANTS.PAGING.DEFAULT_PAGE);
    const [pageSize, setPageSize] = useState(CONSTANTS.PAGING.DEFAULT_PAGE_SIZE);
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        let cancelled = false;
        setIsLoading(true);
        historyService
            .fetch(entityType, entityId, page, pageSize, api)
            .then(response => {
                if (cancelled) return;
                setItems(response.items);
                setTotalCount(response.totalCount);
            })
            .catch(error => {
                if (!cancelled) notifyError(error);
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [entityType, entityId, page, pageSize, api]);

    const getFactoryName = (factoryId: string | null) => {
        if (factoryId === null) return "-";
        return factories.find(factory => String(factory.id) === String(factoryId))?.name ?? factoryId;
    };

    return (
        <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
            <Box sx={{ height: 4 }}>{isLoading && <LinearProgress />}</Box>
            <TableContainer>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell sx={tableStyles.headCell}>When</TableCell>
                            <TableCell sx={tableStyles.headCell}>Who</TableCell>
                            <TableCell sx={tableStyles.headCell}>Factory</TableCell>
                            <TableCell sx={tableStyles.headCell}>Field</TableCell>
                            <TableCell sx={tableStyles.headCell}>Old value</TableCell>
                            <TableCell sx={tableStyles.headCell}>New value</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {items.length === 0 && !isLoading && (
                            <TableRow>
                                <TableCell colSpan={6} align="center" sx={tableStyles.emptyCell}>
                                    No changes recorded yet.
                                </TableCell>
                            </TableRow>
                        )}
                        {items.map((entry, index) => (
                            <TableRow key={`${entry.changedAt}_${entry.field}_${index}`}>
                                <TableCell
                                    sx={{
                                        ...tableStyles.bodyCell,
                                        ...tableStyles.monospace,
                                        color: "rgba(0, 0, 0, 0.6)",
                                        whiteSpace: "nowrap",
                                    }}>
                                    {formatDateTime(entry.changedAt)}
                                </TableCell>
                                <TableCell sx={tableStyles.bodyCell}>
                                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                                        {entry.source === "User" ? (
                                            <PersonOutlineIcon sx={{ fontSize: 16, color: "rgba(0, 0, 0, 0.45)" }} />
                                        ) : (
                                            <CloudSyncOutlinedIcon sx={{ fontSize: 16, color: "#00695c" }} />
                                        )}
                                        {entry.source === "User" ? entry.changedBy : `${entry.source} · ${entry.changedBy}`}
                                    </Box>
                                </TableCell>
                                <TableCell sx={tableStyles.bodyCell}>{getFactoryName(entry.factoryId)}</TableCell>
                                <TableCell sx={tableStyles.bodyCell}>{entry.field}</TableCell>
                                <TableCell
                                    sx={{ ...tableStyles.bodyCell, color: "rgba(0, 0, 0, 0.5)", textDecoration: "line-through" }}>
                                    {entry.oldValue ?? "-"}
                                </TableCell>
                                <TableCell sx={{ ...tableStyles.bodyCell, fontWeight: 500 }}>
                                    {entry.newValue ?? "-"}
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
            <TablePagination
                component="div"
                rowsPerPageOptions={CONSTANTS.PAGING.PAGE_SIZE_OPTIONS}
                count={totalCount}
                rowsPerPage={pageSize}
                page={totalCount === 0 ? 0 : page - 1}
                onPageChange={(_event, nextPage) => setPage(nextPage + 1)}
                onRowsPerPageChange={event => {
                    setPageSize(Number.parseInt(event.target.value, 10));
                    setPage(CONSTANTS.PAGING.DEFAULT_PAGE);
                }}
            />
        </Paper>
    );
};

export default HistoryTab;
