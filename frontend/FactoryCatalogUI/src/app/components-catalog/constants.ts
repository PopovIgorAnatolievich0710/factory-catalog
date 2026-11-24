export const COMPONENTS_RESOURCE = "components";
export const COMPONENT_STOCK_LEVELS_RESOURCE = "component-stock-levels";
export const COMPONENTS_LIST_KEY = "components";
export const COMPONENT_STOCK_LEVELS_LIST_KEY = "component-stock-levels";
export const SAP_COMPONENTS_SYNC_TYPE = "sap-components";
export const COMPONENT_HISTORY_ENTITY_TYPE = "component";
export const FACTORY_STOCK_COLUMN_PREFIX = "stock_";
export const STOCK_TAB_KEY = "stock";

export const GetComponentApiUrl = (erpId: string) => `v1/components/${encodeURIComponent(erpId)}`;
export const COMPONENT_FILTER_OPTIONS_API_URL = "v1/components/filter-options";
export const GetManualOosApiUrl = (erpId: string, factoryId: string) =>
    `${GetComponentApiUrl(erpId)}/factories/${encodeURIComponent(factoryId)}/manual-oos`;
export const GetCriticalLevelApiUrl = (erpId: string) => `${GetComponentApiUrl(erpId)}/critical-level`;
export const GetCriticalLevelOverrideApiUrl = (erpId: string, factoryId: string) =>
    `${GetComponentApiUrl(erpId)}/factories/${encodeURIComponent(factoryId)}/critical-level-override`;
export const GetAlternativesApiUrl = (erpId: string) => `${GetComponentApiUrl(erpId)}/alternatives`;
export const GetAlternativeApiUrl = (erpId: string, alternativeErpId: string) =>
    `${GetAlternativesApiUrl(erpId)}/${encodeURIComponent(alternativeErpId)}`;
