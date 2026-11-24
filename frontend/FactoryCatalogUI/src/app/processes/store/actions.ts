import {
    PROCESS_CAPACITY_FACTORY_LOADED,
    PROCESS_CAPACITY_FAILED,
    PROCESS_CAPACITY_LOADING,
    PROCESS_CAPACITY_OVERVIEW_LOADED,
    PROCESS_CAPACITY_ROW_SAVED,
    PROCESS_FAILED,
    PROCESS_LOADED,
    PROCESS_LOADING,
} from "./action-types";
import processService from "../services/process-service";
import { ProcessDetails, ProcessFactorySetup, ProcessSpecification, SetCapacityRequest } from "../entities";
import { errorResponseHandler } from "../../../helpers/response-helper";
import { notificationError, notificationSuccess } from "../../notifications/actions";

const saveProcess = (request: () => Promise<ProcessDetails>, successMessage: string) => async dispatch => {
    try {
        dispatch({ type: PROCESS_LOADED, payload: await request() });
        dispatch(notificationSuccess(successMessage));
    } catch (error) {
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const loadProcess = (processId: string) => async dispatch => {
    dispatch({ type: PROCESS_LOADING, payload: processId });
    try {
        dispatch({ type: PROCESS_LOADED, payload: await processService.fetch(processId) });
    } catch (error) {
        dispatch({ type: PROCESS_FAILED });
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const saveProcessSpecification = (processId: string, specification: ProcessSpecification) =>
    saveProcess(() => processService.updateSpecification(processId, specification), "Specifications saved");

export const saveProcessFactorySetup = (processId: string, factoryId: string, setup: ProcessFactorySetup) =>
    saveProcess(() => processService.updateFactorySetup(processId, factoryId, setup), "Factory setup saved");

export const loadCapacityOverview = () => async dispatch => {
    dispatch({ type: PROCESS_CAPACITY_LOADING });
    try {
        dispatch({ type: PROCESS_CAPACITY_OVERVIEW_LOADED, payload: await processService.fetchCapacityOverview() });
    } catch (error) {
        dispatch({ type: PROCESS_CAPACITY_FAILED });
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const loadFactoryCapacity = (factoryId: string) => async dispatch => {
    dispatch({ type: PROCESS_CAPACITY_LOADING });
    try {
        dispatch({
            type: PROCESS_CAPACITY_FACTORY_LOADED,
            payload: { factoryId, rows: await processService.fetchFactoryCapacity(factoryId) },
        });
    } catch (error) {
        dispatch({ type: PROCESS_CAPACITY_FAILED });
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const saveCapacity = (factoryId: string, processId: string, request: SetCapacityRequest) => async dispatch => {
    try {
        const row = await processService.setCapacity(factoryId, processId, request);
        if (row) {
            dispatch({ type: PROCESS_CAPACITY_ROW_SAVED, payload: { factoryId, row } });
        }
        dispatch(notificationSuccess("Capacity saved"));
    } catch (error) {
        errorResponseHandler(error, dispatch, notificationError);
    }
};
