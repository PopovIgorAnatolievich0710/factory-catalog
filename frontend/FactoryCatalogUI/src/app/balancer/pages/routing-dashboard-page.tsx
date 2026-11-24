import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import dayjs from "dayjs";
import FileSaver from "file-saver";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import LinearProgress from "@mui/material/LinearProgress";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import AltRouteOutlinedIcon from "@mui/icons-material/AltRouteOutlined";
import SwapHorizOutlinedIcon from "@mui/icons-material/SwapHorizOutlined";
import ScheduleOutlinedIcon from "@mui/icons-material/ScheduleOutlined";
import SavingsOutlinedIcon from "@mui/icons-material/SavingsOutlined";
import UndoOutlinedIcon from "@mui/icons-material/UndoOutlined";
import FileDownloadOutlinedIcon from "@mui/icons-material/FileDownloadOutlined";
import SectionCard from "../../../components/section-card";
import SummaryTile from "../../../components/summary-tile";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { useErrorNotification } from "../../../helpers/response-helper";
import { DashboardFilter, RoutedPerDay } from "../entities";
import { loadRoutingDashboard } from "../store/actions";
import balancerService from "../services/balancer-service";
import { Factory, formatMoney, formatPercent, PageTitle, pageSx, useActiveFactories, useFactoryCodes } from "../components/balancer-ui";

const ALL = "all";
const RANGES = [7, 30, 90];
const DIM_COST_COLUMNS = [
    "orderId", "orderItemId", "skuUuid", "factoryId", "mode", "decidedAt", "producedAt",
    "quotedMaterial", "quotedLabor", "quotedPacking", "quotedTotal", "actualLabor", "laborVariance", "currency",
];
const FACTORY_COLORS = ["#1976d2", "#2e7d32", "#ed6c02", "#6a1b9a", "#00838f", "#ad1457"];

const sectionLabelSx = { fontSize: 11, textTransform: "uppercase", letterSpacing: "0.07em", color: "rgba(0, 0, 0, 0.55)" } as const;

const RoutedPerDayChart: React.FC<{ rows: RoutedPerDay[]; factoryIds: string[] }> = ({ rows, factoryIds }) => {
    const codeOf = useFactoryCodes();
    const days = useMemo(() => Array.from(new Set(rows.map(row => row.date))).sort(), [rows]);
    const totals = days.map(day => rows.filter(row => row.date === day).reduce((sum, row) => sum + row.items, 0));
    const max = Math.max(1, ...totals);

    if (!days.length) {
        return <Box sx={{ color: "rgba(0, 0, 0, 0.5)", fontSize: 13 }}>No items routed in this period.</Box>;
    }

    return (
        <Box>
            <Box sx={{ display: "flex", alignItems: "flex-end", gap: "3px", height: 140 }}>
                {days.map((day, index) => (
                    <Tooltip
                        key={day}
                        title={`${dayjs(day).format("DD MMM")}: ${totals[index]} items`}
                        placement="top">
                        <Box sx={{ flex: 1, minWidth: 4, height: `${(totals[index] / max) * 100}%`, display: "flex", flexDirection: "column-reverse" }}>
                            {factoryIds.map((factoryId, colorIndex) => {
                                const items = rows.find(row => row.date === day && row.factoryId === factoryId)?.items ?? 0;
                                return items ? (
                                    <Box
                                        key={factoryId}
                                        sx={{ height: `${(items / totals[index]) * 100}%`, background: FACTORY_COLORS[colorIndex % FACTORY_COLORS.length] }}
                                    />
                                ) : null;
                            })}
                        </Box>
                    </Tooltip>
                ))}
            </Box>
            <Box sx={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "rgba(0, 0, 0, 0.5)", marginTop: 0.5 }}>
                <span>{dayjs(days[0]).format("DD MMM")}</span>
                <span>{dayjs(days[days.length - 1]).format("DD MMM")}</span>
            </Box>
            <Box sx={{ display: "flex", gap: 2, flexWrap: "wrap", marginTop: 1, fontSize: 12 }}>
                {factoryIds.map((factoryId, colorIndex) => (
                    <Box key={factoryId} sx={{ display: "inline-flex", alignItems: "center", gap: 0.75 }}>
                        <Box sx={{ width: 10, height: 10, borderRadius: "2px", background: FACTORY_COLORS[colorIndex % FACTORY_COLORS.length] }} />
                        {codeOf(factoryId)}
                    </Box>
                ))}
            </Box>
        </Box>
    );
};

const RoutingDashboardPage: React.FC = () => {
    const dispatch = useDispatch();
    const notifyError = useErrorNotification();
    const { can } = usePermissions();
    const { factories } = useActiveFactories();
    const { dashboard, shadow, isDashboardLoading } = useRootStateSelector(state => state.balancer);

    const [factoryId, setFactoryId] = useState(ALL);
    const [country, setCountry] = useState(ALL);
    const [range, setRange] = useState(30);
    const [isExporting, setIsExporting] = useState(false);

    const filter: DashboardFilter = useMemo(
        () => ({
            factoryId: factoryId === ALL ? null : factoryId,
            country: country === ALL ? null : country,
            from: dayjs().subtract(range - 1, "day").format("YYYY-MM-DD"),
            to: dayjs().format("YYYY-MM-DD"),
        }),
        [factoryId, country, range],
    );

    const canViewDimCost = can(Permissions.Balancer.ViewDimCost);

    useEffect(() => {
        dispatch(loadRoutingDashboard(filter, true) as any);
    }, [dispatch, filter]);

    const shares = dashboard?.shares ?? [];
    const maxShare = Math.max(1, ...shares.flatMap(share => [share.targetPct, share.actualPct])) * 1.15;
    const chartFactoryIds = useMemo(
        () => Array.from(new Set((dashboard?.routedPerDay ?? []).map(row => row.factoryId))).sort(),
        [dashboard],
    );

    const exportDimCost = async () => {
        setIsExporting(true);
        try {
            const file = await balancerService.exportDimCost({
                page: 1,
                pageSize: 100,
                search: "",
                sortBy: "decidedAt",
                sortDirection: "desc",
                filters: {
                    decidedAt: { operator: "between", value: `${filter.from}T00:00:00Z`, valueTo: `${filter.to}T23:59:59Z` },
                    ...(filter.factoryId ? { factoryId: { values: [filter.factoryId] } } : {}),
                },
                columns: DIM_COST_COLUMNS,
            });
            FileSaver.saveAs(file.blob, file.fileName ?? `dim_cost_${dayjs().format("YYYYMMDD_HHmmss")}.xlsx`);
        } catch (error) {
            notifyError(error);
        } finally {
            setIsExporting(false);
        }
    };

    return (
        <Box sx={pageSx}>
            <PageTitle title="Routing dashboard">
                {canViewDimCost && (
                    <Button
                        variant="outlined"
                        size="small"
                        startIcon={<FileDownloadOutlinedIcon />}
                        disabled={isExporting}
                        onClick={exportDimCost}>
                        DIM cost (quoted vs actual)
                    </Button>
                )}
            </PageTitle>

            <Box sx={{ display: "flex", gap: 1.25, alignItems: "center", flexWrap: "wrap" }}>
                <TextField select size="small" value={factoryId} onChange={event => setFactoryId(event.target.value)} sx={{ minWidth: 170 }}>
                    <MenuItem value={ALL}>All factories</MenuItem>
                    {factories.map(factory => (
                        <MenuItem key={factory.id} value={String(factory.id)}>
                            {factory.name}
                        </MenuItem>
                    ))}
                </TextField>
                <TextField select size="small" value={country} onChange={event => setCountry(event.target.value)} sx={{ minWidth: 160 }}>
                    <MenuItem value={ALL}>All countries</MenuItem>
                    {(dashboard?.countries ?? []).map(code => (
                        <MenuItem key={code} value={code}>
                            {code}
                        </MenuItem>
                    ))}
                </TextField>
                <TextField select size="small" value={range} onChange={event => setRange(Number(event.target.value))} sx={{ minWidth: 150 }}>
                    {RANGES.map(days => (
                        <MenuItem key={days} value={days}>
                            Last {days} days
                        </MenuItem>
                    ))}
                </TextField>
            </Box>

            <Box sx={{ height: 4 }}>{isDashboardLoading && <LinearProgress />}</Box>

            <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(210px, 1fr))", gap: 1.5 }}>
                <SummaryTile
                    icon={<AltRouteOutlinedIcon />}
                    label="Orders routed"
                    value={(dashboard?.ordersRouted ?? 0).toLocaleString()}
                    hint={`${(dashboard?.itemsRouted ?? 0).toLocaleString()} items`}
                />
                <SummaryTile
                    icon={<SwapHorizOutlinedIcon />}
                    label="Transfer rate"
                    value={formatPercent(dashboard?.transferRatePct)}
                    hint={`${dashboard?.itemsTransferred ?? 0} items moved`}
                    color="#ed6c02"
                />
                <SummaryTile icon={<ScheduleOutlinedIcon />} label="On time vs ETA" value={formatPercent(dashboard?.onTimePct)} color="#2e7d32" />
                <SummaryTile
                    icon={<SavingsOutlinedIcon />}
                    label="Avg cost vs cheapest"
                    value={dashboard?.avgCostVsCheapestPct == null ? "—" : `+${formatPercent(dashboard.avgCostVsCheapestPct)}`}
                    hint="above cheapest feasible"
                    color="#6a1b9a"
                />
                <SummaryTile
                    icon={<UndoOutlinedIcon />}
                    label="Fallback routings"
                    value={(dashboard?.fallbackCount ?? 0).toLocaleString()}
                    hint="by default share"
                    color={dashboard?.fallbackCount ? "#d32f2f" : "#607d8b"}
                />
            </Box>

            <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "minmax(0, 1.4fr) minmax(0, 1fr)" }, gap: 2 }}>
                <SectionCard
                    title="Target vs actual share"
                    subtitle={`Target = share quoted by the catalog; actual = routed items. Gaps above ${dashboard?.shareGapThresholdPct ?? 0} pt are highlighted.`}>
                    <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
                        {shares.map(share => (
                            <Box key={share.factoryId} sx={{ display: "grid", gridTemplateColumns: "56px 1fr 70px", gap: 1.5, alignItems: "center" }}>
                                <Factory id={share.factoryId} />
                                <Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                                        <Box sx={{ height: 10, width: `${(share.targetPct / maxShare) * 100}%`, background: "rgba(0, 0, 0, 0.14)", borderRadius: "3px" }} />
                                        <Box component="span" sx={{ fontSize: 11, color: "rgba(0, 0, 0, 0.55)" }}>
                                            {formatPercent(share.targetPct)} target
                                        </Box>
                                    </Box>
                                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                                        <Box sx={{ height: 14, width: `${(share.actualPct / maxShare) * 100}%`, background: "#1976d2", borderRadius: "3px" }} />
                                        <Box component="span" sx={{ fontSize: 12, fontWeight: 500 }}>
                                            {formatPercent(share.actualPct)} actual
                                        </Box>
                                    </Box>
                                </Box>
                                <Box
                                    component="span"
                                    sx={{ fontSize: 12, fontWeight: 500, textAlign: "right", color: share.isGapHighlighted ? "#b45309" : "rgba(0, 0, 0, 0.55)" }}>
                                    {share.gapPct > 0 ? "+" : ""}
                                    {Number(share.gapPct.toFixed(1))} pt
                                </Box>
                            </Box>
                        ))}
                        {!shares.length && <Box sx={{ color: "rgba(0, 0, 0, 0.5)", fontSize: 13 }}>No routed items in this period.</Box>}
                    </Box>
                </SectionCard>

                <SectionCard title="Shadow mode" subtitle="New balancer decisions compared with the legacy balancer for the same order items.">
                    <Box sx={{ display: "flex", gap: 3, alignItems: "center", marginBottom: 2, flexWrap: "wrap" }}>
                        <Box>
                            <Box sx={sectionLabelSx}>Agreement rate</Box>
                            <Box sx={{ fontSize: 32, fontWeight: 300 }}>{formatPercent(shadow?.agreementRatePct)}</Box>
                            <Box sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.55)" }}>
                                of {(shadow?.compared ?? 0).toLocaleString()} compared items
                            </Box>
                        </Box>
                        <Box>
                            <Box sx={sectionLabelSx}>Estimated savings</Box>
                            <Box sx={{ fontSize: 32, fontWeight: 300, color: "#2e7d32" }}>
                                {formatMoney(shadow?.estimatedSavings ?? 0, shadow?.currency ?? "USD")}
                            </Box>
                            <Box sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.55)" }}>if the new decisions had been live</Box>
                        </Box>
                    </Box>
                    <Box sx={{ height: 8, borderRadius: "4px", background: "rgba(0, 0, 0, 0.08)", overflow: "hidden", marginBottom: 1.75 }}>
                        <Box sx={{ width: `${shadow?.agreementRatePct ?? 0}%`, height: "100%", background: "#2e7d32" }} />
                    </Box>
                    <Box sx={{ ...sectionLabelSx, marginBottom: 0.75 }}>Why they disagree</Box>
                    {(shadow?.disagreementReasons ?? []).map(reason => (
                        <Box key={reason.reason} sx={{ display: "flex", gap: 1.25, fontSize: 13, padding: "5px 0", borderBottom: "1px solid rgba(0, 0, 0, 0.05)" }}>
                            <Box component="span" sx={{ flex: 1 }}>
                                {reason.reason}
                            </Box>
                            <Box component="b" sx={{ fontWeight: 500 }}>
                                {formatPercent(reason.sharePct)}
                            </Box>
                        </Box>
                    ))}
                    {!shadow?.disagreementReasons?.length && <Box sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.5)" }}>No disagreements.</Box>}
                </SectionCard>
            </Box>

            <SectionCard title="Items routed per day" subtitle="Live and fallback allocations by decision day and factory.">
                <RoutedPerDayChart rows={dashboard?.routedPerDay ?? []} factoryIds={chartFactoryIds} />
            </SectionCard>
        </Box>
    );
};

export default RoutingDashboardPage;
