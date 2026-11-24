import {SET_AUTH} from "./action-types";

const initialState = {
    isAuth: undefined,
    userName: undefined,
    isInit: false
}

const authReducer = (state = initialState, action) => {
    switch (action.type) {

        case SET_AUTH:
            return {
                ...state,
                isAuth: action.payload.isAuth,
                userName: action.payload.userName,
                isInit: action.payload.isInit
            };

        default:
            return state;
    }
};

export default authReducer;

export type Auth = ReturnType<typeof authReducer>;