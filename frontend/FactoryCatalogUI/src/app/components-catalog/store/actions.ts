import {
    COMPONENT_FAILED,
    COMPONENT_LOADED,
    COMPONENT_LOADING,
    COMPONENT_SAVED,
    COMPONENT_SAVING,
    FILTER_OPTIONS_LOADED,
} from "./action-types";
import componentsService from "../services/components-service";
import { errorResponseHandler } from "../../../helpers/response-helper";
import { notificationError, notificationSuccess } from "../../notifications/actions";

export const loadComponent = (erpId: string) => async dispatch => {
    dispatch({ type: COMPONENT_LOADING });
    try {
        dispatch({ type: COMPONENT_LOADED, payload: await componentsService.fetch(erpId) });
    } catch (error) {
        dispatch({ type: COMPONENT_FAILED });
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const loadFilterOptions = () => async (dispatch, getState) => {
    if (getState().componentsCatalog.filterOptions) return;
    try {
        dispatch({ type: FILTER_OPTIONS_LOADED, payload: await componentsService.fetchFilterOptions() });
    } catch (error) {
        errorResponseHandler(error, dispatch, notificationError);
    }
};

const saveComponent =
    (erpId: string, save: () => Promise<void>, successMessage: string | null) =>
    async (dispatch): Promise<boolean> => {
        dispatch({ type: COMPONENT_SAVING });
        try {
            await save();
            dispatch({ type: COMPONENT_LOADED, payload: await componentsService.fetch(erpId) });
            if (successMessage) dispatch(notificationSuccess(successMessage));
            return true;
        } catch (error) {
            errorResponseHandler(error, dispatch, notificationError);
            return false;
        } finally {
            dispatch({ type: COMPONENT_SAVED });
        }
    };

export const setManualOos = (erpId: string, factoryId: string, active: boolean, note: string | null, successMessage: string) =>
    saveComponent(erpId, () => componentsService.setManualOos(erpId, factoryId, active, note), successMessage);

export const setCriticalLevel = (erpId: string, level: number | null) =>
    saveComponent(erpId, () => componentsService.setCriticalLevel(erpId, level), "Global critical level saved");

export const setCriticalLevelOverride = (erpId: string, factoryId: string, level: number | null) =>
    saveComponent(
        erpId,
        () => componentsService.setCriticalLevelOverride(erpId, factoryId, level),
        level === null ? "Override cleared" : "Override saved",
    );

export const addAlternative = (erpId: string, alternativeErpId: string) =>
    saveComponent(
        erpId,
        () => componentsService.addAlternative(erpId, alternativeErpId),
        `${alternativeErpId} added as an alternative`,
    );

export const setAlternativeUse = (erpId: string, alternativeErpId: string, forBalancer: boolean, forPlanner: boolean) =>
    saveComponent(erpId, () => componentsService.setAlternativeUse(erpId, alternativeErpId, forBalancer, forPlanner), null);

export const removeAlternative = (erpId: string, alternativeErpId: string) =>
    saveComponent(erpId, () => componentsService.removeAlternative(erpId, alternativeErpId), `${alternativeErpId} removed`);
