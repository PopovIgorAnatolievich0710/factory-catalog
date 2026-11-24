import React from "react";
import { Redirect, Route, Switch } from "react-router-dom";
import { BALANCER_URL, BALANCER_ROUTING_URL, BALANCER_DECISION_LOG_URL, BALANCER_DAILY_PLAN_URL, BALANCER_TRANSFERS_URL } from "../../constants/urls";
import { balancerApi } from "../../helpers/api-helper";
import PrivateRoute from "../components/private-route";
import { Permissions } from "../../core/auth/permissions";
import KitDemoListPage from "../components/kit-demo/kit-demo-list-page";
import KitDemoViewPage from "../components/kit-demo/kit-demo-view-page";
import KitDemoPlaceholderPage from "../components/kit-demo/kit-demo-placeholder-page";

const BalancerRoutes = () => (
    <Switch>
        <Redirect exact from={BALANCER_URL} to={BALANCER_DECISION_LOG_URL} />
        <PrivateRoute exact path={BALANCER_ROUTING_URL} requiredPermission={Permissions.Balancer.ViewDashboards}>
            <KitDemoPlaceholderPage title="Routing dashboard" />
        </PrivateRoute>
        <Route exact path={BALANCER_DECISION_LOG_URL}>
            <KitDemoListPage
                title="Balancer decision log"
                listKey="allocations"
                resource="allocations"
                baseUrl={BALANCER_DECISION_LOG_URL}
                api={balancerApi}
            />
        </Route>
        <Route exact path={`${BALANCER_DECISION_LOG_URL}/:id`}>
            <KitDemoViewPage
                title="Balancer decision log"
                resource="allocations"
                entityType="allocation"
                baseUrl={BALANCER_DECISION_LOG_URL}
                api={balancerApi}
            />
        </Route>
        <Route exact path={BALANCER_DAILY_PLAN_URL}>
            <KitDemoPlaceholderPage title="Daily plan" />
        </Route>
        <Route exact path={BALANCER_TRANSFERS_URL}>
            <KitDemoPlaceholderPage title="Transfers" />
        </Route>
    </Switch>
);

export default BalancerRoutes;
