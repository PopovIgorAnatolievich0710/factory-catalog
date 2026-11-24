import {
    COMPONENT_FAILED,
    COMPONENT_LOADED,
    COMPONENT_LOADING,
    COMPONENT_SAVED,
    COMPONENT_SAVING,
    FILTER_OPTIONS_LOADED,
} from "./action-types";
import { ComponentDetails, ComponentFilterOptions } from "../entities/component";

export interface ComponentsCatalogState {
    component: ComponentDetails | null;
    isComponentLoading: boolean;
    isComponentSaving: boolean;
    filterOptions: ComponentFilterOptions | null;
}

const initialState: ComponentsCatalogState = {
    component: null,
    isComponentLoading: false,
    isComponentSaving: false,
    filterOptions: null,
};

const componentsCatalogReducer = (state = initialState, action): ComponentsCatalogState => {
    switch (action.type) {
        case COMPONENT_LOADING:
            return { ...state, component: null, isComponentLoading: true };

        case COMPONENT_LOADED:
            return { ...state, component: action.payload, isComponentLoading: false };

        case COMPONENT_FAILED:
            return { ...state, isComponentLoading: false };

        case COMPONENT_SAVING:
            return { ...state, isComponentSaving: true };

        case COMPONENT_SAVED:
            return { ...state, isComponentSaving: false };

        case FILTER_OPTIONS_LOADED:
            return { ...state, filterOptions: action.payload };

        default:
            return state;
    }
};

export default componentsCatalogReducer;
