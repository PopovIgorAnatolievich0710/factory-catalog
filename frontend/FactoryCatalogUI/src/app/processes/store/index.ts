import {
    PROCESS_CAPACITY_FACTORY_LOADED,
    PROCESS_CAPACITY_FAILED,
    PROCESS_CAPACITY_LOADING,
    PROCESS_CAPACITY_OVERVIEW_LOADED,
    PROCESS_CAPACITY_ROW_SAVED,
    PROCESS_FAILED,
    PROCESS_LOADED,
    PROCESS_LOADING,
} from "./action-types";
import { CapacityRow, ProcessDetails } from "../entities";

export interface ProcessesState {
    process: ProcessDetails | null;
    isProcessLoading: boolean;
    capacityOverview: CapacityRow[];
    factoryCapacity: { factoryId: string | null; rows: CapacityRow[] };
    isCapacityLoading: boolean;
}

const initialState: ProcessesState = {
    process: null,
    isProcessLoading: false,
    capacityOverview: [],
    factoryCapacity: { factoryId: null, rows: [] },
    isCapacityLoading: false,
};

const processesReducer = (state = initialState, action): ProcessesState => {
    switch (action.type) {

        case PROCESS_LOADING:
            return {
                ...state,
                process: state.process?.processId === action.payload ? state.process : null,
                isProcessLoading: true,
            };

        case PROCESS_LOADED:
            return { ...state, process: action.payload, isProcessLoading: false };

        case PROCESS_FAILED:
            return { ...state, isProcessLoading: false };

        case PROCESS_CAPACITY_LOADING:
            return { ...state, isCapacityLoading: true };

        case PROCESS_CAPACITY_OVERVIEW_LOADED:
            return { ...state, capacityOverview: action.payload, isCapacityLoading: false };

        case PROCESS_CAPACITY_FACTORY_LOADED:
            return { ...state, factoryCapacity: action.payload, isCapacityLoading: false };

        case PROCESS_CAPACITY_ROW_SAVED:
            if (state.factoryCapacity.factoryId !== action.payload.factoryId) return state;
            return {
                ...state,
                factoryCapacity: {
                    ...state.factoryCapacity,
                    rows: state.factoryCapacity.rows.map(row =>
                        row.processId === action.payload.row.processId ? action.payload.row : row,
                    ),
                },
            };

        case PROCESS_CAPACITY_FAILED:
            return { ...state, isCapacityLoading: false };

        default:
            return state;
    }
};

export default processesReducer;
