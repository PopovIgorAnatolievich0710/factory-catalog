import { FACTORIES_FAILED, FACTORIES_LOADED, FACTORIES_LOADING } from "./action-types";
import factoryService from "../../../services/factory-service";
import { errorResponseHandler } from "../../../helpers/response-helper";
import { notificationError } from "../../notifications/actions";

export const loadFactories = () => async (dispatch, getState) => {
    const { status } = getState().factories;
    if (status !== "idle") return;

    dispatch({ type: FACTORIES_LOADING });
    try {
        const factories = await factoryService.fetchAll();
        dispatch({ type: FACTORIES_LOADED, payload: factories });
    } catch (error) {
        dispatch({ type: FACTORIES_FAILED });
        errorResponseHandler(error, dispatch, notificationError);
    }
};
