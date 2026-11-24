import React, { useEffect, useMemo } from "react";
import { useDispatch } from "react-redux";
import { AbilityContext, PermissionLoadingContext } from "./can";
import { defineAbilitiesFor } from "./ability";
import { loadCurrentUser } from "../../app/current-user/store/actions";
import { useRootStateSelector } from "../../store/selectors/rootStateSelector";

export const PermissionProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const dispatch = useDispatch();
    const { user, permissionsEnabled, isLoaded } = useRootStateSelector(state => state.currentUser);

    useEffect(() => {
        dispatch(loadCurrentUser() as any);
    }, [dispatch]);

    const ability = useMemo(
        () => defineAbilitiesFor(user?.roles ?? [], { permissionsEnabled }),
        [user, permissionsEnabled],
    );

    return (
        <AbilityContext.Provider value={ability}>
            <PermissionLoadingContext.Provider value={!isLoaded}>
                {children}
            </PermissionLoadingContext.Provider>
        </AbilityContext.Provider>
    );
};
