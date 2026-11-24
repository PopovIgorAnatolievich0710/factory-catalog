export interface FeatureToggle {
    name: string;
    isEnabled: boolean;
}

export const FeatureToggleNames = {
    PermissionsEnableAll: "permissions.enable_all",
} as const;
