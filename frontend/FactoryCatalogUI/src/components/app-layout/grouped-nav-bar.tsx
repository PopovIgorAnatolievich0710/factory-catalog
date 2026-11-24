import React from "react";
import { NavLink as RouterLink, useLocation } from "react-router-dom";
import Box from "@mui/material/Box";
import ButtonBase from "@mui/material/ButtonBase";
import Drawer from "@mui/material/Drawer";
import AppSwitcher from "../../../bo-frontend/src/components/DashboardLayout/components/AppSwitcher/AppSwitcher";
import { NavSection } from "../../entities/navigation";

const TOP_BAR_HEIGHT = 64;
const DRAWER_WIDTH = 294;
const APP_SWITCHER_WIDTH = 100;
const ACTIVE_BACKGROUND = "rgba(25, 118, 210, 0.08)";

interface GroupedNavBarProps {
    sections: NavSection[];
}

const GroupedNavBar: React.FC<GroupedNavBarProps> = ({ sections }) => {
    const location = useLocation();

    return (
        <Drawer
            anchor="left"
            open
            variant="persistent"
            PaperProps={{
                sx: {
                    width: DRAWER_WIDTH,
                    top: TOP_BAR_HEIGHT,
                    height: `calc(100% - ${TOP_BAR_HEIGHT}px)`,
                    background: "primary.main",
                    border: "none",
                    overflow: "hidden",
                },
            }}>
            <Box sx={{ display: "grid", gridTemplateColumns: `${APP_SWITCHER_WIDTH}px 1fr`, height: "100%" }}>
                <AppSwitcher />
                <Box
                    component="nav"
                    sx={{
                        background: "#fff",
                        borderTopLeftRadius: "5px",
                        borderRight: "1px solid rgba(0, 0, 0, 0.12)",
                        padding: "8px 0",
                        overflowY: "auto",
                    }}>
                    {sections.map(section => (
                        <React.Fragment key={section.group}>
                            <Box
                                sx={{
                                    padding: "14px 16px 6px",
                                    fontSize: 11,
                                    letterSpacing: "0.08333em",
                                    textTransform: "uppercase",
                                    color: "rgba(0, 0, 0, 0.6)",
                                }}>
                                {section.group}
                            </Box>
                            {section.items.map(item => {
                                const isActive =
                                    location.pathname === item.href || location.pathname.startsWith(`${item.href}/`);
                                const Icon = item.icon;
                                return (
                                    <ButtonBase
                                        key={item.href}
                                        component={RouterLink}
                                        to={item.href}
                                        sx={{
                                            display: "flex",
                                            justifyContent: "flex-start",
                                            alignItems: "center",
                                            gap: 1.5,
                                            width: "calc(100% - 16px)",
                                            margin: "1px 8px",
                                            padding: "8px 10px",
                                            borderRadius: "4px",
                                            textAlign: "left",
                                            fontSize: 14,
                                            fontWeight: isActive ? 500 : 400,
                                            color: isActive ? "primary.main" : "rgba(0, 0, 0, 0.87)",
                                            background: isActive ? ACTIVE_BACKGROUND : "transparent",
                                            "&:hover": { background: isActive ? ACTIVE_BACKGROUND : "rgba(0, 0, 0, 0.04)" },
                                        }}>
                                        <Icon sx={{ fontSize: 20, color: isActive ? "primary.main" : "rgba(0, 0, 0, 0.54)" }} />
                                        {item.title}
                                    </ButtonBase>
                                );
                            })}
                        </React.Fragment>
                    ))}
                </Box>
            </Box>
        </Drawer>
    );
};

export default GroupedNavBar;
