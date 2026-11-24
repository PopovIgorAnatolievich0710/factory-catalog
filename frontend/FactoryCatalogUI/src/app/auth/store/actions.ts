import {SET_AUTH} from "./action-types";

export const setAuth = (isAuth: boolean, userName: string | null, isInit: boolean) => {
    return {
        type: SET_AUTH,
        payload: {isAuth: isAuth, userName: userName, isInit: isInit}
    }
};