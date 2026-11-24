import React from "react";
import { Route, Switch } from "react-router-dom";
import { COMPONENTS_URL, COMPONENT_STOCK_LEVELS_URL } from "../../constants/urls";
import KitDemoListPage from "../components/kit-demo/kit-demo-list-page";
import KitDemoViewPage from "../components/kit-demo/kit-demo-view-page";
import KitDemoPlaceholderPage from "../components/kit-demo/kit-demo-placeholder-page";

const ComponentsCatalogRoutes = () => (
    <Switch>
        <Route exact path={COMPONENTS_URL}>
            <KitDemoListPage
                title="Components"
                listKey="components"
                resource="components"
                baseUrl={COMPONENTS_URL}
                syncType="sap-components"
                syncLabel="Last SAP sync"
            />
        </Route>
        <Route exact path={`${COMPONENTS_URL}/:id`}>
            <KitDemoViewPage
                title="Components"
                resource="components"
                entityType="component"
                baseUrl={COMPONENTS_URL}
            />
        </Route>
        <Route exact path={COMPONENT_STOCK_LEVELS_URL}>
            <KitDemoPlaceholderPage title="Component stock levels" />
        </Route>
    </Switch>
);

export default ComponentsCatalogRoutes;
