export const Roles = {
    FactoryManager: "FC-FactoryManagers",
    ShiftManager: "FC-ShiftManagers",
    SupplyChainManager: "FC-SupplyChainManagers",
    CatalogAdmin: "FC-CatalogAdmins",
    ProductionFloor: "FC-ProductionFloor",
    OperationsManager: "FC-OperationsManagers",
    Finance: "FC-Finance",
    BOAdministrator: "BO_Administrator",
} as const;

export type RoleCode = (typeof Roles)[keyof typeof Roles];

const knownRoles: string[] = Object.values(Roles);

export const getKnownRoles = (roles: string[]): RoleCode[] =>
    roles.filter(role => knownRoles.includes(role)) as RoleCode[];
