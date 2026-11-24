import React from "react";
import { Redirect, Route, Switch } from "react-router-dom";
import { BALANCER_URL, BALANCER_ROUTING_URL, BALANCER_DECISION_LOG_URL, BALANCER_DAILY_PLAN_URL, BALANCER_TRANSFERS_URL } from "../../constants/urls";
import PrivateRoute from "../components/private-route";
import { Permissions } from "../../core/auth/permissions";
import RoutingDashboardPage from "./pages/routing-dashboard-page";
import DecisionLogPage from "./pages/decision-log-page";
import DailyPlanPage from "./pages/daily-plan-page";
import TransfersPage from "./pages/transfers-page";

const BalancerRoutes = () => (
    <Switch>
        <Redirect exact from={BALANCER_URL} to={BALANCER_DECISION_LOG_URL} />
        <PrivateRoute exact path={BALANCER_ROUTING_URL} requiredPermission={Permissions.Balancer.ViewDashboards}>
            <RoutingDashboardPage />
        </PrivateRoute>
        <Route exact path={BALANCER_DECISION_LOG_URL}>
            <DecisionLogPage />
        </Route>
        <PrivateRoute exact path={BALANCER_DAILY_PLAN_URL} requiredPermission={Permissions.Balancer.ViewDailyPlan}>
            <DailyPlanPage />
        </PrivateRoute>
        <Route exact path={BALANCER_TRANSFERS_URL}>
            <TransfersPage />
        </Route>
    </Switch>
);

export default BalancerRoutes;
