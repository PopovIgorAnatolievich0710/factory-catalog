import React, { useEffect, useMemo } from "react";
import { useDispatch } from "react-redux";
import { useHistory, useParams } from "react-router-dom";
import Box from "@mui/material/Box";
import { ClientDataTable, DataTableColumn } from "../../../components/data-table";
import EntityViewPage from "../../../components/entity-view-page";
import HistoryTab from "../../../components/history-tab";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { PROCESSES_URL, SKUS_URL } from "../../../constants/urls";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import ProcessFactoriesTab from "../components/process-factories-tab";
import ProcessSpecificationsTab from "../components/process-specifications-tab";
import { formatQuantity, ProcessStatusChip, ProcessTypeChip } from "../components/process-chips";
import { loadProcess } from "../store/actions";
import { getUnitLabel, getUnitShortLabel, ProcessSku } from "../entities";

const PROCESS_HISTORY_ENTITY_TYPE = "process";

const ProcessViewPage: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const dispatch = useDispatch();
    const history = useHistory();
    const { process: loadedProcess, isProcessLoading } = useRootStateSelector(state => state.processes);
    const process = loadedProcess?.processId === id ? loadedProcess : null;

    useEffect(() => {
        dispatch(loadProcess(id) as any);
    }, [dispatch, id]);

    const skuColumns = useMemo<DataTableColumn<ProcessSku>[]>(
        () => [
            { key: "name", label: "Product", sortable: true, filterType: "text" },
            { key: "siteSku", label: "Site SKU", sortable: true, filterType: "text", monospace: true },
            {
                key: "quantity",
                label: `Quantity required (${process ? getUnitShortLabel(process.measuredIn) : ""})`,
                sortable: true,
                filterType: "number",
                monospace: true,
                width: 260,
                render: row => formatQuantity(row.quantity),
            },
        ],
        [process?.measuredIn],
    );

    return (
        <EntityViewPage
            title={process?.name ?? id}
            entityId={id}
            backUrl={PROCESSES_URL}
            backLabel="Processes"
            isLoading={isProcessLoading}
            statusChips={
                process && (
                    <>
                        <ProcessTypeChip type={process.type} />
                        <ProcessStatusChip status={process.status} />
                        <Box component="span" sx={{ fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                            Measured in {getUnitLabel(process.measuredIn)}
                        </Box>
                    </>
                )
            }
            tabs={
                process
                    ? [
                          {
                              key: "specifications",
                              label: "Specifications",
                              content: <ProcessSpecificationsTab process={process} />,
                          },
                          { key: "factories", label: "Factories", content: <ProcessFactoriesTab process={process} /> },
                          {
                              key: "skus",
                              label: "SKUs using this",
                              count: process.skus.length,
                              content: (
                                  <SectionCard
                                      title="SKUs using this process"
                                      headerRight={<LockTag text="Read only · Source: Info file / Supplier" />}
                                      noPadding>
                                      <ClientDataTable
                                          columns={skuColumns}
                                          rows={process.skus}
                                          getRowId={row => row.skuUuid}
                                          noun="SKUs"
                                          defaultSort={{ sortBy: "siteSku", sortDirection: "asc" }}
                                          onRowClick={row => history.push(`${SKUS_URL}/${row.skuUuid}`)}
                                          emptyText="No SKUs map this process."
                                      />
                                  </SectionCard>
                              ),
                          },
                          {
                              key: "audit",
                              label: "Audit",
                              content: (
                                  <HistoryTab
                                      key={process.lastUpdate}
                                      entityType={PROCESS_HISTORY_ENTITY_TYPE}
                                      entityId={process.processId}
                                  />
                              ),
                          },
                      ]
                    : []
            }
        />
    );
};

export default ProcessViewPage;
