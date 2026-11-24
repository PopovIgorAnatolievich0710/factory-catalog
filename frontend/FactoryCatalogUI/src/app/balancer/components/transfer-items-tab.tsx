import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import Link from "@mui/material/Link";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import TablePagination from "@mui/material/TablePagination";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import SearchIcon from "@mui/icons-material/Search";
import SwapHorizIcon from "@mui/icons-material/SwapHoriz";
import SectionCard from "../../../components/section-card";
import { tableStyles } from "../../../components/data-table";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { useErrorNotification } from "../../../helpers/response-helper";
import { notificationSuccess } from "../../notifications/actions";
import { formatDate } from "../../../utils/format-date";
import { ListFilters, ListRequest } from "../../../entities/list";
import { OpenItem, OpenItemsQuery, TransferItem, TransferReason, transferReasonLabels, TransferReasons, TransferRequest } from "../entities";
import balancerService from "../services/balancer-service";
import { loadOpenItems } from "../store/actions";
import { deltaColor, Factory, formatSignedMoney, monoSx, PageTag, useActiveFactories } from "./balancer-ui";

const PAGE_SIZE = 25;
const PREVIEW_ROWS = 20;

const emptyQuery: OpenItemsQuery = { orderId: "", factoryId: "", productionPage: "", eventName: "", site: "", dueDate: "" };

const toFilters = (query: OpenItemsQuery): ListFilters => ({
    ...(query.orderId.trim() ? { orderId: { operator: "contains", value: query.orderId.trim() } } : {}),
    ...(query.factoryId ? { factoryId: { values: [query.factoryId] } } : {}),
    ...(query.productionPage ? { productionPage: { values: [query.productionPage] } } : {}),
    ...(query.eventName.trim() ? { eventName: { operator: "contains", value: query.eventName.trim() } } : {}),
    ...(query.site.trim() ? { site: { operator: "contains", value: query.site.trim() } } : {}),
    ...(query.dueDate ? { dueDate: { operator: "eq", value: query.dueDate } } : {}),
});

const usePages = (factoryId: string) => {
    const [pages, setPages] = useState<string[]>([]);
    useEffect(() => {
        if (!factoryId) {
            setPages([]);
            return;
        }
        let isCurrent = true;
        balancerService.fetchPages(factoryId).then(result => isCurrent && setPages(result), () => isCurrent && setPages([]));
        return () => {
            isCurrent = false;
        };
    }, [factoryId]);
    return pages;
};

const Field: React.FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => (
    <Box component="label" sx={{ display: "flex", flexDirection: "column", gap: 0.5, fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
        {label}
        {children}
    </Box>
);

const capacityText = (item: TransferItem) =>
    item.targetCapacityPct == null ? (
        "—"
    ) : item.targetCapacityPct < 20 ? (
        <Box component="span" sx={{ color: "#b45309" }}>Tight · {item.targetCapacityPct}%</Box>
    ) : (
        <Box component="span" sx={{ color: "#2e7d32" }}>OK · {item.targetCapacityPct}%</Box>
    );

const TransferItemsTab: React.FC<{ canTransfer: boolean; ownFactoryNote: string | null }> = ({ canTransfer, ownFactoryNote }) => {
    const dispatch = useDispatch();
    const notifyError = useErrorNotification();
    const { factories } = useActiveFactories();
    const { openItems, isOpenItemsLoading } = useRootStateSelector(state => state.balancer);

    const [draft, setDraft] = useState<OpenItemsQuery>(emptyQuery);
    const [applied, setApplied] = useState<OpenItemsQuery>(emptyQuery);
    const [page, setPage] = useState(0);
    const [selected, setSelected] = useState<Record<string, OpenItem | true>>({});

    const [toFactoryId, setToFactoryId] = useState("");
    const [toPage, setToPage] = useState("");
    const [reason, setReason] = useState<TransferReason | "">("");
    const [note, setNote] = useState("");
    const [preview, setPreview] = useState<TransferItem[] | null>(null);
    const [isBusy, setIsBusy] = useState(false);

    const filterPages = usePages(draft.factoryId);
    const targetPages = usePages(toFactoryId);

    const request: ListRequest = useMemo(
        () => ({ page: page + 1, pageSize: PAGE_SIZE, search: "", sortBy: "dueDate", sortDirection: "asc", filters: toFilters(applied) }),
        [page, applied],
    );

    useEffect(() => {
        dispatch(loadOpenItems(request) as any);
    }, [dispatch, request]);

    const selectedIds = Object.keys(selected);
    const selectedCount = selectedIds.length;
    const pageRows = openItems.items.filter(item => canTransfer && item.canTransfer);
    const isPageSelected = pageRows.length > 0 && pageRows.every(item => selected[item.allocationId]);
    const fromFactories = Array.from(
        new Set(Object.values(selected).filter((value): value is OpenItem => value !== true).map(item => item.factoryId)),
    );
    const noteRequired = reason === TransferReasons.Other;
    const isFormValid = !!toFactoryId && !!toPage && !!reason && (!noteRequired || !!note.trim());

    const resetPreview = () => setPreview(null);

    const setDraftField = (field: keyof OpenItemsQuery, value: string) =>
        setDraft(current => ({ ...current, [field]: value, ...(field === "factoryId" ? { productionPage: "" } : {}) }));

    const search = () => {
        setApplied(draft);
        setPage(0);
        setSelected({});
        resetPreview();
    };

    const reset = () => {
        setDraft(emptyQuery);
        setApplied(emptyQuery);
        setPage(0);
        setSelected({});
        resetPreview();
    };

    const toggle = (item: OpenItem) => {
        resetPreview();
        setSelected(current => {
            const next = { ...current };
            if (next[item.allocationId]) delete next[item.allocationId];
            else next[item.allocationId] = item;
            return next;
        });
    };

    const togglePage = () => {
        resetPreview();
        setSelected(current => {
            const next = { ...current };
            pageRows.forEach(item => {
                if (isPageSelected) delete next[item.allocationId];
                else next[item.allocationId] = item;
            });
            return next;
        });
    };

    const selectAll = async () => {
        try {
            const ids = await balancerService.fetchOpenItemIds({ ...request, page: 1 });
            resetPreview();
            setSelected(current => {
                const next = { ...current };
                ids.forEach(id => {
                    next[id] = next[id] ?? openItems.items.find(item => item.allocationId === id) ?? true;
                });
                return next;
            });
        } catch (error) {
            notifyError(error);
        }
    };

    const transferRequest = (): TransferRequest => ({
        allocationIds: selectedIds,
        toFactoryId,
        toPage,
        reason: reason as TransferReason,
        note: note.trim() || null,
    });

    const runPreview = async () => {
        setIsBusy(true);
        try {
            setPreview(await balancerService.previewTransfer(transferRequest()));
        } catch (error) {
            notifyError(error);
        } finally {
            setIsBusy(false);
        }
    };

    const confirm = async () => {
        setIsBusy(true);
        try {
            const result = await balancerService.transfer(transferRequest());
            const problems = result.items.filter(item => item.problem && !item.skipped).length;
            dispatch(
                notificationSuccess(
                    `${result.transferred} item${result.transferred !== 1 ? "s" : ""} moved to ${toFactoryId} · page ${toPage} — re-quoted and logged` +
                        (problems ? ` (${problems} not moved)` : ""),
                ),
            );
            setSelected({});
            setPreview(null);
            setReason("");
            setNote("");
            dispatch(loadOpenItems(request) as any);
        } catch (error) {
            notifyError(error);
        } finally {
            setIsBusy(false);
        }
    };

    const movable = (preview ?? []).filter(item => !item.skipped && !item.problem);

    return (
        <>
            <Paper variant="outlined" sx={{ borderRadius: "8px", padding: 2 }}>
                <Box sx={{ display: "flex", gap: 1.5, alignItems: "flex-end", flexWrap: "wrap" }}>
                    <Field label="Order ID">
                        <TextField
                            size="small"
                            value={draft.orderId}
                            placeholder="e.g. ORD-884120"
                            onChange={event => setDraftField("orderId", event.target.value)}
                            onKeyDown={event => event.key === "Enter" && search()}
                            sx={{ width: 170 }}
                        />
                    </Field>
                    <Field label="Supplier">
                        <TextField select size="small" value={draft.factoryId} onChange={event => setDraftField("factoryId", event.target.value)} sx={{ minWidth: 150 }}>
                            <MenuItem value="">All suppliers</MenuItem>
                            {factories.map(factory => (
                                <MenuItem key={factory.id} value={String(factory.id)}>
                                    {factory.name}
                                </MenuItem>
                            ))}
                        </TextField>
                    </Field>
                    <Field label="Page">
                        <Tooltip title={draft.factoryId ? "" : "Pick a supplier first"}>
                            <TextField
                                select
                                size="small"
                                value={draft.productionPage}
                                disabled={!draft.factoryId}
                                onChange={event => setDraftField("productionPage", event.target.value)}
                                sx={{ minWidth: 130 }}>
                                <MenuItem value="">All pages</MenuItem>
                                {filterPages.map(value => (
                                    <MenuItem key={value} value={value}>
                                        {value}
                                    </MenuItem>
                                ))}
                            </TextField>
                        </Tooltip>
                    </Field>
                    <Field label="Event">
                        <TextField size="small" value={draft.eventName} onChange={event => setDraftField("eventName", event.target.value)} onKeyDown={event => event.key === "Enter" && search()} sx={{ width: 140 }} />
                    </Field>
                    <Field label="Site">
                        <TextField size="small" value={draft.site} onChange={event => setDraftField("site", event.target.value)} onKeyDown={event => event.key === "Enter" && search()} sx={{ width: 120 }} />
                    </Field>
                    <Field label="Due day">
                        <TextField type="date" size="small" value={draft.dueDate} onChange={event => setDraftField("dueDate", event.target.value)} sx={{ width: 160 }} />
                    </Field>
                    <Button variant="contained" startIcon={<SearchIcon />} onClick={search}>
                        Search
                    </Button>
                    <Button variant="text" onClick={reset}>
                        Reset
                    </Button>
                </Box>
            </Paper>

            <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
                <Box sx={{ padding: "12px 16px", display: "flex", gap: 1.25, alignItems: "center", borderBottom: "1px solid rgba(0, 0, 0, 0.08)", flexWrap: "wrap", fontSize: 13 }}>
                    <Box component="span" sx={{ color: "rgba(0, 0, 0, 0.6)" }}>
                        <Box component="b" sx={{ fontWeight: 500, color: "rgba(0, 0, 0, 0.87)" }}>
                            {openItems.totalCount.toLocaleString()}
                        </Box>{" "}
                        open items (allocated, not produced)
                    </Box>
                    <Box sx={{ flex: 1 }} />
                    {selectedCount > 0 && (
                        <span>
                            <b>{selectedCount}</b> selected across pages ·{" "}
                            <Link component="button" onClick={() => { setSelected({}); resetPreview(); }}>
                                Clear
                            </Link>
                        </span>
                    )}
                    {canTransfer && openItems.totalCount > 0 && (
                        <Button variant="text" size="small" onClick={selectAll}>
                            Select all that match
                        </Button>
                    )}
                </Box>
                <Box component="table" sx={{ width: "100%", borderCollapse: "collapse", tableLayout: "fixed" }}>
                    <colgroup>
                        <col style={{ width: 52 }} />
                        <col style={{ width: 140 }} />
                        <col style={{ width: 90 }} />
                        <col />
                        <col style={{ width: 100 }} />
                        <col style={{ width: 100 }} />
                        <col style={{ width: 130 }} />
                        <col style={{ width: 90 }} />
                        <col style={{ width: 110 }} />
                    </colgroup>
                    <thead>
                        <tr>
                            <Box component="th" sx={tableStyles.headCell}>
                                <Checkbox size="small" disabled={!pageRows.length} checked={isPageSelected} onChange={togglePage} title="Select this page" />
                            </Box>
                            {["Order ID", "Item", "SKU", "Supplier", "Page", "Event", "Site", "Due"].map(label => (
                                <Box component="th" key={label} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                                    {label}
                                </Box>
                            ))}
                        </tr>
                    </thead>
                    <tbody>
                        {openItems.items.map(item => {
                            const allowed = canTransfer && item.canTransfer;
                            return (
                                <Box
                                    component="tr"
                                    key={item.allocationId}
                                    sx={{ background: selected[item.allocationId] ? "rgba(25, 118, 210, 0.05)" : "transparent", opacity: allowed ? 1 : 0.6 }}>
                                    <Box component="td" sx={tableStyles.bodyCell}>
                                        <Tooltip title={allowed ? "" : `Only the ${item.factoryId} manager or operations can transfer this item`}>
                                            <span>
                                                <Checkbox size="small" disabled={!allowed} checked={!!selected[item.allocationId]} onChange={() => toggle(item)} />
                                            </span>
                                        </Tooltip>
                                    </Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, color: "#1976d2" }}>{item.orderId}</Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>{item.orderItemId}</Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 11.5, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                                        {item.skuUuid}
                                    </Box>
                                    <Box component="td" sx={tableStyles.bodyCell}><Factory id={item.factoryId} /></Box>
                                    <Box component="td" sx={tableStyles.bodyCell}><PageTag page={item.productionPage} /></Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 13, color: item.eventName ? "#b45309" : "rgba(0, 0, 0, 0.6)" }}>
                                        {item.eventName ?? "Regular"}
                                    </Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 13 }}>{item.site ?? "—"}</Box>
                                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 13 }}>{formatDate(item.dueDate)}</Box>
                                </Box>
                            );
                        })}
                        {!openItems.items.length && !isOpenItemsLoading && (
                            <tr>
                                <Box component="td" colSpan={9} sx={{ ...tableStyles.emptyCell, textAlign: "center" }}>
                                    No open items match this search.
                                </Box>
                            </tr>
                        )}
                    </tbody>
                </Box>
                <TablePagination
                    component="div"
                    count={openItems.totalCount}
                    page={page}
                    rowsPerPage={PAGE_SIZE}
                    rowsPerPageOptions={[PAGE_SIZE]}
                    onPageChange={(_event, next) => setPage(next)}
                />
            </Paper>

            {selectedCount > 0 && (
                <SectionCard
                    title={`Transfer ${selectedCount} item${selectedCount > 1 ? "s" : ""}${fromFactories.length === 1 ? ` from ${fromFactories[0]}` : ""}`}
                    subtitle={ownFactoryNote ?? "Pick the supplier and the production page to move them to. The balancer re-quotes each item before you confirm."}>
                    <Box sx={{ display: "flex", gap: 1.5, alignItems: "flex-end", flexWrap: "wrap" }}>
                        <Field label="To supplier">
                            <TextField
                                select
                                size="small"
                                value={toFactoryId}
                                onChange={event => { setToFactoryId(event.target.value); setToPage(""); resetPreview(); }}
                                sx={{ minWidth: 150 }}>
                                {factories.map(factory => (
                                    <MenuItem key={factory.id} value={String(factory.id)}>
                                        {factory.name}
                                    </MenuItem>
                                ))}
                            </TextField>
                        </Field>
                        <Field label="To page">
                            <TextField
                                select
                                size="small"
                                value={toPage}
                                disabled={!toFactoryId}
                                onChange={event => { setToPage(event.target.value); resetPreview(); }}
                                sx={{ minWidth: 130 }}>
                                {targetPages.map(value => (
                                    <MenuItem key={value} value={value}>
                                        {value}
                                    </MenuItem>
                                ))}
                            </TextField>
                        </Field>
                        <Field label="Reason *">
                            <TextField
                                select
                                size="small"
                                value={reason}
                                onChange={event => { setReason(event.target.value as TransferReason); resetPreview(); }}
                                sx={{ minWidth: 190 }}>
                                {Object.values(TransferReasons).map(value => (
                                    <MenuItem key={value} value={value}>
                                        {transferReasonLabels[value]}
                                    </MenuItem>
                                ))}
                            </TextField>
                        </Field>
                        <Field label={noteRequired ? "Note *" : "Note"}>
                            <TextField
                                size="small"
                                value={note}
                                placeholder="e.g. Laser down until Nov 28"
                                onChange={event => { setNote(event.target.value); resetPreview(); }}
                                sx={{ minWidth: 260 }}
                                inputProps={{ maxLength: 500 }}
                            />
                        </Field>
                        <Button variant="outlined" disabled={!isFormValid || isBusy} onClick={runPreview}>
                            Preview re-quote
                        </Button>
                    </Box>

                    {preview && (
                        <Box sx={{ marginTop: 2 }}>
                            <Box component="table" sx={{ width: "100%", borderCollapse: "collapse", border: "1px solid rgba(0, 0, 0, 0.08)" }}>
                                <thead>
                                    <tr>
                                        {["Order · item", "From", "To", "Quote (old → new)", "Cost change", "ETA", `Capacity at ${toFactoryId}`].map(label => (
                                            <Box component="th" key={label} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                                                {label}
                                            </Box>
                                        ))}
                                    </tr>
                                </thead>
                                <tbody>
                                    {preview.slice(0, PREVIEW_ROWS).map(item => (
                                        <Box component="tr" key={item.allocationId} sx={{ opacity: item.skipped || item.problem ? 0.55 : 1 }}>
                                            <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>
                                                {item.orderId} · {item.orderItemId}
                                            </Box>
                                            <Box component="td" sx={tableStyles.bodyCell}>
                                                <Box component="span" sx={{ display: "inline-flex", gap: 0.75, alignItems: "center" }}>
                                                    <Factory id={item.fromFactoryId} />
                                                    <PageTag page={item.fromPage} />
                                                </Box>
                                            </Box>
                                            <Box component="td" sx={tableStyles.bodyCell}>
                                                <Box component="span" sx={{ display: "inline-flex", gap: 0.75, alignItems: "center" }}>
                                                    <Factory id={item.toFactoryId} />
                                                    <PageTag page={item.toPage} />
                                                </Box>
                                            </Box>
                                            {item.skipped || item.problem ? (
                                                <Box component="td" colSpan={4} sx={{ ...tableStyles.bodyCell, fontSize: 12, color: item.problem ? "#c62828" : "rgba(0, 0, 0, 0.6)" }}>
                                                    {item.problem ?? "Already on this page — skipped"}
                                                </Box>
                                            ) : (
                                                <>
                                                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 12 }}>
                                                        <Box sx={{ color: "rgba(0, 0, 0, 0.5)" }}>{item.oldQuoteId ?? "—"}</Box>
                                                        <Box sx={{ color: "#1976d2" }}>{item.newQuoteId ?? "same quote"}</Box>
                                                    </Box>
                                                    <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, color: deltaColor(item.costDelta) }}>
                                                        {formatSignedMoney(item.costDelta, item.currency)}
                                                    </Box>
                                                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 12 }}>
                                                        {formatDate(item.oldEta)} → {formatDate(item.newEta)}
                                                    </Box>
                                                    <Box component="td" sx={{ ...tableStyles.bodyCell, fontSize: 12 }}>{capacityText(item)}</Box>
                                                </>
                                            )}
                                        </Box>
                                    ))}
                                    {preview.length > PREVIEW_ROWS && (
                                        <tr>
                                            <Box component="td" colSpan={7} sx={{ ...tableStyles.bodyCell, fontSize: 12, color: "rgba(0, 0, 0, 0.5)" }}>
                                                + {preview.length - PREVIEW_ROWS} more items
                                            </Box>
                                        </tr>
                                    )}
                                </tbody>
                            </Box>
                            <Box sx={{ display: "flex", gap: 1.25, marginTop: 1.5, alignItems: "center", flexWrap: "wrap" }}>
                                <Button variant="contained" startIcon={<SwapHorizIcon />} disabled={!movable.length || isBusy} onClick={confirm}>
                                    Confirm transfer
                                </Button>
                                <Button variant="text" onClick={resetPreview}>
                                    Cancel
                                </Button>
                                <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.55)" }}>
                                    Old quotes are marked transferred, new ones selected, and the items move to page {toPage}. Every move is logged.
                                </Box>
                            </Box>
                        </Box>
                    )}
                </SectionCard>
            )}
        </>
    );
};

export default TransferItemsTab;
