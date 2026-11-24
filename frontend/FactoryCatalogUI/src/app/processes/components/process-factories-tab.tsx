import React from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import FactoryGrid, { FactoryGridColumn } from "../../../components/factory-grid";
import GatedCheckbox from "../../../components/gated-checkbox";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { CapacityChip } from "../../../components/status-chips";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { Factory } from "../../../entities/factory";
import CommitTextField from "./commit-text-field";
import { CapacityPercent, formatQuantity, TodayLine } from "./process-chips";
import { saveProcessFactorySetup } from "../store/actions";
import {
    FULL_CAPACITY,
    getUnitShortLabel,
    ProcessDetails,
    ProcessFactory,
    ProcessFactorySetup,
    ProcessTypes,
} from "../entities";

const DEFAULT_CURRENCY = "USD";
const CURRENCY_LENGTH = 3;
const DENIED_MESSAGE = "Only the factory manager of this factory edits";

const mutedStyle = { color: "rgba(0, 0, 0, 0.38)" };
const unitHintStyle = { fontSize: 12, color: "rgba(0, 0, 0, 0.6)" };

const toNumber = (text: string): number | null => {
    const value = Number.parseFloat(text);
    return text === "" || Number.isNaN(value) || value < 0 ? null : value;
};

const ProcessFactoriesTab: React.FC<{ process: ProcessDetails }> = ({ process }) => {
    const dispatch = useDispatch();
    const { can, canInFactory } = usePermissions();

    const isMachine = process.type === ProcessTypes.Machine;
    const unit = getUnitShortLabel(process.measuredIn);
    const canViewCost = can(Permissions.Processes.ViewCost);

    const canEdit = (factory: Factory) => canInFactory(Permissions.Processes.EditFactorySetup, factory.id);

    const getRow = (factory: Factory): ProcessFactory =>
        process.factories.find(row => row.factoryId === String(factory.id)) ?? {
            factoryId: String(factory.id),
            factoryName: factory.name,
            runsHere: false,
            machineType: null,
            capacityPct: FULL_CAPACITY,
            capacityStatus: "Full",
            capacityNote: null,
            capacityUpdatedBy: null,
            capacityUpdatedAt: null,
            laborCost: null,
            maxPerDay: null,
            today: null,
        };

    const save = (row: ProcessFactory, patch: Partial<ProcessFactorySetup>) => {
        dispatch(
            saveProcessFactorySetup(process.processId, row.factoryId, {
                runsHere: row.runsHere,
                machineType: row.machineType,
                laborCostAmount: row.laborCost?.amountPerUnit ?? null,
                laborCostCurrency: row.laborCost?.currency ?? null,
                maxPerDay: row.maxPerDay,
                ...patch,
            }) as any,
        );
    };

    const columns: FactoryGridColumn[] = [
        {
            key: "runsHere",
            label: "Runs here",
            width: 120,
            render: factory => {
                const row = getRow(factory);
                return (
                    <GatedCheckbox
                        label={row.runsHere ? "Yes" : "No"}
                        allowed={canEdit(factory)}
                        deniedMessage={DENIED_MESSAGE}
                        checked={row.runsHere}
                        onChange={runsHere => save(row, { runsHere })}
                    />
                );
            },
        },
        ...(isMachine
            ? [
                  {
                      key: "machineType",
                      label: "Machine type",
                      width: 220,
                      render: (factory: Factory) => {
                          const row = getRow(factory);
                          if (!row.runsHere) return <span style={mutedStyle}>-</span>;
                          return canEdit(factory) ? (
                              <CommitTextField
                                  value={row.machineType ?? ""}
                                  onCommit={machineType => save(row, { machineType: machineType || null })}
                                  inputProps={{ maxLength: 100 }}
                                  fullWidth
                              />
                          ) : (
                              row.machineType ?? "-"
                          );
                      },
                  },
              ]
            : []),
        {
            key: "laborCost",
            label: `Labor cost per ${unit}`,
            width: 240,
            render: factory => {
                const row = getRow(factory);
                if (!row.runsHere) return <span style={mutedStyle}>-</span>;
                if (!canViewCost) return <span style={mutedStyle}>Hidden</span>;

                const currency = row.laborCost?.currency ?? DEFAULT_CURRENCY;
                if (!canEdit(factory)) {
                    return row.laborCost ? `${formatQuantity(row.laborCost.amountPerUnit)} ${row.laborCost.currency ?? ""}` : "-";
                }
                return (
                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                        <CommitTextField
                            type="number"
                            value={row.laborCost ? String(row.laborCost.amountPerUnit) : ""}
                            onCommit={text =>
                                save(row, { laborCostAmount: toNumber(text), laborCostCurrency: currency })
                            }
                            inputProps={{ min: 0, step: "any" }}
                            sx={{ width: 110 }}
                        />
                        <CommitTextField
                            value={currency}
                            onCommit={text =>
                                text.length === CURRENCY_LENGTH &&
                                row.laborCost &&
                                save(row, { laborCostCurrency: text.toUpperCase() })
                            }
                            inputProps={{ maxLength: CURRENCY_LENGTH, style: { textTransform: "uppercase" } }}
                            sx={{ width: 72 }}
                        />
                    </Box>
                );
            },
        },
        {
            key: "maxPerDay",
            label: "Max per day (100%)",
            width: 190,
            render: factory => {
                const row = getRow(factory);
                if (!row.runsHere) return <span style={mutedStyle}>-</span>;
                if (!canEdit(factory)) {
                    return row.maxPerDay === null ? "-" : `${formatQuantity(row.maxPerDay)} items/day`;
                }
                return (
                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                        <CommitTextField
                            type="number"
                            value={row.maxPerDay === null ? "" : String(row.maxPerDay)}
                            onCommit={text => save(row, { maxPerDay: toNumber(text) })}
                            inputProps={{ min: 0 }}
                            sx={{ width: 100 }}
                        />
                        <span style={unitHintStyle}>items/day</span>
                    </Box>
                );
            },
        },
        {
            key: "capacity",
            label: "Capacity · today",
            render: factory => {
                const row = getRow(factory);
                if (!row.runsHere) return <span style={mutedStyle}>Doesn&apos;t run here</span>;
                return (
                    <div>
                        <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
                            <CapacityPercent capacityPct={row.capacityPct} />
                            <CapacityChip percent={row.capacityPct} small />
                        </Box>
                        <TodayLine today={row.today} maxPerDay={row.maxPerDay} />
                    </div>
                );
            },
        },
    ];

    return (
        <SectionCard title="Factories" headerRight={<LockTag text="Factory manager edits" />} noPadding>
            <FactoryGrid
                columns={columns}
                isRowHighlighted={canEdit}
                isRowDisabled={factory => !canEdit(factory)}
            />
        </SectionCard>
    );
};

export default ProcessFactoriesTab;
