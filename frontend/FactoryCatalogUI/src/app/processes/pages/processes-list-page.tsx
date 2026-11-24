import React, { useMemo, useRef, useState } from "react";
import { useDispatch } from "react-redux";
import { useHistory } from "react-router-dom";
import Box from "@mui/material/Box";
import AddIcon from "@mui/icons-material/Add";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import ExcelImportDialog from "../../../components/excel-import-dialog";
import { CapacityChip, FactoryTag } from "../../../components/status-chips";
import { PrimaryBoButton } from "../../../../bo-frontend/src/components/Buttons/PrimaryBoButton";
import { SecondaryBoButton } from "../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { PROCESSES_URL } from "../../../constants/urls";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { notificationSuccess } from "../../notifications/actions";
import NewProcessDialog from "../components/new-process-dialog";
import { CapacityPercent, ProcessStatusChip, ProcessTypeChip } from "../components/process-chips";
import processService, { PROCESSES_RESOURCE } from "../services/process-service";
import { getUnitLabel, ProcessListItem, ProcessStatuses, ProcessTypes, processUnits } from "../entities";

const IMPORT_DESCRIPTION =
    "Upload the approved .xlsx file. Sheet \"Processes\": Process ID, Name, Type (Machine / Work), Measured in, " +
    "Status, Description. Optional sheet \"Factories\": Process ID, Factory ID, Runs here (Yes / No), Machine type, " +
    "Labor cost, Currency, Max per day. Existing processes are updated by Process ID.";

const mutedStyle = { color: "rgba(0, 0, 0, 0.38)" };

const getProcessUrl = (processId: string) => `${PROCESSES_URL}/${encodeURIComponent(processId)}`;

const ProcessesListPage: React.FC = () => {
    const dispatch = useDispatch();
    const history = useHistory();
    const { can } = usePermissions();
    const { factories } = useFactories();
    const reloadList = useRef<() => void>(() => undefined);

    const [isCreating, setIsCreating] = useState(false);
    const [isImporting, setIsImporting] = useState(false);

    const canEdit = can(Permissions.Processes.Edit);
    const activeFactories = useMemo(() => factories.filter(factory => factory.isActive), [factories]);

    const columns = useMemo<CatalogColumn<ProcessListItem>[]>(
        () => [
            { key: "processId", label: "Process ID", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
            { key: "name", label: "Process", alwaysVisible: true, sortable: true, filterType: "text" },
            {
                key: "type",
                label: "Type",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: [
                    { value: ProcessTypes.Machine, label: "Machine" },
                    { value: ProcessTypes.Work, label: "Work" },
                ],
                render: row => <ProcessTypeChip type={row.type} />,
            },
            {
                key: "machineType",
                label: "Machine type",
                alwaysVisible: true,
                sortable: true,
                filterType: "text",
                render: row =>
                    row.type === ProcessTypes.Machine ? row.machineType ?? "-" : <span style={mutedStyle}>n/a</span>,
            },
            {
                key: "runningFactoryIds",
                label: "Factories running it",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: activeFactories.map(factory => ({ value: factory.id, label: factory.name })),
                render: row => (
                    <Box sx={{ display: "flex", gap: "4px", flexWrap: "wrap" }}>
                        {activeFactories.map(factory => (
                            <FactoryTag
                                key={factory.id}
                                code={factory.countryCode ?? factory.id}
                                dimmed={!row.runningFactoryIds.includes(factory.id)}
                            />
                        ))}
                    </Box>
                ),
            },
            {
                key: "lowestCapacityPct",
                label: "Lowest capacity today",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                render: row =>
                    row.lowestCapacityPct === null ? (
                        <span style={mutedStyle}>-</span>
                    ) : (
                        <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
                            <CapacityPercent capacityPct={row.lowestCapacityPct} />
                            <CapacityChip percent={row.lowestCapacityPct} small />
                        </Box>
                    ),
            },
            {
                key: "avgLaborCost",
                label: "Avg labor cost",
                alwaysVisible: true,
                sortable: true,
                filterType: "number",
                monospace: true,
                render: row =>
                    row.avgLaborCost === null ? "-" : `${row.avgLaborCost.toFixed(2)} ${row.avgLaborCostCurrency ?? ""}`,
            },
            { key: "skusUsing", label: "SKUs using", alwaysVisible: true, sortable: true, filterType: "number", align: "right" },
            {
                key: "measuredIn",
                label: "Measured in",
                sortable: true,
                filterType: "values",
                filterValues: processUnits.map(unit => ({ value: unit.value, label: unit.label })),
                render: row => getUnitLabel(row.measuredIn),
            },
            {
                key: "status",
                label: "Status",
                sortable: true,
                filterType: "values",
                filterValues: [
                    { value: ProcessStatuses.Active, label: "Active" },
                    { value: ProcessStatuses.Inactive, label: "Inactive" },
                ],
                render: row => <ProcessStatusChip status={row.status} />,
            },
        ],
        [activeFactories],
    );

    const onImportConfirm = async (file: File) => {
        const result = await processService.import(file);
        dispatch(
            notificationSuccess(
                `Import finished: ${result.processesCreated} created, ${result.processesUpdated} updated, ` +
                    `${result.factoryRowsChanged} factory rows changed`,
            ),
        );
        reloadList.current();
    };

    return (
        <>
            <CatalogListPage<ProcessListItem>
                listKey="processes"
                title="Processes"
                resource={PROCESSES_RESOURCE}
                columns={columns}
                getRowId={row => row.processId}
                getRowUrl={row => getProcessUrl(row.processId)}
                defaultSort={{ sortBy: "processId", sortDirection: "asc" }}
                searchPlaceholder="Search process ID or name"
                headerActions={
                    canEdit && (
                        <PrimaryBoButton startIcon={<AddIcon />} onClick={() => setIsCreating(true)}>
                            New process
                        </PrimaryBoButton>
                    )
                }
                toolbarActions={context =>
                    canEdit && (
                        <SecondaryBoButton
                            startIcon={<UploadFileIcon />}
                            onClick={() => {
                                reloadList.current = context.reload;
                                setIsImporting(true);
                            }}>
                            Import
                        </SecondaryBoButton>
                    )
                }
            />
            <NewProcessDialog
                open={isCreating}
                onClose={() => setIsCreating(false)}
                onCreated={process => {
                    dispatch(notificationSuccess(`Process "${process.name}" created. Factories set where it runs.`));
                    history.push(`${getProcessUrl(process.processId)}?tab=factories`);
                }}
            />
            <ExcelImportDialog
                open={isImporting}
                title="Import processes"
                description={IMPORT_DESCRIPTION}
                onClose={() => setIsImporting(false)}
                onValidate={file => processService.validateImport(file)}
                onConfirm={onImportConfirm}
            />
        </>
    );
};

export default ProcessesListPage;
