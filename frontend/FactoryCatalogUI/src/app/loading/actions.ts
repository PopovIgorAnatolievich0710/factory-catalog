import {LOADING_FINISH, LOADING_START} from "./action-types";

const loadingStart = () => {
    return {
        type: LOADING_START
    }
};

const loadingFinish = () => {
    return {
        type: LOADING_FINISH
    }
};

export {loadingStart, loadingFinish};