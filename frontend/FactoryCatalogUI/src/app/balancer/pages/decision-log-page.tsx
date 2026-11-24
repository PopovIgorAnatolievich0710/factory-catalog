import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import InputAdornment from "@mui/material/InputAdornment";
import LinearProgress from "@mui/material/LinearProgress";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import TablePagination from "@mui/material/TablePagination";
import TextField from "@mui/material/TextField";
import SearchIcon from "@mui/icons-material/Search";
import PlaceOutlinedIcon from "@mui/icons-material/PlaceOutlined";
import EmojiEventsOutlinedIcon from "@mui/icons-material/EmojiEventsOutlined";
import { tableStyles } from "../../../components/data-table";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { formatDate, formatDateTime } from "../../../utils/format-date";
import { ListRequest } from "../../../entities/list";
import { Allocation, AllocationModes, Candidate } from "../entities";
import { loadDecisions } from "../store/actions";
import { Caption, Factory, formatMoney, ModeChip, monoSx, PageTitle, pageSx, useActiveFactories } from "../components/balancer-ui";

const ALL = "all";
const PAGE_SIZES = [25, 50, 100];
const SEARCH_DELAY_MS = 400;

const candidateNote = (candidate: Candidate, allocation: Allocation): string => {
    if (candidate.excludedReason) return candidate.excludedReason.split(",").join(", ");
    if (candidate.isWinner) return allocation.decisionReason;
    return "Feasible, not selected";
};

const etaText = (from: string | null, to: string | null): string =>
    from && to && from !== to ? `${formatDate(from)} – ${formatDate(to)}` : formatDate(to ?? from);

const CandidatesTable: React.FC<{ allocation: Allocation }> = ({ allocation }) => (
    <Box component="table" sx={{ width: "100%", borderCollapse: "collapse", border: "1px solid rgba(0, 0, 0, 0.08)" }}>
        <thead>
            <tr>
                {["Candidate", "Production cost", "Shipping cost", "Total", "ETA", "Share · capacity", "Excluded because / why"].map(label => (
                    <Box component="th" key={label} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                        {label}
                    </Box>
                ))}
            </tr>
        </thead>
        <tbody>
            {allocation.candidates.map(candidate => (
                <Box
                    component="tr"
                    key={candidate.factoryId}
                    sx={{ background: candidate.isWinner ? "rgba(46, 125, 50, 0.06)" : "transparent", opacity: candidate.excludedReason ? 0.75 : 1 }}>
                    <Box component="td" sx={tableStyles.bodyCell}>
                        <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 0.75 }}>
                            <Factory id={candidate.factoryId} />
                            {candidate.isWinner && <EmojiEventsOutlinedIcon sx={{ fontSize: 16, color: "#2e7d32" }} />}
                        </Box>
                    </Box>
                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>
                        {formatMoney(candidate.productionCost, candidate.currency)}
                    </Box>
                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>
                        {formatMoney(candidate.shippingCost, candidate.currency)}
                    </Box>
                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontWeight: candidate.isWinner ? 600 : 400 }}>
                        {formatMoney(candidate.totalCost, candidate.currency)}
                    </Box>
                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 13 }}>
                        {etaText(candidate.etaFrom, candidate.etaTo)}
                    </Box>
                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 13 }}>
                        {candidate.sharePct}% · {candidate.capacityPct ?? "—"}%
                    </Box>
                    <Box
                        component="td"
                        sx={{ ...tableStyles.bodyCell, fontSize: 13, color: candidate.excludedReason ? "#c62828" : "rgba(0, 0, 0, 0.6)" }}>
                        {candidateNote(candidate, allocation)}
                    </Box>
                </Box>
            ))}
        </tbody>
    </Box>
);

const AllocationRow: React.FC<{ allocation: Allocation; isLast: boolean }> = ({ allocation, isLast }) => (
    <Box sx={{ padding: "14px 18px", borderBottom: isLast ? "none" : "1px dashed rgba(0, 0, 0, 0.1)" }}>
        <Box sx={{ display: "flex", gap: 2.25, alignItems: "center", flexWrap: "wrap", marginBottom: 1.25 }}>
            <Box sx={{ minWidth: 220, flex: 1 }}>
                <Box sx={{ fontWeight: 500 }}>Item {allocation.orderItemId}</Box>
                <Box sx={{ ...monoSx, fontSize: 11.5, color: "rgba(0, 0, 0, 0.5)" }}>{allocation.skuUuid}</Box>
            </Box>
            <Caption label="Winner">
                <Factory id={allocation.factoryId} />
            </Caption>
            <Caption label="Packing">
                <Factory id={allocation.packingFacilityId} />
            </Caption>
            <Caption label="Total cost">
                <Box component="b" sx={monoSx}>
                    {formatMoney(allocation.totalCost, allocation.currency)}
                </Box>
            </Caption>
            <Caption label="ETA">{etaText(allocation.etaFrom, allocation.etaTo)}</Caption>
            <Caption label="Mode">
                <ModeChip mode={allocation.mode} />
            </Caption>
            {allocation.fulfillmentType !== "Factory" && (
                <StatusChip label={`${allocation.fulfillmentType} · not routed`} colors={statusColors.neutral} small />
            )}
            {allocation.isSplit && <StatusChip label="Split order" colors={statusColors.warning} small />}
            {allocation.transferCount > 0 && (
                <StatusChip label={`Transferred · from ${allocation.originalFactoryId}`} colors={statusColors.info} small />
            )}
            {allocation.legacyFactoryId && (
                <Box component="span" sx={{ fontSize: 12, color: "#512da8" }}>
                    Legacy picked {allocation.legacyFactoryId}
                </Box>
            )}
        </Box>
        {allocation.fallbackReason && (
            <Box sx={{ fontSize: 12, color: "#b45309", marginBottom: 1 }}>Fallback: {allocation.fallbackReason}</Box>
        )}
        {allocation.candidates.length > 0 ? (
            <CandidatesTable allocation={allocation} />
        ) : (
            <Box sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)" }}>{allocation.decisionReason}</Box>
        )}
        {allocation.selectedQuoteId && (
            <Box sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.55)", marginTop: 1 }}>
                Quote <Box component="span" sx={{ ...monoSx, color: "#1976d2" }}>{allocation.selectedQuoteId}</Box>
            </Box>
        )}
    </Box>
);

const DecisionLogPage: React.FC = () => {
    const dispatch = useDispatch();
    const { factories } = useActiveFactories();
    const { decisions, isDecisionsLoading } = useRootStateSelector(state => state.balancer);

    const [searchText, setSearchText] = useState("");
    const [search, setSearch] = useState("");
    const [mode, setMode] = useState(ALL);
    const [factoryId, setFactoryId] = useState(ALL);
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
            sortBy: "decidedAt",
            sortDirection: "desc",
            filters: {
                ...(mode !== ALL ? { mode: { values: [mode] } } : {}),
                ...(factoryId !== ALL ? { factoryId: { values: [factoryId] } } : {}),
            },
        }),
        [page, pageSize, search, mode, factoryId],
    );

    useEffect(() => {
        dispatch(loadDecisions(request) as any);
    }, [dispatch, request]);

    const orders = useMemo(() => {
        const grouped = new Map<string, Allocation[]>();
        decisions.items.forEach(allocation => grouped.set(allocation.orderId, [...(grouped.get(allocation.orderId) ?? []), allocation]));
        return Array.from(grouped.entries());
    }, [decisions]);

    return (
        <Box sx={pageSx}>
            <PageTitle title="Balancer decision log" />

            <Box sx={{ display: "flex", gap: 1.25, alignItems: "center", flexWrap: "wrap" }}>
                <TextField
                    size="small"
                    value={searchText}
                    onChange={event => setSearchText(event.target.value)}
                    placeholder="Search by order ID…"
                    sx={{ width: 340 }}
                    InputProps={{ startAdornment: <InputAdornment position="start"><SearchIcon fontSize="small" /></InputAdornment> }}
                />
                <TextField select size="small" value={mode} onChange={event => { setMode(event.target.value); setPage(0); }} sx={{ minWidth: 140 }}>
                    <MenuItem value={ALL}>All modes</MenuItem>
                    {Object.values(AllocationModes).map(value => (
                        <MenuItem key={value} value={value}>
                            {value}
                        </MenuItem>
                    ))}
                </TextField>
                <TextField select size="small" value={factoryId} onChange={event => { setFactoryId(event.target.value); setPage(0); }} sx={{ minWidth: 160 }}>
                    <MenuItem value={ALL}>All factories</MenuItem>
                    {factories.map(factory => (
                        <MenuItem key={factory.id} value={String(factory.id)}>
                            {factory.name}
                        </MenuItem>
                    ))}
                </TextField>
            </Box>

            <Box sx={{ height: 4 }}>{isDecisionsLoading && <LinearProgress />}</Box>

            {orders.map(([orderId, allocations]) => (
                <Paper key={orderId} variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
                    <Box sx={{ padding: "12px 18px", display: "flex", gap: 1.75, alignItems: "center", borderBottom: "1px solid rgba(0, 0, 0, 0.08)", flexWrap: "wrap" }}>
                        <Box component="b" sx={{ ...monoSx, fontSize: 14, fontWeight: 600 }}>
                            {orderId}
                        </Box>
                        <Box component="span" sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)" }}>
                            {formatDateTime(allocations[0].decidedAt)}
                        </Box>
                        <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 0.5, fontSize: 13, color: "rgba(0, 0, 0, 0.7)" }}>
                            <PlaceOutlinedIcon sx={{ fontSize: 15 }} />
                            {allocations[0].destinationCountry}
                        </Box>
                        {allocations[0].slaDeliveryDate && (
                            <StatusChip label={`SLA ${formatDate(allocations[0].slaDeliveryDate)}`} colors={statusColors.info} small />
                        )}
                        {allocations[0].isEventOrder && <StatusChip label="Event order" colors={statusColors.warning} small />}
                        <Box sx={{ flex: 1 }} />
                        <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.55)" }}>
                            {allocations.length} item{allocations.length > 1 ? "s" : ""}
                        </Box>
                    </Box>
                    {allocations.map((allocation, index) => (
                        <AllocationRow key={allocation.id} allocation={allocation} isLast={index === allocations.length - 1} />
                    ))}
                </Paper>
            ))}

            {!orders.length && !isDecisionsLoading && (
                <Paper variant="outlined" sx={{ borderRadius: "8px", padding: 4, textAlign: "center", color: "rgba(0, 0, 0, 0.5)" }}>
                    No decisions match this search.
                </Paper>
            )}

            <TablePagination
                component="div"
                count={decisions.totalCount}
                page={page}
                rowsPerPage={pageSize}
                rowsPerPageOptions={PAGE_SIZES}
                labelRowsPerPage="Items per page"
                onPageChange={(_event, next) => setPage(next)}
                onRowsPerPageChange={event => { setPageSize(Number(event.target.value)); setPage(0); }}
            />
        </Box>
    );
};

export default DecisionLogPage;
