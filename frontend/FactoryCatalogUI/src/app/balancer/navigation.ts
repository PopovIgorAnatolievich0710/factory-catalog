import AltRouteOutlinedIcon from "@mui/icons-material/AltRouteOutlined";
import ManageSearchOutlinedIcon from "@mui/icons-material/ManageSearchOutlined";
import EventNoteOutlinedIcon from "@mui/icons-material/EventNoteOutlined";
import SwapHorizOutlinedIcon from "@mui/icons-material/SwapHorizOutlined";
import { BALANCER_ROUTING_URL, BALANCER_DECISION_LOG_URL, BALANCER_DAILY_PLAN_URL, BALANCER_TRANSFERS_URL } from "../../constants/urls";
import { Permissions } from "../../core/auth/permissions";
import { ModuleNavItem, NavGroups } from "../../entities/navigation";

const balancerNavigation: ModuleNavItem[] = [
    {
        group: NavGroups.Balancer,
        order: 10,
        title: "Routing dashboard",
        href: BALANCER_ROUTING_URL,
        icon: AltRouteOutlinedIcon,
        permission: Permissions.Balancer.ViewDashboards,
    },
    {
        group: NavGroups.Balancer,
        order: 20,
        title: "Balancer decision log",
        href: BALANCER_DECISION_LOG_URL,
        icon: ManageSearchOutlinedIcon,
        permission: Permissions.Balancer.View,
    },
    {
        group: NavGroups.Balancer,
        order: 30,
        title: "Daily plan",
        href: BALANCER_DAILY_PLAN_URL,
        icon: EventNoteOutlinedIcon,
        permission: Permissions.Balancer.View,
    },
    {
        group: NavGroups.Balancer,
        order: 40,
        title: "Transfers",
        href: BALANCER_TRANSFERS_URL,
        icon: SwapHorizOutlinedIcon,
        permission: Permissions.Balancer.View,
    },
];

export default balancerNavigation;
