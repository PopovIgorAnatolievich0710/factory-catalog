import {
    NOTIFICATION_ERROR,
    NOTIFICATION_INFO,
    NOTIFICATION_RESET,
    NOTIFICATION_SUCCESS,
    NOTIFICATION_WARNING
} from "./action-types";

const toArrayConvert = (source) => {
    return !Array.isArray(source) ? [source] : source;
};

export const notificationInfo = (message) => {
    return {
        type: NOTIFICATION_INFO,
        payload: toArrayConvert(message)
    }
};

export const notificationWarning = (message) => {
    return {
        type: NOTIFICATION_WARNING,
        payload: toArrayConvert(message)
    }
};

export const notificationError = (message) => {
    return {
        type: NOTIFICATION_ERROR,
        payload: toArrayConvert(message)
    }
};

export const notificationSuccess = (message) => {
    return {
        type: NOTIFICATION_SUCCESS,
        payload: toArrayConvert(message)
    }
};

export const resetNotification = () => {
    return {
        type: NOTIFICATION_RESET
    }
};