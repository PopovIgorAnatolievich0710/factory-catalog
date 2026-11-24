import PrecisionManufacturingOutlinedIcon from "@mui/icons-material/PrecisionManufacturingOutlined";
import SpeedOutlinedIcon from "@mui/icons-material/SpeedOutlined";
import { PROCESSES_URL, PROCESS_CAPACITY_URL } from "../../constants/urls";
import { Permissions } from "../../core/auth/permissions";
import { ModuleNavItem, NavGroups } from "../../entities/navigation";

const processesNavigation: ModuleNavItem[] = [
    {
        group: NavGroups.Configurations,
        order: 20,
        title: "Processes",
        href: PROCESSES_URL,
        icon: PrecisionManufacturingOutlinedIcon,
        permission: Permissions.Processes.View,
    },
    {
        group: NavGroups.Overview,
        order: 20,
        title: "Process capacity",
        href: PROCESS_CAPACITY_URL,
        icon: SpeedOutlinedIcon,
        permission: Permissions.Processes.View,
    },
];

export default processesNavigation;
