import React from "react";
import { BrowserRouter } from "react-router-dom";
import { StyledEngineProvider, ThemeProvider } from "@mui/material";
import { LocalizationProvider } from "@mui/x-date-pickers";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import GlobalStyles from "../../../bo-frontend/src/components/GlobalStyles";
import Loader from "../../../bo-frontend/src/components/Loader/Loader";
import TopBar from "../../../bo-frontend/src/components/DashboardLayout/components/TopBar/TopBar";
import TGLayoutsStyles from "../../../bo-frontend/src/components/DashboardLayout/DashboardLayoutStyles";
import GroupedNavBar from "./grouped-nav-bar";
import factoryCatalogTheme from "../../theme";
import { NavSection } from "../../entities/navigation";

interface AppLayoutProps {
    title: string;
    user: string;
    appLoading: boolean;
    navSections: NavSection[];
    children: React.ReactNode;
}

const LayoutFrame: React.FC<AppLayoutProps> = ({ title, user, appLoading, navSections, children }) => {
    const classes = TGLayoutsStyles({ navbarCollapsed: false });

    return (
        <div className={classes.root}>
            <TopBar title={title} user={user} onBeforeSiteSwitch={undefined} />
            <GroupedNavBar sections={navSections} />
            <Loader appLoading={appLoading} />
            <div className={classes.wrapper}>
                <div className={classes.contentContainer}>
                    <div className={classes.content}>{children}</div>
                </div>
            </div>
        </div>
    );
};

const AppLayout: React.FC<AppLayoutProps> = props => (
    <StyledEngineProvider injectFirst>
        <ThemeProvider theme={factoryCatalogTheme}>
            <GlobalStyles />
            <LocalizationProvider dateAdapter={AdapterDayjs}>
                <BrowserRouter>
                    <LayoutFrame {...props} />
                </BrowserRouter>
            </LocalizationProvider>
        </ThemeProvider>
    </StyledEngineProvider>
);

export default AppLayout;
