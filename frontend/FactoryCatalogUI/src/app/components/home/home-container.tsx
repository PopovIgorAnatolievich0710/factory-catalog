import React from "react";
import { Redirect } from "react-router-dom";
import { usePermissions } from "../../../core/auth/permissions";
import { getNavSections } from "../../../navigation";
import NoAccessContainer from "../no-access/no-access-container";

const HomeContainer = () => {
    const { can } = usePermissions();
    const firstAvailable = getNavSections(item => can(item.permission))[0]?.items[0];

    return firstAvailable ? <Redirect to={firstAvailable.href} /> : <NoAccessContainer />;
};

export default HomeContainer;
