import { FACTORY_SHARE_URL } from "../../constants/urls";

export const FACTORY_SHARE_SETTINGS_URL = `${FACTORY_SHARE_URL}/settings`;

export const SKUS_RESOURCE = "skus";
export const FACTORY_SHARE_RESOURCE = "factory-share";
export const MASTER_CATALOG_SYNC_TYPE = "master-catalog-skus";
export const SKU_HISTORY_ENTITY_TYPE = "sku";
export const GLOBAL_SHARE_HISTORY_ENTITY_TYPE = "global-factory-share";
export const GLOBAL_SHARE_HISTORY_ENTITY_ID = "global";

export const SKUS_SUMMARY_API_URL = "v1/skus/summary";
export const GLOBAL_FACTORY_SHARE_API_URL = "v1/settings/factory-share";
export const PRODUCIBLE_BULK_API_URL = "v1/factory-share/producible/bulk";
export const PRODUCIBLE_IMPORT_VALIDATE_API_URL = "v1/factory-share/producible/import/validate";
export const PRODUCIBLE_IMPORT_API_URL = "v1/factory-share/producible/import";
export const PROCESS_MAPPING_IMPORT_VALIDATE_API_URL = "v1/skus/process-mappings/import/validate";
export const PROCESS_MAPPING_IMPORT_API_URL = "v1/skus/process-mappings/import";

export const GetSkuApiUrl = (skuUuid: string) => `v1/skus/${encodeURIComponent(skuUuid)}`;
export const GetSkuProducibleApiUrl = (skuUuid: string, factoryId: string) =>
    `${GetSkuApiUrl(skuUuid)}/factories/${encodeURIComponent(factoryId)}/producible`;
export const GetSkuShareOverrideApiUrl = (skuUuid: string, factoryId: string) =>
    `${GetSkuApiUrl(skuUuid)}/factories/${encodeURIComponent(factoryId)}/share-override`;
