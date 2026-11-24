import { AbilityBuilder, createMongoAbility, MongoAbility } from "@casl/ability";
import { getKnownRoles, RoleCode, Roles } from "./role";
import { Permission, PermissionActions, Permissions, PermissionSubjects } from "./permissions";

export type AppAbility = MongoAbility<[string, string]>;

export interface AbilityOptions {
    permissionsEnabled: boolean;
}

const P = Permissions;

const viewEveryModule: Permission[] = [
    P.Components.View,
    P.Processes.View,
    P.Skus.View,
    P.Balancer.View,
];

const factoryManagerPermissions: Permission[] = [
    ...viewEveryModule,
    P.Components.EditManualOos,
    P.Processes.EditFactorySetup,
    P.Processes.EditCapacity,
    P.Processes.ViewCost,
    P.Skus.EditProducible,
    P.Skus.EditShareOverride,
    P.Balancer.ViewDailyPlan,
    P.Balancer.Transfer,
];

const RolePermissions: Record<Exclude<RoleCode, typeof Roles.BOAdministrator>, Permission[]> = {
    [Roles.FactoryManager]: factoryManagerPermissions,
    [Roles.ShiftManager]: factoryManagerPermissions,
    [Roles.SupplyChainManager]: [
        ...viewEveryModule,
        P.Components.EditCriticalLevels,
        P.Components.EditAlternatives,
        P.Components.ViewCost,
        P.Components.EditCost,
    ],
    [Roles.CatalogAdmin]: [
        ...viewEveryModule,
        P.Components.ViewCost,
        P.Sync.Monitor,
        P.Sync.Trigger,
        P.Processes.Edit,
        P.Processes.ViewCost,
        P.Skus.EditGlobalShare,
        P.Skus.EditMappings,
    ],
    [Roles.ProductionFloor]: viewEveryModule,
    [Roles.OperationsManager]: [
        ...viewEveryModule,
        P.Balancer.ViewDashboards,
        P.Balancer.EditReservations,
        P.Balancer.Transfer,
    ],
    [Roles.Finance]: [
        ...viewEveryModule,
        P.Components.ViewCost,
        P.Processes.ViewCost,
        P.Balancer.ViewDimCost,
    ],
};

export const defineAbilitiesFor = (roles: string[], options: AbilityOptions): AppAbility => {
    const { can, build } = new AbilityBuilder<AppAbility>(createMongoAbility);

    if (!options.permissionsEnabled) {
        can(PermissionActions.Manage, PermissionSubjects.All);
        return build();
    }

    getKnownRoles(roles).forEach(role => {
        if (role === Roles.BOAdministrator) {
            can(PermissionActions.Manage, PermissionSubjects.All);
            return;
        }
        RolePermissions[role].forEach(permission => can(PermissionActions.Access, permission));
    });

    return build();
};
