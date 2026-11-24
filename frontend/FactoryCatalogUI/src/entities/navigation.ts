import { SvgIconComponent } from "@mui/icons-material";
import { Permission } from "../core/auth/permissions";

export const NavGroups = {
    Configurations: "Configurations",
    Overview: "Overview",
    Balancer: "Balancer",
} as const;

export type NavGroup = (typeof NavGroups)[keyof typeof NavGroups];

export interface ModuleNavItem {
    group: NavGroup;
    order: number;
    title: string;
    href: string;
    icon: SvgIconComponent;
    permission: Permission;
}

export interface NavSection {
    group: NavGroup;
    items: ModuleNavItem[];
}
