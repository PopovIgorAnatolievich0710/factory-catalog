import { useCallback } from "react";
import { useDispatch } from "react-redux";
import { loadingFinish } from "../app/loading/actions";
import { notificationError } from "../app/notifications/actions";
import { CONSTANTS } from "../core/CONSTANTS";
import { ApiError } from "../entities/api-error";
import { extractApiErrorMessages, getApiErrorStatus } from "./api-error";

const getFallbackMessage = (error: ApiError): string => {
    const status = getApiErrorStatus(error);

    if (status === null) return error?.error?.message ?? "Unspecified error type.";
    if (status === CONSTANTS.ERROR_CODES.UNAUTHORIZED) return "Access denied. You should log in to the application.";
    if (status === CONSTANTS.ERROR_CODES.FORBIDDEN) return "Access denied. You have no permissions for the operation.";
    if (status === CONSTANTS.ERROR_CODES.NOT_FOUND) return "Entity not found.";
    if (status === CONSTANTS.ERROR_CODES.CONFLICT) return "The operation conflicts with the current state.";
    if (status >= CONSTANTS.ERROR_CODES.ISE) return "Internal server error.";
    return "The request could not be processed.";
};

export const errorResponseHandler = (error: ApiError, dispatch, notificationAction) => {
    dispatch(loadingFinish());
    dispatch(notificationAction(extractApiErrorMessages(error, getFallbackMessage(error))));
};

export const useErrorNotification = () => {
    const dispatch = useDispatch();
    return useCallback((error: ApiError) => errorResponseHandler(error, dispatch, notificationError), [dispatch]);
};
