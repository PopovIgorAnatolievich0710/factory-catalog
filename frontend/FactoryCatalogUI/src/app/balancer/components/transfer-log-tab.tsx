import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import InputAdornment from "@mui/material/InputAdornment";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import TablePagination from "@mui/material/TablePagination";
import TextField from "@mui/material/TextField";
import SearchIcon from "@mui/icons-material/Search";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import { tableStyles } from "../../../components/data-table";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { formatDate, formatDateTime } from "../../../utils/format-date";
import { ListRequest } from "../../../entities/list";
import { TransferReason, transferReasonLabels, TransferReasons } from "../entities";
import { loadTransferLog } from "../store/actions";
import { deltaColor, Factory, formatSignedMoney, monoSx, PageTag, useActiveFactories } from "./balancer-ui";

const ALL = "all";
const PAGE_SIZES = [25, 50, 100];
const SEARCH_DELAY_MS = 400;

const TransferLogTab: React.FC = () => {
    const dispatch = useDispatch();
    const { factories } = useActiveFactories();
    const { transferLog, isTransferLogLoading } = useRootStateSelector(state => state.balancer);

    const [searchText, setSearchText] = useState("");
    const [search, setSearch] = useState("");
    const [supplier, setSupplier] = useState(ALL);
    const [reason, setReason] = useState<TransferReason | typeof ALL>(ALL);
    const [page, setPage] = useState(0);
    const [pageSize, setPageSize] = useState(PAGE_SIZES[0]);

    useEffect(() => {
        const timer = setTimeout(() => {
            setSearch(searchText.trim());
            setPage(0);
        }, SEARCH_DELAY_MS);
        return () => clearTimeout(timer);
    }, [searchText]);

    const request: ListRequest = useMemo(
        () => ({
            page: page + 1,
            pageSize,
            search,
            sortBy: "transferredAt",
            sortDirection: "desc",
            filters: {
                ...(supplier !== ALL ? { supplier: { values: [supplier] } } : {}),
                ...(reason !== ALL ? { reason: { values: [reason] } } : {}),
            },
        }),
        [page, pageSize, search, supplier, reason],
    );

    useEffect(() => {
        dispatch(loadTransferLog(request) as any);
    }, [dispatch, request]);

    return (
        <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
            <Box sx={{ padding: "12px 16px", display: "flex", gap: 1.25, alignItems: "center", borderBottom: "1px solid rgba(0, 0, 0, 0.08)", flexWrap: "wrap" }}>
                <TextField
                    size="small"
                    value={searchText}
                    onChange={event => setSearchText(event.target.value)}
                    placeholder="Search order, SKU, page, user or note…"
                    sx={{ width: 340 }}
                    InputProps={{ startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }}
                />
                <TextField select size="small" value={supplier} onChange={event => { setSupplier(event.target.value); setPage(0); }} sx={{ minWidth: 160 }}>
                    <MenuItem value={ALL}>All suppliers</MenuItem>
                    {factories.map(factory => (
                        <MenuItem key={factory.id} value={String(factory.id)}>
                            {factory.name}
                        </MenuItem>
                    ))}
                </TextField>
                <TextField
                    select
                    size="small"
                    value={reason}
                    onChange={event => { setReason(event.target.value as TransferReason); setPage(0); }}
                    sx={{ minWidth: 170 }}>
                    <MenuItem value={ALL}>All reasons</MenuItem>
                    {Object.values(TransferReasons).map(value => (
                        <MenuItem key={value} value={value}>
                            {transferReasonLabels[value]}
                        </MenuItem>
                    ))}
                </TextField>
            </Box>
            <Box component="table" sx={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                    <tr>
                        {["When", "Order · item", "Move (supplier · page)", "Reason", "By", "Quotes (old → new)", "Cost · ETA"].map(label => (
                            <Box component="th" key={label} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                                {label}
                            </Box>
                        ))}
                    </tr>
                </thead>
                <tbody>
                    {transferLog.items.map(entry => (
                        <tr key={entry.id}>
                            <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 12, color: "rgba(0, 0, 0, 0.6)", whiteSpace: "nowrap" }}>
                                {formatDateTime(entry.transferredAt)}
                            </Box>
                            <Box component="td" sx={tableStyles.bodyCell}>
                                <Box sx={{ ...monoSx, color: "#1976d2" }}>
                                    {entry.orderId} · {entry.orderItemId}
                                </Box>
                                <Box sx={{ ...monoSx, fontSize: 11.5, color: "rgba(0, 0, 0, 0.5)" }}>{entry.skuUuid}</Box>
                            </Box>
                            <Box component="td" sx={tableStyles.bodyCell}>
                                <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 0.75, flexWrap: "wrap" }}>
                                    <Factory id={entry.fromFactoryId} />
                                    <PageTag page={entry.fromPage} />
                                    <ArrowForwardIcon sx={{ fontSize: 15, color: "rgba(0, 0, 0, 0.5)" }} />
                                    <Factory id={entry.toFactoryId} />
                                    <PageTag page={entry.toPage} />
                                </Box>
                            </Box>
                            <Box component="td" sx={tableStyles.bodyCell}>
                                <Box sx={{ fontWeight: 500, fontSize: 13 }}>{transferReasonLabels[entry.reason] ?? entry.reason}</Box>
                                {entry.note && <Box sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>{entry.note}</Box>}
                            </Box>
                            <Box component="td" sx={tableStyles.bodyCell}>
                                <Box sx={{ fontSize: 13 }}>{entry.transferredBy}</Box>
                                <StatusChip
                                    label={entry.initiator}
                                    colors={entry.initiator === "Balancer" ? { background: "rgba(81, 45, 168, 0.1)", text: "#512da8" } : statusColors.neutral}
                                    small
                                />
                            </Box>
                            <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 12 }}>
                                <Box sx={{ color: "rgba(0, 0, 0, 0.55)", textDecoration: entry.newQuoteId ? "line-through" : "none" }}>{entry.oldQuoteId ?? "—"}</Box>
                                {entry.newQuoteId && <Box sx={{ color: "#1976d2" }}>{entry.newQuoteId}</Box>}
                            </Box>
                            <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 12 }}>
                                <Box component="span" sx={{ ...monoSx, color: deltaColor(entry.costDelta) }}>
                                    {formatSignedMoney(entry.costDelta, entry.currency)}
                                </Box>
                                <Box sx={{ color: "rgba(0, 0, 0, 0.6)" }}>
                                    ETA {formatDate(entry.oldEta)} → {formatDate(entry.newEta)}
                                </Box>
                            </Box>
                        </tr>
                    ))}
                    {!transferLog.items.length && !isTransferLogLoading && (
                        <tr>
                            <Box component="td" colSpan={7} sx={{ ...tableStyles.emptyCell, textAlign: "center" }}>
                                No transfers match.
                            </Box>
                        </tr>
                    )}
                </tbody>
            </Box>
            <TablePagination
                component="div"
                count={transferLog.totalCount}
                page={page}
                rowsPerPage={pageSize}
                rowsPerPageOptions={PAGE_SIZES}
                onPageChange={(_event, next) => setPage(next)}
                onRowsPerPageChange={event => { setPageSize(Number(event.target.value)); setPage(0); }}
            />
        </Paper>
    );
};

export default TransferLogTab;
