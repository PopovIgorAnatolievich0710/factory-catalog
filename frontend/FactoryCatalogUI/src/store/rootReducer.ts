import {combineReducers} from "redux";
import notificationReducer from "../app/notifications/notification-reducer";
import loadingReducer from "../app/loading/loading-reducer";
import authReducer from "../app/auth/store/auth-reducer";
import currentUserReducer from "../app/current-user/store/current-user-reducer";
import factoriesReducer from "../app/factories/store/factories-reducer";
import componentsCatalogReducer from "../app/components-catalog/store";
import processesReducer from "../app/processes/store";
import skusReducer from "../app/skus/store";
import balancerReducer from "../app/balancer/store";

export const reducerMap = {
    notification: notificationReducer,
    loading: loadingReducer,
    auth: authReducer,
    currentUser: currentUserReducer,
    factories: factoriesReducer,
    componentsCatalog: componentsCatalogReducer,
    processes: processesReducer,
    skus: skusReducer,
    balancer: balancerReducer,
};

export const rootReducer = combineReducers(reducerMap);

export type RootState = ReturnType<typeof rootReducer>;
