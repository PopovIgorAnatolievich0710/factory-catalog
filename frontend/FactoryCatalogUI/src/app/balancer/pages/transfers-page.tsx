import React, { useState } from "react";
import Box from "@mui/material/Box";
import LinearProgress from "@mui/material/LinearProgress";
import ToggleButton from "@mui/material/ToggleButton";
import ToggleButtonGroup from "@mui/material/ToggleButtonGroup";
import LockTag from "../../../components/lock-tag";
import { useCurrentUser } from "../../../core/auth/current-user";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { Roles } from "../../../core/auth/role";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import TransferItemsTab from "../components/transfer-items-tab";
import TransferLogTab from "../components/transfer-log-tab";
import { PageTitle, pageSx } from "../components/balancer-ui";

const Tabs = {
    Items: "items",
    Log: "log",
} as const;

type Tab = (typeof Tabs)[keyof typeof Tabs];

const anySupplierRoles: string[] = [Roles.OperationsManager, Roles.BOAdministrator];

const TransfersPage: React.FC = () => {
    const { can } = usePermissions();
    const { user, permissionsEnabled } = useCurrentUser();
    const { isOpenItemsLoading, isTransferLogLoading, transferLog } = useRootStateSelector(state => state.balancer);
    const [tab, setTab] = useState<Tab>(Tabs.Items);

    const canTransfer = can(Permissions.Balancer.Transfer);
    const isAnySupplier = !permissionsEnabled || (user?.roles ?? []).some(role => anySupplierRoles.includes(role));
    const ownFactories = (user?.factoryIds ?? []).join(", ");
    const ownFactoryNote =
        canTransfer && !isAnySupplier
            ? `You can transfer items from ${ownFactories || "your factory"}; operations can transfer from any supplier.`
            : null;

    return (
        <Box sx={pageSx}>
            <PageTitle title="Transfers">
                {tab === Tabs.Items && ownFactoryNote && <LockTag text={ownFactoryNote} />}
                {tab === Tabs.Items && !canTransfer && <LockTag text="Read only · Transfers need the transfer permission" />}
            </PageTitle>

            <ToggleButtonGroup
                exclusive
                size="small"
                color="primary"
                value={tab}
                onChange={(_event, next: Tab | null) => next && setTab(next)}
                sx={{ alignSelf: "flex-start" }}>
                <ToggleButton value={Tabs.Items} sx={{ textTransform: "none", padding: "4px 14px" }}>
                    Find &amp; transfer
                </ToggleButton>
                <ToggleButton value={Tabs.Log} sx={{ textTransform: "none", padding: "4px 14px" }}>
                    Transfer log{transferLog.totalCount ? ` (${transferLog.totalCount})` : ""}
                </ToggleButton>
            </ToggleButtonGroup>

            <Box sx={{ height: 4 }}>{(isOpenItemsLoading || isTransferLogLoading) && <LinearProgress />}</Box>

            {tab === Tabs.Items ? <TransferItemsTab canTransfer={canTransfer} ownFactoryNote={ownFactoryNote} /> : <TransferLogTab />}
        </Box>
    );
};

export default TransfersPage;
