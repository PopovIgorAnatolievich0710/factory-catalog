import componentsCatalogNavigation from "./app/components-catalog/navigation";
import processesNavigation from "./app/processes/navigation";
import skusNavigation from "./app/skus/navigation";
import balancerNavigation from "./app/balancer/navigation";
import { ModuleNavItem, NavGroup, NavGroups, NavSection } from "./entities/navigation";

const groupOrder: NavGroup[] = [NavGroups.Configurations, NavGroups.Overview, NavGroups.Balancer];

const moduleNavItems: ModuleNavItem[] = [
    ...componentsCatalogNavigation,
    ...processesNavigation,
    ...skusNavigation,
    ...balancerNavigation,
];

export const getNavSections = (isAllowed: (item: ModuleNavItem) => boolean): NavSection[] =>
    groupOrder
        .map(group => ({
            group,
            items: moduleNavItems
                .filter(item => item.group === group && isAllowed(item))
                .sort((left, right) => left.order - right.order),
        }))
        .filter(section => section.items.length > 0);
