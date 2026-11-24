import React from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import Tooltip from "@mui/material/Tooltip";
import { SecondaryBoButton } from "../../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import SectionCard from "../../../../components/section-card";
import LockTag from "../../../../components/lock-tag";
import FactoryGrid from "../../../../components/factory-grid";
import { tableStyles } from "../../../../components/data-table";
import PermissionGate from "../../../../core/auth/permission-gate";
import { Permissions, usePermissions } from "../../../../core/auth/permissions";
import { setCriticalLevel, setCriticalLevelOverride } from "../../store/actions";
import LevelSourceTag from "../level-source-tag";
import LevelInput from "./level-input";
import { ComponentDetails } from "../../entities/component";
import { formatQuantity } from "../../utils/format";

interface SettingsTabProps {
    component: ComponentDetails;
}

const DENIED_MESSAGE = "Supply chain manager edits";

const SettingsTab: React.FC<SettingsTabProps> = ({ component }) => {
    const dispatch = useDispatch();
    const { can } = usePermissions();
    const canEdit = can(Permissions.Components.EditCriticalLevels);

    const overrideOf = (factoryId: string): number | null =>
        component.factories.find(stock => stock.factoryId === factoryId)?.criticalLevelOverride ?? null;

    const saveOverride = (factoryId: string, level: number | null) =>
        dispatch(setCriticalLevelOverride(component.erpId, factoryId, level) as any);

    return (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <SectionCard
                title="Global critical stock level"
                subtitle="Applies to every factory without an override."
                headerRight={<LockTag text={DENIED_MESSAGE} />}>
                <Box sx={{ display: "flex", alignItems: "center", gap: 1.25 }}>
                    <Tooltip title={canEdit ? "" : DENIED_MESSAGE}>
                        <span>
                            <LevelInput
                                value={component.criticalStockLevel}
                                disabled={!canEdit}
                                placeholder="not set"
                                onCommit={level => dispatch(setCriticalLevel(component.erpId, level) as any)}
                            />
                        </span>
                    </Tooltip>
                    <Box component="span" sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)" }}>
                        {component.unitOfMeasure.unit}
                    </Box>
                </Box>
            </SectionCard>

            <SectionCard
                title="Per-factory overrides"
                subtitle="Optional. When set, the override wins over the global level."
                headerRight={<LockTag text={DENIED_MESSAGE} />}
                noPadding>
                <FactoryGrid
                    columns={[
                        {
                            key: "override",
                            label: "Override",
                            width: 200,
                            render: factory => (
                                <Tooltip title={canEdit ? "" : DENIED_MESSAGE}>
                                    <span>
                                        <LevelInput
                                            value={overrideOf(factory.id)}
                                            disabled={!canEdit}
                                            placeholder="none"
                                            onCommit={level => saveOverride(factory.id, level)}
                                        />
                                    </span>
                                </Tooltip>
                            ),
                        },
                        {
                            key: "applies",
                            label: "Level that applies",
                            render: factory => {
                                const override = overrideOf(factory.id);
                                const level = override ?? component.criticalStockLevel;
                                return (
                                    <>
                                        <Box component="span" sx={tableStyles.monospace}>
                                            {formatQuantity(level)}
                                        </Box>
                                        {level !== null && <LevelSourceTag isOverride={override !== null} />}
                                    </>
                                );
                            },
                        },
                        {
                            key: "clear",
                            label: "",
                            width: 120,
                            align: "right",
                            render: factory =>
                                overrideOf(factory.id) !== null && (
                                    <PermissionGate allowed={canEdit} message={DENIED_MESSAGE}>
                                        <SecondaryBoButton size="small" onClick={() => saveOverride(factory.id, null)}>
                                            Clear
                                        </SecondaryBoButton>
                                    </PermissionGate>
                                ),
                        },
                    ]}
                />
            </SectionCard>
        </Box>
    );
};

export default SettingsTab;
