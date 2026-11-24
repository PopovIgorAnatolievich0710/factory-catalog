export const HEALTH_CHECK_URL = "/healthcheck";
export const PING_URL = "/ping";
export const NOT_FOUND = "/404";
export const SIGN_IN_CALLBACK_URL = "/signInCallback";

export const COMPONENTS_URL = "/components";
export const PROCESSES_URL = "/processes";
export const SKUS_URL = "/skus";
export const COMPONENT_STOCK_LEVELS_URL = "/component-stock-levels";
export const PROCESS_CAPACITY_URL = "/process-capacity";
export const FACTORY_SHARE_URL = "/factory-share";
export const BALANCER_URL = "/balancer";
export const BALANCER_ROUTING_URL = `${BALANCER_URL}/routing`;
export const BALANCER_DECISION_LOG_URL = `${BALANCER_URL}/decision-log`;
export const BALANCER_DAILY_PLAN_URL = `${BALANCER_URL}/daily-plan`;
export const BALANCER_TRANSFERS_URL = `${BALANCER_URL}/transfers`;

export const USER_ME_API_URL = "v1/user/me";
export const FACTORIES_API_URL = "v1/factories";
export const FEATURE_TOGGLE_API_URL = "v1/feature-toggle";

export const GetListSettingsApiUrl = (listKey: string) => `v1/user/list-settings/${encodeURIComponent(listKey)}`;
export const GetHistoryApiUrl = (entityType: string, entityId: string) =>
    `v1/history/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`;
export const GetSyncStatusApiUrl = (syncType: string) => `v1/sync/${encodeURIComponent(syncType)}/status`;
export const GetSyncRunApiUrl = (syncType: string) => `v1/sync/${encodeURIComponent(syncType)}/run`;
export const GetResourceSearchApiUrl = (resource: string) => `v1/${resource}/search`;
export const GetResourceExportApiUrl = (resource: string) => `v1/${resource}/export`;
