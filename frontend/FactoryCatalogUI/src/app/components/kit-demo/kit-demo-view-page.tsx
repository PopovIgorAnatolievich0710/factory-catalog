import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import Box from "@mui/material/Box";
import EntityViewPage from "../../../components/entity-view-page";
import HistoryTab from "../../../components/history-tab";
import FactoryGrid from "../../../components/factory-grid";
import SpecificationsTable from "../../../components/specifications-table";
import SectionCard from "../../../components/section-card";
import LockTag from "../../../components/lock-tag";
import GatedCheckbox from "../../../components/gated-checkbox";
import { ClientDataTable, DataTableColumn } from "../../../components/data-table";
import { CapacityChip, FactoryTag, StockStatusChip } from "../../../components/status-chips";
import { Api, catalogApi } from "../../../helpers/api-helper";
import { useErrorNotification } from "../../../helpers/response-helper";
import { useCurrentUser } from "../../../core/auth/current-user";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { Factory } from "../../../entities/factory";
import { StockStatuses } from "../../../entities/status";

const factoryColumns: DataTableColumn<Factory>[] = [
    { key: "id", label: "Factory ID", sortable: true, filterType: "text", monospace: true, width: 160 },
    { key: "name", label: "Name", sortable: true, filterType: "text" },
    {
        key: "countryCode",
        label: "Country",
        sortable: true,
        filterType: "values",
        render: factory => <FactoryTag code={factory.countryCode} dimmed={!factory.isActive} />,
    },
];

interface KitDemoViewPageProps {
    title: string;
    resource: string;
    entityType: string;
    baseUrl: string;
    api?: Api;
}

const KitDemoViewPage: React.FC<KitDemoViewPageProps> = ({ title, resource, entityType, baseUrl, api = catalogApi }) => {
    const { id } = useParams<{ id: string }>();
    const notifyError = useErrorNotification();
    const { canEditFactory } = useCurrentUser();
    const { factories, isLoading: isFactoriesLoading } = useFactories();

    const [entity, setEntity] = useState<Record<string, unknown> | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [flaggedFactoryIds, setFlaggedFactoryIds] = useState<string[]>([]);

    useEffect(() => {
        let cancelled = false;
        setIsLoading(true);
        api.get<Record<string, unknown>>(`v1/${resource}/${encodeURIComponent(id)}`)
            .then(response => {
                if (!cancelled) setEntity(response);
            })
            .catch(error => {
                if (!cancelled) notifyError(error);
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [api, resource, id]);

    const toggleFlag = (factoryId: string, flagged: boolean) => {
        setFlaggedFactoryIds(
            flagged ? [...flaggedFactoryIds, factoryId] : flaggedFactoryIds.filter(x => x !== factoryId),
        );
    };

    return (
        <EntityViewPage
            title={String(entity?.name ?? id)}
            entityId={id}
            backUrl={baseUrl}
            backLabel={title}
            isLoading={isLoading}
            tabs={[
                {
                    key: "specifications",
                    label: "Specifications",
                    content: (
                        <SectionCard title="Specifications" headerRight={<LockTag text="Read only · Source: placeholder" />} noPadding>
                            <SpecificationsTable
                                rows={Object.entries(entity ?? {}).map(([label, value]) => ({
                                    label,
                                    value: typeof value === "object" ? JSON.stringify(value) : String(value),
                                }))}
                            />
                        </SectionCard>
                    ),
                },
                {
                    key: "factories",
                    label: "By factory",
                    content: (
                        <SectionCard title="By factory" headerRight={<LockTag text="Factory manager edits" />} noPadding>
                            <FactoryGrid
                                isRowHighlighted={factory => canEditFactory(factory.id)}
                                columns={[
                                    {
                                        key: "stock",
                                        label: "Status",
                                        width: 160,
                                        render: factory => (
                                            <StockStatusChip
                                                status={
                                                    flaggedFactoryIds.includes(factory.id)
                                                        ? StockStatuses.OosConflict
                                                        : StockStatuses.Ok
                                                }
                                            />
                                        ),
                                    },
                                    {
                                        key: "flag",
                                        label: "OOS flag",
                                        width: 140,
                                        render: factory => (
                                            <GatedCheckbox
                                                label="OOS"
                                                allowed={canEditFactory(factory.id)}
                                                checked={flaggedFactoryIds.includes(factory.id)}
                                                onChange={flagged => toggleFlag(factory.id, flagged)}
                                            />
                                        ),
                                    },
                                    {
                                        key: "capacity",
                                        label: "Capacity",
                                        render: factory => (
                                            <Box sx={{ display: "inline-flex" }}>
                                                <CapacityChip percent={factory.isActive ? 100 : 0} />
                                            </Box>
                                        ),
                                    },
                                ]}
                            />
                        </SectionCard>
                    ),
                },
                {
                    key: "related",
                    label: "Client-side table",
                    count: factories.length,
                    content: (
                        <SectionCard title="Factories" headerRight={<LockTag text="Read only · Source: catalog" />} noPadding>
                            <ClientDataTable
                                columns={factoryColumns}
                                rows={factories}
                                getRowId={factory => factory.id}
                                noun="factories"
                                isActionRequired={factory => !factory.isActive}
                                isLoading={isFactoriesLoading}
                                fixedLayout
                            />
                        </SectionCard>
                    ),
                },
                {
                    key: "audit",
                    label: "Audit",
                    content: <HistoryTab entityType={entityType} entityId={id} api={api} />,
                },
            ]}
        />
    );
};

export default KitDemoViewPage;
