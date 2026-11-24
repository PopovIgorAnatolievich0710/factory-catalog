import CategoryOutlinedIcon from "@mui/icons-material/CategoryOutlined";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import { COMPONENTS_URL, COMPONENT_STOCK_LEVELS_URL } from "../../constants/urls";
import { Permissions } from "../../core/auth/permissions";
import { ModuleNavItem, NavGroups } from "../../entities/navigation";

const componentsCatalogNavigation: ModuleNavItem[] = [
    {
        group: NavGroups.Configurations,
        order: 10,
        title: "Components",
        href: COMPONENTS_URL,
        icon: CategoryOutlinedIcon,
        permission: Permissions.Components.View,
    },
    {
        group: NavGroups.Overview,
        order: 10,
        title: "Component stock levels",
        href: COMPONENT_STOCK_LEVELS_URL,
        icon: Inventory2OutlinedIcon,
        permission: Permissions.Components.View,
    },
];

export default componentsCatalogNavigation;
