import {
    GLOBAL_FACTORY_SHARE_LOADED,
    SKU_FAILED,
    SKU_LOADED,
    SKU_LOADING,
    SKU_SHARES_CLEARED,
    SKU_SHARES_UPDATED,
    SKUS_SUMMARY_LOADED,
} from "./action-types";
import skuService from "../sku-service";
import { FactorySharePct, SkuFactoryShare, SkuFactoryShares, SkuShareSources } from "../entities";
import { errorResponseHandler } from "../../../helpers/response-helper";
import { notificationError, notificationSuccess } from "../../notifications/actions";

const notifyError = (error, dispatch) => errorResponseHandler(error, dispatch, notificationError);

export const loadSkusSummary = () => async dispatch => {
    try {
        dispatch({ type: SKUS_SUMMARY_LOADED, payload: await skuService.fetchSummary() });
    } catch (error) {
        notifyError(error, dispatch);
    }
};

export const loadSku = (skuUuid: string) => async dispatch => {
    dispatch({ type: SKU_LOADING });
    try {
        dispatch({ type: SKU_LOADED, payload: await skuService.fetch(skuUuid) });
    } catch (error) {
        dispatch({ type: SKU_FAILED });
        notifyError(error, dispatch);
    }
};

export const loadGlobalFactoryShare = () => async dispatch => {
    try {
        dispatch({ type: GLOBAL_FACTORY_SHARE_LOADED, payload: await skuService.fetchGlobalShare() });
    } catch (error) {
        notifyError(error, dispatch);
    }
};

export const saveGlobalFactoryShare = (factoryShares: FactorySharePct[]) => async dispatch => {
    try {
        dispatch({ type: GLOBAL_FACTORY_SHARE_LOADED, payload: await skuService.updateGlobalShare(factoryShares) });
        dispatch({ type: SKU_SHARES_CLEARED });
        dispatch(notificationSuccess("Default balance saved"));
    } catch (error) {
        notifyError(error, dispatch);
    }
};

export const clearSkuShares = () => ({ type: SKU_SHARES_CLEARED });

const saveShares = (request: () => Promise<SkuFactoryShares>) => async dispatch => {
    try {
        dispatch({ type: SKU_SHARES_UPDATED, payload: await request() });
    } catch (error) {
        notifyError(error, dispatch);
    }
};

export const setSkuProducible = (skuUuid: string, factoryId: string, producible: boolean) =>
    saveShares(() => skuService.setProducible(skuUuid, factoryId, producible));

export const setSkuShareOverride = (skuUuid: string, factoryId: string, sharePct: number) =>
    saveShares(() => skuService.setShareOverride(skuUuid, factoryId, sharePct));

export const resetSkuShareOverrides = (skuUuid: string, factories: SkuFactoryShare[]) =>
    saveShares(async () => {
        let shares: SkuFactoryShares | null = null;
        for (const factory of factories.filter(f => f.source === SkuShareSources.Override)) {
            shares = await skuService.resetShareOverride(skuUuid, factory.factoryId);
        }
        return shares ?? { skuUuid, factories, shareTotalPct: factories.reduce((sum, f) => sum + f.effectiveSharePct, 0) };
    });
