import InventoryOutlinedIcon from "@mui/icons-material/InventoryOutlined";
import TuneOutlinedIcon from "@mui/icons-material/TuneOutlined";
import { SKUS_URL, FACTORY_SHARE_URL } from "../../constants/urls";
import { Permissions } from "../../core/auth/permissions";
import { ModuleNavItem, NavGroups } from "../../entities/navigation";

const skusNavigation: ModuleNavItem[] = [
    {
        group: NavGroups.Configurations,
        order: 30,
        title: "SKU",
        href: SKUS_URL,
        icon: InventoryOutlinedIcon,
        permission: Permissions.Skus.View,
    },
    {
        group: NavGroups.Overview,
        order: 30,
        title: "Factory share",
        href: FACTORY_SHARE_URL,
        icon: TuneOutlinedIcon,
        permission: Permissions.Skus.View,
    },
];

export default skusNavigation;
