import React, { useEffect } from "react";
import { useDispatch } from "react-redux";
import { useParams } from "react-router-dom";
import EntityViewPage, { EntityViewTab } from "../../../components/entity-view-page";
import HistoryTab from "../../../components/history-tab";
import { SKUS_URL } from "../../../constants/urls";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { ProductionConflictChip, SkuStatusChip, SkuTypeChip, SkuValidationChip } from "../components/sku-chips";
import { SkuProductTypes } from "../entities";
import { loadSku } from "../store/actions";
import { SKU_HISTORY_ENTITY_TYPE } from "../urls";
import SkuOverviewTab from "./sku-overview-tab";
import SkuComponentsTab from "./sku-components-tab";
import SkuProcessesTab from "./sku-processes-tab";
import SkuFactoriesTab from "./sku-factories-tab";

const SkuViewPage: React.FC = () => {
    const { id } = useParams<{ id: string }>();
    const dispatch = useDispatch();
    const { sku, isSkuLoading } = useRootStateSelector(state => state.skus);
    const current = sku?.skuUuid === id ? sku : null;

    useEffect(() => {
        dispatch(loadSku(id) as any);
    }, [dispatch, id]);

    const tabs: EntityViewTab[] = [];
    if (current) {
        tabs.push({ key: "overview", label: "Overview", content: <SkuOverviewTab sku={current} /> });
        if (current.productType !== SkuProductTypes.Dropshipper) {
            tabs.push(
                {
                    key: "components",
                    label: "Components",
                    count: current.components.length,
                    content: <SkuComponentsTab sku={current} />,
                },
                {
                    key: "processes",
                    label: "Processes",
                    count: current.processes.length,
                    content: <SkuProcessesTab sku={current} />,
                },
                { key: "factories", label: "Factories", content: <SkuFactoriesTab sku={current} /> },
                {
                    key: "audit",
                    label: "Audit",
                    content: <HistoryTab entityType={SKU_HISTORY_ENTITY_TYPE} entityId={current.skuUuid} />,
                },
            );
        }
    }

    return (
        <EntityViewPage
            title={current?.name ?? "SKU"}
            subtitle={current ? [current.siteSku, ...current.aliases.map(alias => alias.id)].join(" · ") : undefined}
            entityId={current?.siteSku ?? id}
            backUrl={SKUS_URL}
            backLabel="SKU"
            isLoading={isSkuLoading}
            statusChips={
                current && (
                    <>
                        <SkuTypeChip type={current.productType} />
                        <SkuStatusChip status={current.status} />
                        <SkuValidationChip status={current.validation.status} />
                        {current.productionConflict.status === "Conflict" && <ProductionConflictChip />}
                    </>
                )
            }
            tabs={tabs}
        />
    );
};

export default SkuViewPage;
