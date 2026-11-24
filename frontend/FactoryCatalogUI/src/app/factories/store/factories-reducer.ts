import { FACTORIES_FAILED, FACTORIES_LOADED, FACTORIES_LOADING } from "./action-types";
import { Factory } from "../../../entities/factory";

export interface FactoriesState {
    items: Factory[];
    status: "idle" | "loading" | "loaded" | "failed";
}

const initialState: FactoriesState = {
    items: [],
    status: "idle",
};

const factoriesReducer = (state = initialState, action): FactoriesState => {
    switch (action.type) {

        case FACTORIES_LOADING:
            return { ...state, status: "loading" };

        case FACTORIES_LOADED:
            return { items: action.payload, status: "loaded" };

        case FACTORIES_FAILED:
            return { ...state, status: "failed" };

        default:
            return state;
    }
};

export default factoriesReducer;
