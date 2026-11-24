import { useCallback } from "react";
import { useDispatch } from "react-redux";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { SkuFactoryShare, SkuShareSources } from "../entities";
import { resetSkuShareOverrides, setSkuProducible, setSkuShareOverride } from "../store/actions";
import skuService from "../sku-service";
import { SKU_SHARES_UPDATED } from "../store/action-types";
import { useErrorNotification } from "../../../helpers/response-helper";

export const useShareEditing = () => {
    const dispatch = useDispatch();
    const notifyError = useErrorNotification();
    const { canInFactory } = usePermissions();

    const canEditProducible = useCallback(
        (factoryId: string) => canInFactory(Permissions.Skus.EditProducible, factoryId),
        [canInFactory],
    );
    const canEditShare = useCallback(
        (factoryId: string) => canInFactory(Permissions.Skus.EditShareOverride, factoryId),
        [canInFactory],
    );

    const changeProducible = (skuUuid: string, factoryId: string, producible: boolean) =>
        dispatch(setSkuProducible(skuUuid, factoryId, producible) as any);

    const changeShare = (skuUuid: string, factoryId: string, sharePct: number | null) => {
        if (sharePct !== null) {
            dispatch(setSkuShareOverride(skuUuid, factoryId, sharePct) as any);
            return;
        }
        skuService
            .resetShareOverride(skuUuid, factoryId)
            .then(shares => dispatch({ type: SKU_SHARES_UPDATED, payload: shares }))
            .catch(notifyError);
    };

    const resettableFactories = (factories: SkuFactoryShare[]) =>
        factories.filter(f => f.source === SkuShareSources.Override && canEditShare(f.factoryId));

    const resetToDefault = (skuUuid: string, factories: SkuFactoryShare[]) =>
        dispatch(resetSkuShareOverrides(skuUuid, resettableFactories(factories)) as any);

    return { canEditProducible, canEditShare, changeProducible, changeShare, resettableFactories, resetToDefault };
};
