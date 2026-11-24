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

const RolePermissions: Record<Exclude<RoleCode, typeof Roles.BOAdministrator>, Permission[]> = {
    [Roles.FactoryManager]: [
        ...viewEveryModule,
        P.Components.EditManualOos,
        P.Processes.EditCapacity,
        P.Skus.EditProducible,
        P.Skus.EditShareOverride,
    ],
    [Roles.SupplyChainManager]: [
        ...viewEveryModule,
        P.Components.EditCriticalLevels,
        P.Components.EditAlternatives,
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
    [Roles.ProductionFloor]: [
        P.Components.View,
        P.Processes.View,
        P.Skus.View,
    ],
    [Roles.OperationsManager]: [
        ...viewEveryModule,
        P.Balancer.ViewDashboards,
        P.Balancer.EditReservations,
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
