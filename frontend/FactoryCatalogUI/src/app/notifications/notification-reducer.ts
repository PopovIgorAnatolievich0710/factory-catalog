import {
    NOTIFICATION_ERROR,
    NOTIFICATION_INFO,
    NOTIFICATION_RESET,
    NOTIFICATION_SUCCESS,
    NOTIFICATION_WARNING
} from "./action-types";
import NotificationType from "../../enums/notification-type";

export type Notification = {
    type: string,
    message: string[]
};

const notificationReducer = (state: Notification | null = null, action) : {type: string, message: string[]} | null => {

    switch (action.type) {

        case NOTIFICATION_INFO:
            return {
                type: NotificationType.Info,
                message: action.payload
            };

        case NOTIFICATION_SUCCESS:
            return {
                type: NotificationType.Success,
                message: action.payload
            };

        case NOTIFICATION_WARNING:
            return {
                type: NotificationType.Warning,
                message: action.payload
            };

        case NOTIFICATION_ERROR:
            return {
                type: NotificationType.Error,
                message: action.payload
            };

      case NOTIFICATION_RESET:
            return null;

        default:
            return state;
    }
};

export default notificationReducer;

export type AppNotification = ReturnType<typeof notificationReducer>;