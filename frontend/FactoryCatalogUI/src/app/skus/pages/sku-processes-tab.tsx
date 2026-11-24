import React, { useMemo } from "react";
import { useHistory } from "react-router-dom";
import Box from "@mui/material/Box";
import Tooltip from "@mui/material/Tooltip";
import { ClientDataTable, DataTableColumn } from "../../../components/data-table";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { FactoryTag, StatusChip, statusColors } from "../../../components/status-chips";
import { PROCESSES_URL } from "../../../constants/urls";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { formatDate } from "../../../utils/format-date";
import { MutedText, ProductionConflictChip } from "../components/sku-chips";
import { ProcessType, ProductionConflictKind, SkuDetails, SkuProcess, SkuProductionConflictIssue } from "../entities";

const NOT_FOUND_TEXT = "Not found in the catalog";
const FULL_CAPACITY = 100;

const unitLabels: Record<string, string> = {
    Minutes: "min",
    Seconds: "sec",
    Hours: "h",
    Units: "units",
    Pieces: "pcs",
    Cm: "cm",
    Mm: "mm",
    Cm2: "cm²",
    Mm2: "mm²",
};

const conflictLabels: Record<ProductionConflictKind, string> = {
    ProcessNotListed: "Used but not listed",
    ProcessNotUsed: "Listed but not used",
    MachineMismatch: "Different machine",
};

const capacityColor = (percent: number): string => {
    if (percent <= 0) return "#c62828";
    return percent < FULL_CAPACITY ? "#b45309" : "#1b5e20";
};

const ProcessTypeChip: React.FC<{ type: ProcessType | null }> = ({ type }) =>
    type ? <StatusChip label={type} colors={type === "Machine" ? statusColors.info : statusColors.neutral} small /> : null;

const SkuProcessesTab: React.FC<{ sku: SkuDetails }> = ({ sku }) => {
    const history = useHistory();
    const { factories } = useFactories();
    const factoryIds = useMemo(
        () => factories.filter(factory => factory.isActive).map(factory => factory.id),
        [factories],
    );

    const columns = useMemo<DataTableColumn<SkuProcess>[]>(
        () => [
            { key: "processId", label: "Process ID", sortable: true, filterType: "text", monospace: true, width: 180 },
            {
                key: "name",
                label: "Process",
                sortable: true,
                filterType: "text",
                getValue: row => row.name ?? NOT_FOUND_TEXT,
                render: row => row.name ?? <MutedText danger>{NOT_FOUND_TEXT}</MutedText>,
            },
            {
                key: "type",
                label: "Type",
                sortable: true,
                filterType: "values",
                width: 120,
                render: row => <ProcessTypeChip type={row.type} />,
            },
            {
                key: "quantity",
                label: "Quantity required",
                sortable: true,
                filterType: "number",
                align: "right",
                width: 190,
                render: row => `${row.quantity} ${row.measuredIn ? unitLabels[row.measuredIn] ?? row.measuredIn : ""}`,
            },
            ...factoryIds.map<DataTableColumn<SkuProcess>>(factoryId => {
                const find = (row: SkuProcess) => row.capacity.find(capacity => capacity.factoryId === factoryId);
                return {
                    key: `capacity.${factoryId}`,
                    label: `${factoryId} %`,
                    sortable: true,
                    filterType: "number",
                    align: "center",
                    width: 120,
                    getValue: row => find(row)?.capacityPct ?? null,
                    render: row => {
                        const capacity = find(row);
                        if (!capacity?.runsHere || capacity.capacityPct === null) {
                            return (
                                <Tooltip title="Doesn't run here">
                                    <span>
                                        <MutedText>-</MutedText>
                                    </span>
                                </Tooltip>
                            );
                        }
                        return (
                            <Box
                                component="span"
                                sx={{
                                    fontWeight: capacity.capacityPct < FULL_CAPACITY ? 600 : 400,
                                    color: capacityColor(capacity.capacityPct),
                                }}>
                                {capacity.capacityPct}%
                            </Box>
                        );
                    },
                };
            }),
        ],
        [factoryIds],
    );

    const conflictColumns: DataTableColumn<SkuProductionConflictIssue>[] = [
        {
            key: "kind",
            label: "Difference",
            sortable: true,
            filterType: "values",
            getValue: row => conflictLabels[row.kind],
            render: row => conflictLabels[row.kind],
        },
        {
            key: "processName",
            label: "Process",
            sortable: true,
            filterType: "text",
            getValue: row => row.processName ?? row.processId,
            render: row => row.processName ?? row.processId,
        },
        {
            key: "processType",
            label: "Type",
            sortable: true,
            filterType: "values",
            render: row => <ProcessTypeChip type={row.processType} />,
        },
        { key: "machineType", label: "Machine", sortable: true, filterType: "text", render: row => row.machineType ?? "-" },
        { key: "expected", label: "Expected", sortable: true, filterType: "text" },
        { key: "actual", label: "Used", sortable: true, filterType: "text" },
        { key: "orderId", label: "Order ID", sortable: true, filterType: "text", monospace: true },
        {
            key: "factoryId",
            label: "Factory",
            sortable: true,
            filterType: "values",
            render: row => <FactoryTag code={row.factoryId} />,
        },
        { key: "seenAt", label: "Day", sortable: true, render: row => formatDate(row.seenAt) },
    ];

    return (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <SectionCard
                title="Mapped processes"
                headerRight={<LockTag text="Read only · Source: Info file / Supplier" />}
                noPadding>
                <ClientDataTable
                    columns={columns}
                    rows={sku.processes}
                    getRowId={row => row.processId}
                    noun="processes"
                    isActionRequired={row => row.name === null}
                    onRowClick={row => {
                        if (row.name !== null) history.push(`${PROCESSES_URL}/${encodeURIComponent(row.processId)}`);
                    }}
                    fixedLayout
                />
            </SectionCard>
            {sku.productionConflict.issues.length > 0 && (
                <SectionCard
                    title="Production conflicts"
                    subtitle="From the TG Supplier production log of the latest produced item per factory. Clears once the mapping is fixed or a later log matches."
                    headerRight={
                        <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
                            <ProductionConflictChip />
                            <LockTag text="Read only · Source: TG Supplier production log" />
                        </Box>
                    }
                    noPadding>
                    <ClientDataTable
                        columns={conflictColumns}
                        rows={sku.productionConflict.issues}
                        getRowId={row => `${row.factoryId}:${row.kind}:${row.processId}`}
                        noun="differences"
                    />
                </SectionCard>
            )}
        </Box>
    );
};

export default SkuProcessesTab;
