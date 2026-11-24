import { SET_CURRENT_USER } from "./action-types";
import { CurrentUser } from "../../../entities/user";

export interface CurrentUserState {
    user: CurrentUser | null;
    permissionsEnabled: boolean;
    isLoaded: boolean;
}

const initialState: CurrentUserState = {
    user: null,
    permissionsEnabled: true,
    isLoaded: false,
};

const currentUserReducer = (state = initialState, action): CurrentUserState => {
    switch (action.type) {

        case SET_CURRENT_USER:
            return {
                user: action.payload.user,
                permissionsEnabled: action.payload.permissionsEnabled,
                isLoaded: true,
            };

        default:
            return state;
    }
};

export default currentUserReducer;
