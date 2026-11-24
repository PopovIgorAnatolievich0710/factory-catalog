import {
    BALANCER_DAILY_PLAN_FAILED,
    BALANCER_DAILY_PLAN_LOADED,
    BALANCER_DAILY_PLAN_LOADING,
    BALANCER_DASHBOARD_FAILED,
    BALANCER_DASHBOARD_LOADED,
    BALANCER_DASHBOARD_LOADING,
    BALANCER_DECISIONS_FAILED,
    BALANCER_DECISIONS_LOADED,
    BALANCER_DECISIONS_LOADING,
    BALANCER_OPEN_ITEMS_FAILED,
    BALANCER_OPEN_ITEMS_LOADED,
    BALANCER_OPEN_ITEMS_LOADING,
    BALANCER_TRANSFER_LOG_FAILED,
    BALANCER_TRANSFER_LOG_LOADED,
    BALANCER_TRANSFER_LOG_LOADING,
} from "./action-types";
import { Allocation, DailyPlan, OpenItem, RoutingDashboard, ShadowComparison, TransferLogEntry } from "../entities";
import { ListResponse } from "../../../entities/list";

export interface BalancerState {
    dashboard: RoutingDashboard | null;
    shadow: ShadowComparison | null;
    isDashboardLoading: boolean;
    decisions: ListResponse<Allocation>;
    isDecisionsLoading: boolean;
    dailyPlan: DailyPlan | null;
    isDailyPlanLoading: boolean;
    openItems: ListResponse<OpenItem>;
    isOpenItemsLoading: boolean;
    transferLog: ListResponse<TransferLogEntry>;
    isTransferLogLoading: boolean;
}

const emptyList = { items: [], totalCount: 0 };

const initialState: BalancerState = {
    dashboard: null,
    shadow: null,
    isDashboardLoading: false,
    decisions: emptyList,
    isDecisionsLoading: false,
    dailyPlan: null,
    isDailyPlanLoading: false,
    openItems: emptyList,
    isOpenItemsLoading: false,
    transferLog: emptyList,
    isTransferLogLoading: false,
};

const balancerReducer = (state = initialState, action): BalancerState => {
    switch (action.type) {
        case BALANCER_DASHBOARD_LOADING:
            return { ...state, isDashboardLoading: true };
        case BALANCER_DASHBOARD_LOADED:
            return { ...state, dashboard: action.payload.dashboard, shadow: action.payload.shadow, isDashboardLoading: false };
        case BALANCER_DASHBOARD_FAILED:
            return { ...state, isDashboardLoading: false };

        case BALANCER_DECISIONS_LOADING:
            return { ...state, isDecisionsLoading: true };
        case BALANCER_DECISIONS_LOADED:
            return { ...state, decisions: action.payload, isDecisionsLoading: false };
        case BALANCER_DECISIONS_FAILED:
            return { ...state, isDecisionsLoading: false };

        case BALANCER_DAILY_PLAN_LOADING:
            return { ...state, isDailyPlanLoading: true };
        case BALANCER_DAILY_PLAN_LOADED:
            return { ...state, dailyPlan: action.payload, isDailyPlanLoading: false };
        case BALANCER_DAILY_PLAN_FAILED:
            return { ...state, dailyPlan: null, isDailyPlanLoading: false };

        case BALANCER_OPEN_ITEMS_LOADING:
            return { ...state, isOpenItemsLoading: true };
        case BALANCER_OPEN_ITEMS_LOADED:
            return { ...state, openItems: action.payload, isOpenItemsLoading: false };
        case BALANCER_OPEN_ITEMS_FAILED:
            return { ...state, isOpenItemsLoading: false };

        case BALANCER_TRANSFER_LOG_LOADING:
            return { ...state, isTransferLogLoading: true };
        case BALANCER_TRANSFER_LOG_LOADED:
            return { ...state, transferLog: action.payload, isTransferLogLoading: false };
        case BALANCER_TRANSFER_LOG_FAILED:
            return { ...state, isTransferLogLoading: false };

        default:
            return state;
    }
};

export default balancerReducer;
