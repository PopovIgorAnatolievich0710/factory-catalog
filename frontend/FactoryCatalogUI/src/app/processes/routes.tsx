import React from "react";
import { Route, Switch } from "react-router-dom";
import { PROCESSES_URL, PROCESS_CAPACITY_URL } from "../../constants/urls";
import ProcessesListPage from "./pages/processes-list-page";
import ProcessViewPage from "./pages/process-view-page";
import ProcessCapacityPage from "./pages/process-capacity-page";

const ProcessesRoutes = () => (
    <Switch>
        <Route exact path={PROCESSES_URL}>
            <ProcessesListPage />
        </Route>
        <Route exact path={`${PROCESSES_URL}/:id`}>
            <ProcessViewPage />
        </Route>
        <Route exact path={PROCESS_CAPACITY_URL}>
            <ProcessCapacityPage />
        </Route>
    </Switch>
);

export default ProcessesRoutes;
