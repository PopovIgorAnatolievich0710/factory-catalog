import {LOADING_FINISH, LOADING_START} from "./action-types";

const loadingReducer = (state = false, action) => {
    switch (action.type) {

        case LOADING_START:
            return true;

        case LOADING_FINISH:
            return false;

        default:
            return state;
    }
};

export default loadingReducer;

export type Loading = ReturnType<typeof loadingReducer>;