import { SET_CURRENT_USER } from "./action-types";
import userService from "../../../services/user-service";
import featureToggleService from "../../../services/feature-toggle-service";
import { CurrentUser } from "../../../entities/user";

export const setCurrentUser = (user: CurrentUser | null, permissionsEnabled: boolean) => {
    return {
        type: SET_CURRENT_USER,
        payload: { user, permissionsEnabled },
    };
};

export const loadCurrentUser = () => async dispatch => {
    const [permissionsEnabled, user] = await Promise.all([
        featureToggleService
            .fetchAll()
            .then(toggles => featureToggleService.isPermissionsEnabled(toggles))
            .catch(error => {
                console.error("Failed to fetch FeatureToggles", error);
                return true;
            }),
        userService.fetchMe().catch(error => {
            console.error("Failed to fetch current user", error);
            return null;
        }),
    ]);

    dispatch(setCurrentUser(user, permissionsEnabled));
};
