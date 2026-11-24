import React from "react";
import ListAltOutlinedIcon from "@mui/icons-material/ListAltOutlined";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import SyncStatusBadge from "../../../components/sync-status-badge";
import SummaryTile from "../../../components/summary-tile";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { SecondaryBoButton } from "../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import PermissionGate from "../../../core/auth/permission-gate";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { Api } from "../../../helpers/api-helper";
import { formatDateTime } from "../../../utils/format-date";

interface KitDemoRow {
    id: string;
    name: string;
    status: string;
    quantity: number;
    updatedAt: string;
}

const columns: CatalogColumn<KitDemoRow>[] = [
    { key: "id", label: "ID", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
    { key: "name", label: "Name", alwaysVisible: true, sortable: true, filterType: "text" },
    {
        key: "status",
        label: "Status",
        alwaysVisible: true,
        sortable: true,
        filterType: "values",
        filterValues: [
            { value: "active", label: "Active" },
            { value: "inactive", label: "Inactive" },
        ],
        render: row => (
            <StatusChip
                label={row.status === "active" ? "Active" : "Inactive"}
                colors={row.status === "active" ? statusColors.success : statusColors.neutral}
            />
        ),
    },
    { key: "quantity", label: "Quantity", sortable: true, filterType: "number", align: "right" },
    { key: "updatedAt", label: "Updated at", sortable: true, render: row => formatDateTime(row.updatedAt) },
];

interface KitDemoListPageProps {
    title: string;
    listKey: string;
    resource: string;
    baseUrl: string;
    api?: Api;
    syncType?: string;
    syncLabel?: string;
}

const KitDemoListPage: React.FC<KitDemoListPageProps> = ({
    title,
    listKey,
    resource,
    baseUrl,
    api,
    syncType,
    syncLabel,
}) => {
    const { can } = usePermissions();

    return (
        <CatalogListPage<KitDemoRow>
            listKey={listKey}
            title={title}
            subtitle="Placeholder list built with the page kit"
            resource={resource}
            api={api}
            columns={columns}
            getRowId={row => row.id}
            getRowUrl={row => `${baseUrl}/${encodeURIComponent(row.id)}`}
            defaultSort={{ sortBy: "id", sortDirection: "asc" }}
            searchPlaceholder="Search by ID or name"
            headerInfo={
                syncType && (
                    <SyncStatusBadge
                        syncType={syncType}
                        label={syncLabel}
                        api={api}
                        canTrigger={can(Permissions.Sync.Trigger)}
                    />
                )
            }
            summary={<SummaryTile icon={<ListAltOutlinedIcon />} label={`Total ${title}`} value="-" />}
            isActionRequired={row => row.status === "inactive"}
            selectable
            bulkActions={(selectedIds, { clearSelection }) => (
                <PermissionGate allowed={can(Permissions.Sync.Trigger)}>
                    <SecondaryBoButton size="small" onClick={clearSelection}>
                        Apply to {selectedIds.length} rows
                    </SecondaryBoButton>
                </PermissionGate>
            )}
        />
    );
};

export default KitDemoListPage;
