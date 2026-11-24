import React from "react";
import {Route, Redirect} from "react-router-dom";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { Permission, usePermissions } from "../../../core/auth/permissions";
import { NOT_FOUND } from "../../../constants/urls";

interface PrivateRouteProps {
    children?: React.ReactNode;
    requiredPermission?: Permission;
    [key: string]: any;
}

export const PrivateRoute = ({ children, requiredPermission, ...rest }: PrivateRouteProps) => {
    const {isAuth, isInit} = useRootStateSelector(state => state.auth);
    const { can, isLoading } = usePermissions();

    if (!isInit || isLoading || !isAuth) {
        return (<></>);
    }

    return (
        <Route
            {...rest}
            render={() => {
                if (requiredPermission && !can(requiredPermission)) {
                    return <Redirect to={{ pathname: NOT_FOUND }} />;
                }

                return children;
            }}
        />
    );
}
