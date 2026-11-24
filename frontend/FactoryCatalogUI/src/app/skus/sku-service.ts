import { catalogApi } from "../../helpers/api-helper";
import { ImportValidationResult } from "../../entities/import";
import { FactorySharePct, GlobalFactoryShare, SkuDetails, SkuFactoryShares, SkusSummary } from "./entities";
import {
    GetSkuApiUrl,
    GetSkuProducibleApiUrl,
    GetSkuShareOverrideApiUrl,
    GLOBAL_FACTORY_SHARE_API_URL,
    PROCESS_MAPPING_IMPORT_API_URL,
    PROCESS_MAPPING_IMPORT_VALIDATE_API_URL,
    PRODUCIBLE_BULK_API_URL,
    PRODUCIBLE_IMPORT_API_URL,
    PRODUCIBLE_IMPORT_VALIDATE_API_URL,
    SKUS_SUMMARY_API_URL,
} from "./urls";

const FILE_FIELD = "file";

const toForm = (file: File): FormData => {
    const form = new FormData();
    form.append(FILE_FIELD, file);
    return form;
};

class SkuService {
    fetchSummary(): Promise<SkusSummary> {
        return catalogApi.get<SkusSummary>(SKUS_SUMMARY_API_URL);
    }

    fetch(skuUuid: string): Promise<SkuDetails> {
        return catalogApi.get<SkuDetails>(GetSkuApiUrl(skuUuid));
    }

    setProducible(skuUuid: string, factoryId: string, producible: boolean): Promise<SkuFactoryShares> {
        return catalogApi.put<SkuFactoryShares>(GetSkuProducibleApiUrl(skuUuid, factoryId), { producible });
    }

    setShareOverride(skuUuid: string, factoryId: string, sharePct: number): Promise<SkuFactoryShares> {
        return catalogApi.put<SkuFactoryShares>(GetSkuShareOverrideApiUrl(skuUuid, factoryId), { sharePct });
    }

    resetShareOverride(skuUuid: string, factoryId: string): Promise<SkuFactoryShares> {
        return catalogApi.delete<SkuFactoryShares>(GetSkuShareOverrideApiUrl(skuUuid, factoryId));
    }

    bulkSetProducible(factoryId: string, skuUuids: string[], producible: boolean): Promise<{ updated: number }> {
        return catalogApi.post<{ updated: number }>(PRODUCIBLE_BULK_API_URL, { factoryId, skuUuids, producible });
    }

    validateProducibleImport(file: File): Promise<ImportValidationResult> {
        return catalogApi.postForm<ImportValidationResult>(PRODUCIBLE_IMPORT_VALIDATE_API_URL, toForm(file));
    }

    importProducible(file: File): Promise<void> {
        return catalogApi.postForm<ImportValidationResult>(PRODUCIBLE_IMPORT_API_URL, toForm(file)).then(() => undefined);
    }

    validateProcessMappingImport(file: File): Promise<ImportValidationResult> {
        return catalogApi.postForm<ImportValidationResult>(PROCESS_MAPPING_IMPORT_VALIDATE_API_URL, toForm(file));
    }

    importProcessMappings(file: File): Promise<void> {
        return catalogApi
            .postForm<ImportValidationResult>(PROCESS_MAPPING_IMPORT_API_URL, toForm(file))
            .then(() => undefined);
    }

    fetchGlobalShare(): Promise<GlobalFactoryShare> {
        return catalogApi.get<GlobalFactoryShare>(GLOBAL_FACTORY_SHARE_API_URL);
    }

    updateGlobalShare(factoryShares: FactorySharePct[]): Promise<GlobalFactoryShare> {
        return catalogApi.put<GlobalFactoryShare>(GLOBAL_FACTORY_SHARE_API_URL, { factoryShares });
    }
}

const skuService = new SkuService();
export default skuService;
