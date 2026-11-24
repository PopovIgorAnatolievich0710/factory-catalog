import React from "react";
import { Route, Switch } from "react-router-dom";
import { PROCESSES_URL, PROCESS_CAPACITY_URL } from "../../constants/urls";
import KitDemoListPage from "../components/kit-demo/kit-demo-list-page";
import KitDemoViewPage from "../components/kit-demo/kit-demo-view-page";
import KitDemoPlaceholderPage from "../components/kit-demo/kit-demo-placeholder-page";

const ProcessesRoutes = () => (
    <Switch>
        <Route exact path={PROCESSES_URL}>
            <KitDemoListPage
                title="Processes"
                listKey="processes"
                resource="processes"
                baseUrl={PROCESSES_URL}
            />
        </Route>
        <Route exact path={`${PROCESSES_URL}/:id`}>
            <KitDemoViewPage
                title="Processes"
                resource="processes"
                entityType="process"
                baseUrl={PROCESSES_URL}
            />
        </Route>
        <Route exact path={PROCESS_CAPACITY_URL}>
            <KitDemoPlaceholderPage title="Process capacity" />
        </Route>
    </Switch>
);

export default ProcessesRoutes;
