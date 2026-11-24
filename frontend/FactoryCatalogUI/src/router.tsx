import React, { lazy, Suspense } from 'react';
import { Switch, Route, Redirect } from 'react-router-dom';
import LinearProgress from "@mui/material/LinearProgress";
import PrivateRoute from "./app/components/private-route";
import HomeContainer from "./app/components/home/home-container";
import HealthCheckContainer from "./app/components/health-check/health-check-container";
import NotFoundContainer from "./app/components/not-found/not-found-container";
import {
    HEALTH_CHECK_URL,
    PING_URL,
    NOT_FOUND,
    SIGN_IN_CALLBACK_URL,
    COMPONENTS_URL,
    COMPONENT_STOCK_LEVELS_URL,
    PROCESSES_URL,
    PROCESS_CAPACITY_URL,
    SKUS_URL,
    FACTORY_SHARE_URL,
    BALANCER_URL,
} from "./constants/urls"
import { Permissions } from "./core/auth/permissions";

const ComponentsCatalogRoutes = lazy(() => import("./app/components-catalog/routes"));
const ProcessesRoutes = lazy(() => import("./app/processes/routes"));
const SkusRoutes = lazy(() => import("./app/skus/routes"));
const BalancerRoutes = lazy(() => import("./app/balancer/routes"));

const AppRoutes = () => {
    return (
        <Suspense fallback={<LinearProgress />}>
            <Switch>
                <Route exact path="/">
                    <HomeContainer/>
                </Route>
                <Redirect exact from={SIGN_IN_CALLBACK_URL} to="/"/>

                <Route exact path={PING_URL} component={HealthCheckContainer}/>
                <Route exact path={HEALTH_CHECK_URL} component={HealthCheckContainer}/>
                <Route exact path={NOT_FOUND} component={NotFoundContainer}/>

                <PrivateRoute path={[COMPONENTS_URL, COMPONENT_STOCK_LEVELS_URL]} requiredPermission={Permissions.Components.View}>
                    <ComponentsCatalogRoutes />
                </PrivateRoute>

                <PrivateRoute path={[PROCESSES_URL, PROCESS_CAPACITY_URL]} requiredPermission={Permissions.Processes.View}>
                    <ProcessesRoutes />
                </PrivateRoute>

                <PrivateRoute path={[SKUS_URL, FACTORY_SHARE_URL]} requiredPermission={Permissions.Skus.View}>
                    <SkusRoutes />
                </PrivateRoute>

                <PrivateRoute path={BALANCER_URL} requiredPermission={Permissions.Balancer.View}>
                    <BalancerRoutes />
                </PrivateRoute>

                <Route component={NotFoundContainer}/>
            </Switch>
        </Suspense>
    )
}

export default AppRoutes;
