import { useCallback, useContext } from "react";
import { useAbility } from "@casl/react";
import { AbilityContext, PermissionLoadingContext } from "./can";
import { useCurrentUser } from "./current-user";

export const PermissionActions = {
    Access: "access",
    Manage: "manage",
} as const;

export const PermissionSubjects = {
    All: "all",
} as const;

export const Permissions = {
    Components: {
        View: "Components.View",
        EditManualOos: "Components.EditManualOos",
        EditCriticalLevels: "Components.EditCriticalLevels",
        EditAlternatives: "Components.EditAlternatives",
        ViewCost: "Components.ViewCost",
        EditCost: "Components.EditCost",
    },
    Sync: {
        Monitor: "Sync.Monitor",
        Trigger: "Sync.Trigger",
    },
    Processes: {
        View: "Processes.View",
        Edit: "Processes.Edit",
        EditFactorySetup: "Processes.EditFactorySetup",
        EditCapacity: "Processes.EditCapacity",
        ViewCost: "Processes.ViewCost",
    },
    Skus: {
        View: "Skus.View",
        EditProducible: "Skus.EditProducible",
        EditShareOverride: "Skus.EditShareOverride",
        EditGlobalShare: "Skus.EditGlobalShare",
        EditMappings: "Skus.EditMappings",
    },
    Balancer: {
        View: "Balancer.View",
        ViewDashboards: "Balancer.ViewDashboards",
        EditReservations: "Balancer.EditReservations",
        ViewDimCost: "Balancer.ViewDimCost",
        ViewDailyPlan: "Balancer.ViewDailyPlan",
        Transfer: "Balancer.Transfer",
    },
} as const;

type Leaves<T> = T extends string ? T : { [K in keyof T]: Leaves<T[K]> }[keyof T];

export type Permission = Leaves<typeof Permissions>;

export const usePermissions = () => {
    const ability = useAbility(AbilityContext);
    const isLoading = useContext(PermissionLoadingContext);
    const { canEditFactory } = useCurrentUser();

    const can = useCallback(
        (permission: Permission) => ability.can(PermissionActions.Access, permission),
        [ability],
    );

    const canInFactory = useCallback(
        (permission: Permission, factoryId: string | number) => can(permission) && canEditFactory(factoryId),
        [can, canEditFactory],
    );

    return {
        can,
        canInFactory,
        ability,
        isLoading,
    };
};
