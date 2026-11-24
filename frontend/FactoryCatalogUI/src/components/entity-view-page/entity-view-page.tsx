import React from "react";
import { Link as RouterLink, useHistory, useLocation } from "react-router-dom";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import Link from "@mui/material/Link";
import Tab from "@mui/material/Tab";
import Tabs from "@mui/material/Tabs";
import Typography from "@mui/material/Typography";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { tableStyles } from "../data-table";

const TAB_QUERY_PARAM = "tab";

export interface EntityViewTab {
    key: string;
    label: string;
    count?: number;
    content: React.ReactNode;
}

export interface EntityViewPageProps {
    title: string;
    subtitle?: string;
    entityId?: string;
    backUrl?: string;
    backLabel?: string;
    statusChips?: React.ReactNode;
    actions?: React.ReactNode;
    tabs: EntityViewTab[];
    isLoading?: boolean;
}

const EntityViewPage: React.FC<EntityViewPageProps> = ({
    title,
    subtitle,
    entityId,
    backUrl,
    backLabel = "Back",
    statusChips,
    actions,
    tabs,
    isLoading = false,
}) => {
    const history = useHistory();
    const location = useLocation();

    const requestedTabKey = new URLSearchParams(location.search).get(TAB_QUERY_PARAM);
    const activeTab = tabs.find(tab => tab.key === requestedTabKey) ?? tabs[0];

    const onTabChange = (_event: React.SyntheticEvent, tabKey: string) => {
        const params = new URLSearchParams();
        params.set(TAB_QUERY_PARAM, tabKey);
        history.push({ pathname: location.pathname, search: params.toString() });
    };

    return (
        <Box sx={{ padding: "24px", display: "flex", flexDirection: "column", gap: 2 }}>
            {backUrl && (
                <Box sx={{ display: "flex", alignItems: "center", gap: 1, fontSize: 13, color: "rgba(0, 0, 0, 0.6)" }}>
                    <Link
                        component={RouterLink}
                        to={backUrl}
                        underline="none"
                        color="inherit"
                        sx={{ display: "inline-flex", alignItems: "center", gap: "4px" }}>
                        <ArrowBackIcon sx={{ fontSize: 16 }} />
                        {backLabel}
                    </Link>
                    {entityId && (
                        <>
                            <ChevronRightIcon sx={{ fontSize: 14 }} />
                            <Box component="span" sx={tableStyles.monospace}>
                                {entityId}
                            </Box>
                        </>
                    )}
                </Box>
            )}

            <Box sx={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: 2, flexWrap: "wrap" }}>
                <div>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1.25, flexWrap: "wrap" }}>
                        <Typography component="h1" sx={{ fontSize: 22, fontWeight: 400, color: "rgba(0, 0, 0, 0.87)" }}>
                            {title}
                        </Typography>
                        {statusChips}
                    </Box>
                    {subtitle && (
                        <Typography sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)", marginTop: "2px" }}>
                            {subtitle}
                        </Typography>
                    )}
                </div>
                {actions && <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>{actions}</Box>}
            </Box>

            <Box sx={{ height: 4 }}>{isLoading && <LinearProgress />}</Box>

            {activeTab && (
                <>
                    <Tabs
                        value={activeTab.key}
                        onChange={onTabChange}
                        indicatorColor="primary"
                        textColor="primary"
                        variant="scrollable"
                        sx={{ borderBottom: "1px solid rgba(0, 0, 0, 0.12)" }}>
                        {tabs.map(tab => (
                            <Tab
                                key={tab.key}
                                value={tab.key}
                                sx={{ textTransform: "none", fontSize: 14 }}
                                label={
                                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                                        {tab.label}
                                        {tab.count !== undefined && (
                                            <Box
                                                component="span"
                                                sx={{
                                                    fontSize: 11,
                                                    padding: "0 6px",
                                                    borderRadius: "8px",
                                                    background: "rgba(0, 0, 0, 0.08)",
                                                    color: "rgba(0, 0, 0, 0.6)",
                                                }}>
                                                {tab.count}
                                            </Box>
                                        )}
                                    </Box>
                                }
                            />
                        ))}
                    </Tabs>
                    <div>{activeTab.content}</div>
                </>
            )}
        </Box>
    );
};

export default EntityViewPage;
