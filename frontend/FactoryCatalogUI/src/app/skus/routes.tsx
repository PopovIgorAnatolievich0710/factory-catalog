import React from "react";
import { Route, Switch } from "react-router-dom";
import { SKUS_URL, FACTORY_SHARE_URL } from "../../constants/urls";
import PrivateRoute from "../components/private-route";
import { Permissions } from "../../core/auth/permissions";
import SkuListPage from "./pages/sku-list-page";
import SkuViewPage from "./pages/sku-view-page";
import FactorySharePage from "./pages/factory-share-page";
import FactoryShareSettingsPage from "./pages/factory-share-settings-page";
import { FACTORY_SHARE_SETTINGS_URL } from "./urls";

const SkusRoutes = () => (
    <Switch>
        <Route exact path={SKUS_URL}>
            <SkuListPage />
        </Route>
        <Route exact path={`${SKUS_URL}/:id`}>
            <SkuViewPage />
        </Route>
        <PrivateRoute exact path={FACTORY_SHARE_SETTINGS_URL} requiredPermission={Permissions.Skus.EditGlobalShare}>
            <FactoryShareSettingsPage />
        </PrivateRoute>
        <Route exact path={FACTORY_SHARE_URL}>
            <FactorySharePage />
        </Route>
    </Switch>
);

export default SkusRoutes;
