import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import dayjs from "dayjs";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import MenuItem from "@mui/material/MenuItem";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import AssignmentOutlinedIcon from "@mui/icons-material/AssignmentOutlined";
import PrecisionManufacturingOutlinedIcon from "@mui/icons-material/PrecisionManufacturingOutlined";
import WarningAmberOutlinedIcon from "@mui/icons-material/WarningAmberOutlined";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import SectionCard from "../../../components/section-card";
import SummaryTile from "../../../components/summary-tile";
import { tableStyles } from "../../../components/data-table";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { useCurrentUser } from "../../../core/auth/current-user";
import { Roles } from "../../../core/auth/role";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { formatDate } from "../../../utils/format-date";
import { DailyPlanProcess } from "../entities";
import { loadDailyPlan } from "../store/actions";
import { Factory, monoSx, PageTitle, pageSx, useActiveFactories } from "../components/balancer-ui";

const PLAN_DAYS = 5;
const WARNING_LOAD_PCT = 85;
const allFactoriesRoles: string[] = [Roles.OperationsManager, Roles.BOAdministrator];

const loadColor = (process: DailyPlanProcess): string =>
    process.isOverCapacity ? "#d32f2f" : (process.loadPct ?? 0) >= WARNING_LOAD_PCT ? "#ed6c02" : "#2e7d32";

const dayLabel = (index: number, date: dayjs.Dayjs): string =>
    index === 0 ? "Today" : index === 1 ? "Tomorrow" : date.format("ddd DD MMM");

const headers = ["", "Process", "Type", "Items to do", "Quantity", "Done", "Available", "Load"];

const ProcessRow: React.FC<{ process: DailyPlanProcess; isOpen: boolean; onToggle: () => void }> = ({ process, isOpen, onToggle }) => {
    const color = loadColor(process);
    const over = process.available != null ? (process.done ?? 0) + process.items - process.available : 0;

    return (
        <>
            <Box
                component="tr"
                onClick={onToggle}
                sx={{ cursor: "pointer", background: process.isOverCapacity ? "rgba(211, 47, 47, 0.04)" : "transparent" }}>
                <Box component="td" sx={tableStyles.bodyCell}>
                    {isOpen ? <ExpandMoreIcon sx={{ fontSize: 18, color: "rgba(0, 0, 0, 0.5)" }} /> : <ChevronRightIcon sx={{ fontSize: 18, color: "rgba(0, 0, 0, 0.5)" }} />}
                </Box>
                <Box component="td" sx={{ ...tableStyles.bodyCell, fontWeight: 500 }}>
                    {process.name}
                    <Box sx={{ ...monoSx, fontSize: 11, color: "rgba(0, 0, 0, 0.5)" }}>{process.processId}</Box>
                </Box>
                <Box component="td" sx={tableStyles.bodyCell}>
                    {process.type ? <StatusChip label={process.type} colors={statusColors.neutral} small /> : "—"}
                </Box>
                <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontWeight: 500 }}>
                    {process.items.toLocaleString()}
                </Box>
                <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>
                    {process.quantity.toLocaleString()}
                </Box>
                <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, color: "rgba(0, 0, 0, 0.6)" }}>
                    {process.done?.toLocaleString() ?? "—"}
                </Box>
                <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>
                    {process.available?.toLocaleString() ?? "—"}
                </Box>
                <Box component="td" sx={tableStyles.bodyCell}>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1.25 }}>
                        <Box sx={{ flex: 1, height: 8, borderRadius: "4px", background: "rgba(0, 0, 0, 0.08)", overflow: "hidden" }}>
                            <Box sx={{ width: `${Math.min(100, process.loadPct ?? 0)}%`, height: "100%", background: color }} />
                        </Box>
                        <Box component="b" sx={{ width: 52, textAlign: "right", fontSize: 13, fontWeight: 600, color }}>
                            {process.loadPct != null ? `${Math.round(process.loadPct)}%` : "—"}
                        </Box>
                        {process.isOverCapacity && (
                            <Box component="span" sx={{ fontSize: 11, fontWeight: 500, color: "#c62828", whiteSpace: "nowrap" }}>
                                Over by {Math.ceil(over).toLocaleString()}
                            </Box>
                        )}
                    </Box>
                </Box>
            </Box>
            {isOpen && (
                <tr>
                    <td />
                    <Box component="td" colSpan={7} sx={{ padding: "4px 16px 14px" }}>
                        <Box component="table" sx={{ width: "100%", borderCollapse: "collapse", border: "1px solid rgba(0, 0, 0, 0.08)" }}>
                            <thead>
                                <tr>
                                    {["Order ID", "Item", "SKU", "Production quote", "Quantity", "Due"].map(label => (
                                        <Box component="th" key={label} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                                            {label}
                                        </Box>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {process.orderItems.map(item => (
                                    <tr key={item.allocationId}>
                                        <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, color: "#1976d2" }}>{item.orderId}</Box>
                                        <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>{item.orderItemId}</Box>
                                        <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 11.5 }}>{item.skuUuid}</Box>
                                        <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx, fontSize: 12, color: "#1976d2" }}>{item.quoteId ?? "—"}</Box>
                                        <Box component="td" sx={{ ...tableStyles.bodyCell, ...monoSx }}>{item.quantity.toLocaleString()}</Box>
                                        <Box component="td" sx={tableStyles.bodyCell}>{formatDate(item.dueDate)}</Box>
                                    </tr>
                                ))}
                            </tbody>
                        </Box>
                    </Box>
                </tr>
            )}
        </>
    );
};

const DailyPlanPage: React.FC = () => {
    const dispatch = useDispatch();
    const { user, permissionsEnabled } = useCurrentUser();
    const { factories, isLoading: isFactoriesLoading } = useActiveFactories();
    const { dailyPlan, isDailyPlanLoading } = useRootStateSelector(state => state.balancer);

    const canSwitchFactories = !permissionsEnabled || (user?.roles ?? []).some(role => allFactoriesRoles.includes(role));
    const selectableFactories = useMemo(
        () => (canSwitchFactories ? factories : factories.filter(factory => (user?.factoryIds ?? []).map(String).includes(String(factory.id)))),
        [canSwitchFactories, factories, user],
    );

    const [selectedFactoryId, setSelectedFactoryId] = useState<string | null>(null);
    const [dayIndex, setDayIndex] = useState(0);
    const [openProcessId, setOpenProcessId] = useState<string | null>(null);

    const factoryId = selectedFactoryId ?? (selectableFactories[0] ? String(selectableFactories[0].id) : null);
    const days = useMemo(() => Array.from({ length: PLAN_DAYS }, (_value, index) => dayjs().add(index, "day")), []);
    const date = days[dayIndex].format("YYYY-MM-DD");

    useEffect(() => {
        if (factoryId) {
            dispatch(loadDailyPlan(factoryId, date) as any);
        }
    }, [dispatch, factoryId, date]);

    const plan = dailyPlan?.factoryId === factoryId && dailyPlan?.date === date ? dailyPlan : null;
    const processes = plan?.processes ?? [];
    const overCapacity = processes.filter(process => process.isOverCapacity).length;

    return (
        <Box sx={pageSx}>
            <PageTitle title="Daily plan" />

            <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flexWrap: "wrap" }}>
                {canSwitchFactories || selectableFactories.length > 1 ? (
                    <TextField
                        select
                        size="small"
                        value={factoryId ?? ""}
                        onChange={event => { setSelectedFactoryId(event.target.value); setOpenProcessId(null); }}
                        sx={{ minWidth: 200 }}>
                        {selectableFactories.map(factory => (
                            <MenuItem key={factory.id} value={String(factory.id)}>
                                {factory.name}
                            </MenuItem>
                        ))}
                    </TextField>
                ) : (
                    factoryId && (
                        <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 1, fontSize: 14 }}>
                            Your factory <Factory id={factoryId} />
                        </Box>
                    )
                )}
                <ToggleButtonGroup
                    exclusive
                    size="small"
                    color="primary"
                    value={dayIndex}
                    onChange={(_event, next: number | null) => { if (next !== null) { setDayIndex(next); setOpenProcessId(null); } }}>
                    {days.map((day, index) => (
                        <ToggleButton key={index} value={index} sx={{ textTransform: "none", padding: "4px 14px" }}>
                            {dayLabel(index, day)}
                        </ToggleButton>
                    ))}
                </ToggleButtonGroup>
                <Box sx={{ flex: 1 }} />
                <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "6px", fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                    <InfoOutlinedIcon sx={{ fontSize: 15 }} />
                    Allocated, not produced yet · from the selected production quotes
                </Box>
            </Box>

            <Box sx={{ height: 4 }}>{(isDailyPlanLoading || isFactoriesLoading) && <LinearProgress />}</Box>

            {!factoryId && !isFactoriesLoading && (
                <SectionCard title="No factory assigned">
                    Your user is not bound to a factory yet. Ask the factory catalog admin to assign one.
                </SectionCard>
            )}

            {factoryId && (
                <>
                    <Box sx={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(200px, 1fr))", gap: 1.5 }}>
                        <SummaryTile
                            icon={<AssignmentOutlinedIcon />}
                            label="Items to produce"
                            value={(plan?.itemsToProduce ?? 0).toLocaleString()}
                            hint={days[dayIndex].format("DD MMM YYYY")}
                        />
                        <SummaryTile icon={<PrecisionManufacturingOutlinedIcon />} label="Processes in use" value={processes.length} />
                        <SummaryTile
                            icon={<WarningAmberOutlinedIcon />}
                            label="Over capacity"
                            value={overCapacity}
                            hint={overCapacity ? "processes need attention" : "all within capacity"}
                            color={overCapacity ? "#d32f2f" : "#2e7d32"}
                        />
                    </Box>

                    {plan && !plan.capacityKnown && (
                        <Box sx={{ fontSize: 13, color: "#b45309" }}>
                            Process capacity is unavailable right now; the plan is shown without the available capacity.
                        </Box>
                    )}

                    <Paper variant="outlined" sx={{ borderRadius: "8px", overflow: "hidden" }}>
                        <Box component="table" sx={{ width: "100%", borderCollapse: "collapse", tableLayout: "fixed" }}>
                            <colgroup>
                                <col style={{ width: 36 }} />
                                <col style={{ width: 220 }} />
                                <col style={{ width: 110 }} />
                                <col style={{ width: 110 }} />
                                <col style={{ width: 110 }} />
                                <col style={{ width: 100 }} />
                                <col style={{ width: 100 }} />
                                <col />
                            </colgroup>
                            <thead>
                                <tr>
                                    {headers.map((label, index) => (
                                        <Box component="th" key={index} sx={{ ...tableStyles.headCell, textAlign: "left" }}>
                                            {label}
                                        </Box>
                                    ))}
                                </tr>
                            </thead>
                            <tbody>
                                {processes.map(process => (
                                    <ProcessRow
                                        key={process.processId}
                                        process={process}
                                        isOpen={openProcessId === process.processId}
                                        onToggle={() => setOpenProcessId(openProcessId === process.processId ? null : process.processId)}
                                    />
                                ))}
                                {!processes.length && (
                                    <tr>
                                        <Box component="td" colSpan={8} sx={{ ...tableStyles.emptyCell, textAlign: "center" }}>
                                            Nothing planned for this day.
                                        </Box>
                                    </tr>
                                )}
                            </tbody>
                        </Box>
                    </Paper>
                </>
            )}
        </Box>
    );
};

export default DailyPlanPage;
