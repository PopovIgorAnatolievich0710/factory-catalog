import React from "react";
import { Route, Switch } from "react-router-dom";
import { COMPONENTS_URL, COMPONENT_STOCK_LEVELS_URL } from "../../constants/urls";
import ComponentsListPage from "./components/components-list-page";
import ComponentViewPage from "./components/component-view-page";
import ComponentStockLevelsPage from "./components/component-stock-levels-page";

const ComponentsCatalogRoutes = () => (
    <Switch>
        <Route exact path={COMPONENTS_URL}>
            <ComponentsListPage />
        </Route>
        <Route exact path={`${COMPONENTS_URL}/:erpId`}>
            <ComponentViewPage />
        </Route>
        <Route exact path={COMPONENT_STOCK_LEVELS_URL}>
            <ComponentStockLevelsPage />
        </Route>
    </Switch>
);

export default ComponentsCatalogRoutes;
