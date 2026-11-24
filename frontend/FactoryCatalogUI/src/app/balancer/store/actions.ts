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
import balancerService from "../services/balancer-service";
import { DashboardFilter } from "../entities";
import { ListRequest } from "../../../entities/list";
import { errorResponseHandler } from "../../../helpers/response-helper";
import { notificationError } from "../../notifications/actions";

const load = (loading: string, loaded: string, failed: string, request: () => Promise<unknown>) => async dispatch => {
    dispatch({ type: loading });
    try {
        dispatch({ type: loaded, payload: await request() });
    } catch (error) {
        dispatch({ type: failed });
        errorResponseHandler(error, dispatch, notificationError);
    }
};

export const loadRoutingDashboard = (filter: DashboardFilter, withShadow: boolean) =>
    load(BALANCER_DASHBOARD_LOADING, BALANCER_DASHBOARD_LOADED, BALANCER_DASHBOARD_FAILED, async () => {
        const [dashboard, shadow] = await Promise.all([
            balancerService.fetchRoutingDashboard(filter),
            withShadow ? balancerService.fetchShadowComparison(filter) : Promise.resolve(null),
        ]);
        return { dashboard, shadow };
    });

export const loadDecisions = (request: ListRequest) =>
    load(BALANCER_DECISIONS_LOADING, BALANCER_DECISIONS_LOADED, BALANCER_DECISIONS_FAILED, () =>
        balancerService.searchAllocations(request),
    );

export const loadDailyPlan = (factoryId: string, date: string) =>
    load(BALANCER_DAILY_PLAN_LOADING, BALANCER_DAILY_PLAN_LOADED, BALANCER_DAILY_PLAN_FAILED, () =>
        balancerService.fetchDailyPlan(factoryId, date),
    );

export const loadOpenItems = (request: ListRequest) =>
    load(BALANCER_OPEN_ITEMS_LOADING, BALANCER_OPEN_ITEMS_LOADED, BALANCER_OPEN_ITEMS_FAILED, () =>
        balancerService.searchOpenItems(request),
    );

export const loadTransferLog = (request: ListRequest) =>
    load(BALANCER_TRANSFER_LOG_LOADING, BALANCER_TRANSFER_LOG_LOADED, BALANCER_TRANSFER_LOG_FAILED, () =>
        balancerService.searchTransferLog(request),
    );
