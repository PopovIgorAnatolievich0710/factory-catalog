import React from "react";
import { Route, Switch } from "react-router-dom";
import { SKUS_URL, FACTORY_SHARE_URL } from "../../constants/urls";
import KitDemoListPage from "../components/kit-demo/kit-demo-list-page";
import KitDemoViewPage from "../components/kit-demo/kit-demo-view-page";
import KitDemoPlaceholderPage from "../components/kit-demo/kit-demo-placeholder-page";

const SkusRoutes = () => (
    <Switch>
        <Route exact path={SKUS_URL}>
            <KitDemoListPage
                title="SKU"
                listKey="skus"
                resource="skus"
                baseUrl={SKUS_URL}
                syncType="master-catalog-skus"
                syncLabel="Last master catalog sync"
            />
        </Route>
        <Route exact path={`${SKUS_URL}/:id`}>
            <KitDemoViewPage
                title="SKU"
                resource="skus"
                entityType="sku"
                baseUrl={SKUS_URL}
            />
        </Route>
        <Route exact path={FACTORY_SHARE_URL}>
            <KitDemoPlaceholderPage title="Factory share" />
        </Route>
    </Switch>
);

export default SkusRoutes;
