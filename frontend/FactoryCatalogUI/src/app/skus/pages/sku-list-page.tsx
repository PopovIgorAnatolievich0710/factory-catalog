import React, { useEffect, useMemo, useRef, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import Inventory2OutlinedIcon from "@mui/icons-material/Inventory2Outlined";
import LocalShippingOutlinedIcon from "@mui/icons-material/LocalShippingOutlined";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import ExcelImportDialog from "../../../components/excel-import-dialog";
import SummaryTile from "../../../components/summary-tile";
import SyncStatusBadge from "../../../components/sync-status-badge";
import { FactoryTag } from "../../../components/status-chips";
import { SecondaryBoButton } from "../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { SKUS_URL } from "../../../constants/urls";
import PermissionGate from "../../../core/auth/permission-gate";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { formatDateTime } from "../../../utils/format-date";
import { MutedText, ProductionConflictChip, SkuStatusChip, SkuTypeChip, SkuValidationChip } from "../components/sku-chips";
import { SkuListItem, SkuProductTypes, SkuStatuses, SkuValidationStatuses } from "../entities";
import skuService from "../sku-service";
import { loadSkusSummary } from "../store/actions";
import { MASTER_CATALOG_SYNC_TYPE, SKUS_RESOURCE } from "../urls";

const TILE_VALUE_PLACEHOLDER = "-";

const SkuListPage: React.FC = () => {
    const dispatch = useDispatch();
    const { can } = usePermissions();
    const { factories } = useFactories();
    const summary = useRootStateSelector(state => state.skus.summary);
    const reloadList = useRef<() => void>(() => undefined);
    const [isImportOpen, setIsImportOpen] = useState(false);

    useEffect(() => {
        dispatch(loadSkusSummary() as any);
    }, [dispatch]);

    const refresh = () => {
        dispatch(loadSkusSummary() as any);
        reloadList.current();
    };

    const columns = useMemo<CatalogColumn<SkuListItem>[]>(
        () => [
            { key: "siteSku", label: "Site SKU", alwaysVisible: true, sortable: true, filterType: "text", monospace: true },
            {
                key: "factorySku",
                label: "Factory SKU",
                alwaysVisible: true,
                sortable: true,
                filterType: "text",
                monospace: true,
                render: row => (row.factorySkus.length > 0 ? row.factorySkus.join(", ") : "-"),
            },
            { key: "name", label: "SKU name", alwaysVisible: true, sortable: true, filterType: "text" },
            {
                key: "productType",
                label: "Type",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: Object.values(SkuProductTypes).map(value => ({ value, label: value })),
                render: row => <SkuTypeChip type={row.productType} />,
            },
            {
                key: "status",
                label: "Status",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: Object.values(SkuStatuses).map(value => ({ value, label: value })),
                render: row => <SkuStatusChip status={row.status} />,
            },
            {
                key: "producibleAt",
                label: "Producible at",
                alwaysVisible: true,
                filterType: "values",
                filterValues: factories.map(factory => ({ value: factory.id, label: `${factory.id} · ${factory.name}` })),
                render: row => {
                    if (row.productType === SkuProductTypes.Dropshipper) return <MutedText>Dropshipper</MutedText>;
                    if (row.producibleAt.length === 0) return <MutedText danger>None</MutedText>;
                    return (
                        <Box sx={{ display: "flex", gap: "4px", flexWrap: "wrap" }}>
                            {row.producibleAt.map(factoryId => (
                                <FactoryTag key={factoryId} code={factoryId} />
                            ))}
                        </Box>
                    );
                },
            },
            {
                key: "validationStatus",
                label: "Validation",
                alwaysVisible: true,
                sortable: true,
                filterType: "values",
                filterValues: Object.values(SkuValidationStatuses).map(value => ({ value, label: value })),
                render: row => (
                    <Box sx={{ display: "flex", flexDirection: "column", gap: "4px", alignItems: "flex-start" }}>
                        <SkuValidationChip status={row.validationStatus} />
                        {row.productionConflictFactoryIds.length > 0 && <ProductionConflictChip />}
                    </Box>
                ),
            },
            { key: "skuUuid", label: "UUID", sortable: true, monospace: true },
            { key: "dropshipperName", label: "Dropshipper", sortable: true, filterType: "text" },
            {
                key: "lastMasterSyncAt",
                label: "Last master catalog sync",
                sortable: true,
                render: row => formatDateTime(row.lastMasterSyncAt),
            },
        ],
        [factories],
    );

    return (
        <>
            <CatalogListPage<SkuListItem>
                listKey="skus"
                title="SKU"
                subtitle="Read only from the master catalog"
                resource={SKUS_RESOURCE}
                columns={columns}
                getRowId={row => row.skuUuid}
                getRowUrl={row => `${SKUS_URL}/${row.skuUuid}`}
                defaultSort={{ sortBy: "siteSku", sortDirection: "asc" }}
                searchPlaceholder="Search by UUID, Site SKU, Factory SKU or name"
                headerInfo={
                    <SyncStatusBadge
                        syncType={MASTER_CATALOG_SYNC_TYPE}
                        label="Last master catalog sync"
                        canTrigger={can(Permissions.Sync.Trigger)}
                        onSyncFinished={refresh}
                    />
                }
                summary={
                    <>
                        <SummaryTile
                            icon={<Inventory2OutlinedIcon />}
                            label="Total SKUs"
                            value={summary?.total ?? TILE_VALUE_PLACEHOLDER}
                        />
                        <SummaryTile
                            icon={<LocalShippingOutlinedIcon />}
                            label="Dropshipper"
                            value={summary?.dropshipper ?? TILE_VALUE_PLACEHOLDER}
                            hint="Excluded from factory routing"
                            color="#0288d1"
                        />
                        <SummaryTile
                            icon={<ErrorOutlineIcon />}
                            label="Invalid"
                            value={summary?.invalid ?? TILE_VALUE_PLACEHOLDER}
                            hint="Excluded from quotes until fixed"
                            color="#d32f2f"
                        />
                    </>
                }
                toolbarActions={context => {
                    reloadList.current = context.reload;
                    return (
                        <PermissionGate allowed={can(Permissions.Skus.EditMappings)}>
                            <SecondaryBoButton startIcon={<UploadFileIcon />} onClick={() => setIsImportOpen(true)}>
                                Import process mappings
                            </SecondaryBoButton>
                        </PermissionGate>
                    );
                }}
            />
            <ExcelImportDialog
                open={isImportOpen}
                title="Import SKU process mappings"
                description="Upload the approved .xlsx file: first sheet with columns SKU, Process ID, Quantity (in the process unit) and an 'Approval' sheet with 'Approved by' and 'Approved on'. The mapping of every SKU in the file is replaced."
                onClose={() => setIsImportOpen(false)}
                onValidate={file => skuService.validateProcessMappingImport(file)}
                onConfirm={file => skuService.importProcessMappings(file).then(refresh)}
            />
        </>
    );
};

export default SkuListPage;
