import React, { useEffect } from "react";
import { useDispatch } from "react-redux";
import { useParams } from "react-router-dom";
import Box from "@mui/material/Box";
import EntityViewPage from "../../../components/entity-view-page";
import HistoryTab from "../../../components/history-tab";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { COMPONENTS_URL } from "../../../constants/urls";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { loadComponent } from "../store/actions";
import ComponentStockStatus from "./component-stock-status";
import SpecificationsTab from "./tabs/specifications-tab";
import StockByFactoryTab from "./tabs/stock-by-factory-tab";
import AlternativesTab from "./tabs/alternatives-tab";
import SkusUsingTab from "./tabs/skus-using-tab";
import SettingsTab from "./tabs/settings-tab";
import { ComponentStatuses } from "../entities/component";
import { COMPONENT_HISTORY_ENTITY_TYPE, STOCK_TAB_KEY } from "../constants";
import { getFactoryCode } from "../utils/format";

const ComponentViewPage: React.FC = () => {
    const { erpId } = useParams<{ erpId: string }>();
    const dispatch = useDispatch();
    const { factories } = useFactories();
    const { component: loadedComponent, isComponentLoading, isComponentSaving } = useRootStateSelector(
        state => state.componentsCatalog,
    );
    const component = loadedComponent?.erpId === erpId ? loadedComponent : null;

    useEffect(() => {
        dispatch(loadComponent(erpId) as any);
    }, [dispatch, erpId]);

    return (
        <EntityViewPage
            title={component?.name ?? erpId}
            subtitle={component?.family ?? undefined}
            entityId={erpId}
            backUrl={COMPONENTS_URL}
            backLabel="Components"
            isLoading={isComponentLoading || isComponentSaving}
            statusChips={
                component && (
                    <>
                        {component.status === ComponentStatuses.Inactive && (
                            <StatusChip label="Inactive in SAP" colors={statusColors.neutral} />
                        )}
                        {component.factories.map(factory => (
                            <Box key={factory.factoryId} sx={{ display: "inline-flex", alignItems: "center", gap: "4px" }}>
                                <Box component="span" sx={{ fontSize: 11, fontWeight: 600, color: "rgba(0, 0, 0, 0.55)" }}>
                                    {getFactoryCode(factories, factory.factoryId)}
                                </Box>
                                <ComponentStockStatus status={factory.status} small />
                            </Box>
                        ))}
                    </>
                )
            }
            tabs={
                component
                    ? [
                          {
                              key: "specifications",
                              label: "Specifications",
                              content: <SpecificationsTab component={component} />,
                          },
                          {
                              key: STOCK_TAB_KEY,
                              label: "Stock by factory",
                              content: <StockByFactoryTab component={component} />,
                          },
                          {
                              key: "alternatives",
                              label: "Alternatives",
                              count: component.alternatives.length,
                              content: <AlternativesTab component={component} />,
                          },
                          {
                              key: "skus",
                              label: "SKUs using this",
                              count: component.usedInSkus.length,
                              content: <SkusUsingTab component={component} />,
                          },
                          { key: "settings", label: "Settings", content: <SettingsTab component={component} /> },
                          {
                              key: "audit",
                              label: "Audit",
                              content: <HistoryTab entityType={COMPONENT_HISTORY_ENTITY_TYPE} entityId={component.erpId} />,
                          },
                      ]
                    : []
            }
        />
    );
};

export default ComponentViewPage;
