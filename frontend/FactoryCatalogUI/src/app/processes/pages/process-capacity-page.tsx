import React, { useEffect, useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import Typography from "@mui/material/Typography";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import SectionCard from "../../../components/section-card";
import { FactoryTag } from "../../../components/status-chips";
import { useCurrentUser } from "../../../core/auth/current-user";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { Roles } from "../../../core/auth/role";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import CapacityFactoryView from "../components/capacity-factory-view";
import CapacityGrid from "../components/capacity-grid";
import { loadCapacityOverview, loadFactoryCapacity } from "../store/actions";

const Views = {
    AllFactories: "all",
    ByFactory: "factory",
} as const;

type View = (typeof Views)[keyof typeof Views];

const allFactoriesRoles: string[] = [Roles.OperationsManager, Roles.BOAdministrator];

const ProcessCapacityPage: React.FC = () => {
    const dispatch = useDispatch();
    const { can, canInFactory } = usePermissions();
    const { user, permissionsEnabled } = useCurrentUser();
    const { factories, isLoading: isFactoriesLoading } = useFactories();
    const { capacityOverview, factoryCapacity, isCapacityLoading } = useRootStateSelector(state => state.processes);

    const activeFactories = useMemo(() => factories.filter(factory => factory.isActive), [factories]);
    const canSwitchFactories = !permissionsEnabled || (user?.roles ?? []).some(role => allFactoriesRoles.includes(role));
    const isFactoryManager = !canSwitchFactories && can(Permissions.Processes.EditCapacity);
    const selectableFactories = isFactoryManager
        ? activeFactories.filter(factory => canInFactory(Permissions.Processes.EditCapacity, factory.id))
        : activeFactories;

    const [view, setView] = useState<View>(Views.AllFactories);
    const [selectedFactoryId, setSelectedFactoryId] = useState<string | null>(null);

    const factory = selectableFactories.find(x => String(x.id) === selectedFactoryId) ?? selectableFactories[0] ?? null;
    const factoryId = factory ? String(factory.id) : null;
    const showGrid = !isFactoryManager && (!canSwitchFactories || view === Views.AllFactories);

    useEffect(() => {
        if (showGrid) {
            dispatch(loadCapacityOverview() as any);
        } else if (factoryId !== null) {
            dispatch(loadFactoryCapacity(factoryId) as any);
        }
    }, [dispatch, showGrid, factoryId]);

    return (
        <Box sx={{ padding: "24px", display: "flex", flexDirection: "column", gap: 2 }}>
            <Typography component="h1" sx={{ fontSize: 22, fontWeight: 500, color: "rgba(0, 0, 0, 0.87)" }}>
                Process capacity
            </Typography>

            <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flexWrap: "wrap" }}>
                {canSwitchFactories && (
                    <ToggleButtonGroup
                        exclusive
                        size="small"
                        color="primary"
                        value={view}
                        onChange={(_event, next: View | null) => next && setView(next)}>
                        <ToggleButton value={Views.AllFactories} sx={{ textTransform: "none", padding: "4px 14px" }}>
                            All factories
                        </ToggleButton>
                        <ToggleButton value={Views.ByFactory} sx={{ textTransform: "none", padding: "4px 14px" }}>
                            By factory
                        </ToggleButton>
                    </ToggleButtonGroup>
                )}
                {!showGrid && factory && selectableFactories.length > 1 && (
                    <TextField
                        select
                        size="small"
                        value={factoryId}
                        onChange={event => setSelectedFactoryId(event.target.value)}
                        sx={{ minWidth: 200 }}>
                        {selectableFactories.map(option => (
                            <MenuItem key={option.id} value={String(option.id)}>
                                {option.name}
                            </MenuItem>
                        ))}
                    </TextField>
                )}
                {isFactoryManager && factory && (
                    <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: 1, fontSize: 14 }}>
                        Your factory <FactoryTag code={factory.countryCode ?? factory.id} />
                    </Box>
                )}
                <Box sx={{ flex: 1 }} />
                <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "6px", fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                    <InfoOutlinedIcon sx={{ fontSize: 15 }} />
                    Capacity changes only when you change it.
                </Box>
            </Box>

            <Box sx={{ height: 4 }}>{(isCapacityLoading || isFactoriesLoading) && <LinearProgress />}</Box>

            {showGrid && <CapacityGrid rows={capacityOverview} factories={activeFactories} />}
            {!showGrid && factory && (
                <CapacityFactoryView
                    factory={factory}
                    rows={factoryCapacity.factoryId === factoryId ? factoryCapacity.rows : []}
                    editable={canInFactory(Permissions.Processes.EditCapacity, factory.id)}
                />
            )}
            {!showGrid && !factory && !isFactoriesLoading && (
                <SectionCard title="No factory assigned">
                    Your user is not bound to a factory yet. Ask the factory catalog admin to assign one.
                </SectionCard>
            )}
        </Box>
    );
};

export default ProcessCapacityPage;
