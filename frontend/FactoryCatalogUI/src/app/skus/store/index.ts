import {
    GLOBAL_FACTORY_SHARE_LOADED,
    SKU_FAILED,
    SKU_LOADED,
    SKU_LOADING,
    SKU_SHARES_CLEARED,
    SKU_SHARES_UPDATED,
    SKUS_SUMMARY_LOADED,
} from "./action-types";
import { GlobalFactoryShare, SkuDetails, SkuFactoryShares, SkusSummary } from "../entities";

export interface SkusState {
    summary: SkusSummary | null;
    sku: SkuDetails | null;
    isSkuLoading: boolean;
    globalShare: GlobalFactoryShare | null;
    sharesBySku: Record<string, SkuFactoryShares>;
}

const initialState: SkusState = {
    summary: null,
    sku: null,
    isSkuLoading: false,
    globalShare: null,
    sharesBySku: {},
};

const skusReducer = (state = initialState, action: { type?: string; payload?: any } = {}): SkusState => {
    switch (action.type) {

        case SKUS_SUMMARY_LOADED:
            return { ...state, summary: action.payload };

        case SKU_LOADING:
            return { ...state, sku: null, isSkuLoading: true };

        case SKU_LOADED:
            return { ...state, sku: action.payload, isSkuLoading: false };

        case SKU_FAILED:
            return { ...state, isSkuLoading: false };

        case GLOBAL_FACTORY_SHARE_LOADED:
            return { ...state, globalShare: action.payload };

        case SKU_SHARES_UPDATED: {
            const shares: SkuFactoryShares = action.payload;
            return {
                ...state,
                sharesBySku: { ...state.sharesBySku, [shares.skuUuid]: shares },
                sku: state.sku?.skuUuid === shares.skuUuid
                    ? { ...state.sku, factories: shares.factories, shareTotalPct: shares.shareTotalPct }
                    : state.sku,
            };
        }

        case SKU_SHARES_CLEARED:
            return { ...state, sharesBySku: {} };

        default:
            return state;
    }
};

export default skusReducer;
