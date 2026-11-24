import { useCallback } from "react";
import { useRootStateSelector } from "../../store/selectors/rootStateSelector";
import { CurrentUser } from "../../entities/user";
import { getKnownRoles, Roles } from "./role";

export const hasAnyRole = (user: CurrentUser | null): boolean =>
    user !== null && getKnownRoles(user.roles).length > 0;

export const canEditFactory = (
    user: CurrentUser | null,
    permissionsEnabled: boolean,
    factoryId: string | number,
): boolean => {
    if (!permissionsEnabled) return true;
    if (!user) return false;
    if (user.roles.includes(Roles.BOAdministrator)) return true;
    return user.factoryIds.map(String).includes(String(factoryId));
};

export const useCurrentUser = () => {
    const { user, permissionsEnabled, isLoaded } = useRootStateSelector(state => state.currentUser);

    const canEditOwnFactory = useCallback(
        (factoryId: string | number) => canEditFactory(user, permissionsEnabled, factoryId),
        [user, permissionsEnabled],
    );

    return {
        user,
        isLoaded,
        permissionsEnabled,
        hasAccess: !permissionsEnabled || hasAnyRole(user),
        canEditFactory: canEditOwnFactory,
    };
};
