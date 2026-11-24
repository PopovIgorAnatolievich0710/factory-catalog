import React, { useEffect, useMemo, useRef, useState } from "react";
import { useDispatch } from "react-redux";
import { Link as RouterLink, useHistory } from "react-router-dom";
import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import CloudDoneOutlinedIcon from "@mui/icons-material/CloudDoneOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import CatalogListPage, { CatalogColumn } from "../../../components/catalog-list-page";
import { tableStyles } from "../../../components/data-table";
import ExcelImportDialog from "../../../components/excel-import-dialog";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { SecondaryBoButton } from "../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { SKUS_URL } from "../../../constants/urls";
import PermissionGate from "../../../core/auth/permission-gate";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import { useErrorNotification } from "../../../helpers/response-helper";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { notificationSuccess } from "../../notifications/actions";
import ShareCell from "../components/share-cell";
import ShareTotalCheck from "../components/share-total-check";
import { SkuStatusChip } from "../components/sku-chips";
import { useShareEditing } from "../components/use-share-editing";
import { FactoryShareRow, formatPct, SkuFactoryShares, SkuStatuses } from "../entities";
import skuService from "../sku-service";
import { clearSkuShares, loadGlobalFactoryShare } from "../store/actions";
import { FACTORY_SHARE_RESOURCE, FACTORY_SHARE_SETTINGS_URL } from "../urls";

const OWN_COLUMN_BACKGROUND = "rgba(25, 118, 210, 0.06)";
const NO_FACTORY_TEXT = "No factory";

const LegendSwatch: React.FC<{ label: string; background: string; border: string }> = ({ label, background, border }) => (
    <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
        <Box component="span" sx={{ width: 14, height: 14, borderRadius: "3px", background, border: `1px solid ${border}` }} />
        {label}
    </Box>
);

const FactorySharePage: React.FC = () => {
    const dispatch = useDispatch();
    const history = useHistory();
    const notifyError = useErrorNotification();
    const { can } = usePermissions();
    const { factories } = useFactories();
    const { globalShare, sharesBySku } = useRootStateSelector(state => state.skus);
    const { canEditProducible, canEditShare, changeProducible, changeShare, resettableFactories, resetToDefault } =
        useShareEditing();
    const reloadList = useRef<() => void>(() => undefined);
    const [isImportOpen, setIsImportOpen] = useState(false);

    const activeFactories = useMemo(() => factories.filter(factory => factory.isActive), [factories]);
    const ownFactoryIds = activeFactories
        .filter(factory => canEditProducible(factory.id) || canEditShare(factory.id))
        .map(factory => factory.id);
    const bulkFactoryIds = activeFactories.filter(factory => canEditProducible(factory.id)).map(factory => factory.id);
    const canEditDefault = can(Permissions.Skus.EditGlobalShare);

    useEffect(() => {
        dispatch(clearSkuShares());
        dispatch(loadGlobalFactoryShare() as any);
    }, [dispatch]);

    const refresh = () => {
        dispatch(clearSkuShares());
        reloadList.current();
    };

    const current = (row: FactoryShareRow): SkuFactoryShares => sharesBySku[row.skuUuid] ?? row;

    const bulkSetProducible = async (factoryId: string, skuUuids: string[], producible: boolean, clearSelection: () => void) => {
        try {
            const result = await skuService.bulkSetProducible(factoryId, skuUuids, producible);
            dispatch(notificationSuccess(`${result.updated} SKUs updated for factory ${factoryId}`));
            clearSelection();
            refresh();
        } catch (error) {
            notifyError(error);
        }
    };

    const ownKey = ownFactoryIds.join(",");
    const columns = useMemo<CatalogColumn<FactoryShareRow>[]>(
        () => [
            {
                key: "siteSku",
                label: "Site SKU",
                alwaysVisible: true,
                sortable: true,
                filterType: "text",
                render: row => (
                    <Link component={RouterLink} to={`${SKUS_URL}/${row.skuUuid}?tab=factories`} underline="hover" sx={tableStyles.monospace}>
                        {row.siteSku}
                    </Link>
                ),
            },
            { key: "name", label: "SKU name", alwaysVisible: true, sortable: true, filterType: "text" },
            {
                key: "status",
                label: "Status",
                sortable: true,
                filterType: "values",
                filterValues: Object.values(SkuStatuses).map(value => ({ value, label: value })),
                render: row => <SkuStatusChip status={row.status} />,
            },
            ...activeFactories.map<CatalogColumn<FactoryShareRow>>(factory => {
                const isOwn = ownFactoryIds.includes(factory.id);
                return {
                    key: `share.${factory.id}`,
                    label: `${factory.id} · producible / share %${isOwn ? " · yours" : ""}`,
                    alwaysVisible: true,
                    align: "center",
                    width: 230,
                    render: row => {
                        const share = current(row).factories.find(item => item.factoryId === factory.id);
                        return share ? (
                            <Box sx={{ margin: "-6px -8px", padding: "6px 8px", borderRadius: "4px", background: isOwn ? OWN_COLUMN_BACKGROUND : "transparent" }}>
                                <ShareCell
                                    share={share}
                                    canEditProducible={canEditProducible(factory.id)}
                                    canEditShare={canEditShare(factory.id)}
                                    centered
                                    onProducibleChange={producible => changeProducible(row.skuUuid, factory.id, producible)}
                                    onShareChange={sharePct => changeShare(row.skuUuid, factory.id, sharePct)}
                                />
                            </Box>
                        ) : null;
                    },
                };
            }),
            {
                key: "total",
                label: "Total",
                alwaysVisible: true,
                width: 110,
                render: row => {
                    const shares = current(row);
                    return (
                        <ShareTotalCheck
                            totalPct={shares.shareTotalPct}
                            emptyText={shares.factories.some(share => share.producible) ? null : NO_FACTORY_TEXT}
                        />
                    );
                },
            },
            {
                key: "reset",
                label: "",
                alwaysVisible: true,
                align: "right",
                width: 170,
                render: row => {
                    const shares = current(row);
                    return ownFactoryIds.length > 0 ? (
                        <SecondaryBoButton
                            size="small"
                            startIcon={<RestartAltIcon />}
                            disabled={resettableFactories(shares.factories).length === 0}
                            onClick={() => resetToDefault(row.skuUuid, shares.factories)}>
                            Reset to default
                        </SecondaryBoButton>
                    ) : null;
                },
            },
        ],
        [activeFactories, ownKey, sharesBySku, canEditProducible, canEditShare],
    );

    return (
        <>
            <CatalogListPage<FactoryShareRow>
                listKey="factory-share"
                title="Factory share"
                subtitle="Which factories can produce each SKU and how volume is split between them"
                resource={FACTORY_SHARE_RESOURCE}
                columns={columns}
                getRowId={row => row.skuUuid}
                defaultSort={{ sortBy: "siteSku", sortDirection: "asc" }}
                searchPlaceholder="Search by Site SKU, Factory SKU, UUID or name"
                headerInfo={
                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1.75, fontSize: 13, flexWrap: "wrap" }}>
                        {ownFactoryIds.length > 0 ? (
                            <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                                <EditOutlinedIcon sx={{ fontSize: 16, color: "#1976d2" }} />
                                You can edit the <b>{ownFactoryIds.join(", ")}</b> column.
                            </Box>
                        ) : (
                            <LockTag text={canEditDefault ? "You edit the default balance only" : "Read only for your role"} />
                        )}
                        <LegendSwatch label="Default" background="#fafafa" border="rgba(0, 0, 0, 0.2)" />
                        <LegendSwatch label="Override" background="rgba(25, 118, 210, 0.12)" border="rgba(25, 118, 210, 0.5)" />
                        <Box component="span" sx={{ display: "inline-flex", alignItems: "center", gap: "5px", color: "#2e7d32", fontSize: 12 }}>
                            <CloudDoneOutlinedIcon sx={{ fontSize: 16 }} />
                            Changes are saved automatically
                        </Box>
                    </Box>
                }
                summary={
                    <Box sx={{ gridColumn: "1 / -1" }}>
                        <SectionCard
                            title="Default balance · global"
                            subtitle="Every SKU without an override uses it"
                            headerRight={
                                canEditDefault ? (
                                    <SecondaryBoButton
                                        size="small"
                                        startIcon={<SettingsOutlinedIcon />}
                                        onClick={() => history.push(FACTORY_SHARE_SETTINGS_URL)}>
                                        Edit default balance
                                    </SecondaryBoButton>
                                ) : (
                                    <LockTag text="Factory catalog admin edits" />
                                )
                            }>
                            <Box sx={{ display: "flex", alignItems: "center", gap: 3, flexWrap: "wrap" }}>
                                {(globalShare?.factoryShares ?? []).map(share => (
                                    <Box key={share.factoryId} component="span" sx={{ fontSize: 14 }}>
                                        {share.factoryId}: <b>{formatPct(share.pct)}</b>
                                    </Box>
                                ))}
                                {globalShare && <ShareTotalCheck totalPct={globalShare.totalPct} />}
                            </Box>
                        </SectionCard>
                    </Box>
                }
                toolbarActions={context => {
                    reloadList.current = context.reload;
                    return (
                        <PermissionGate allowed={can(Permissions.Skus.EditProducible)}>
                            <SecondaryBoButton startIcon={<UploadFileIcon />} onClick={() => setIsImportOpen(true)}>
                                Import producible
                            </SecondaryBoButton>
                        </PermissionGate>
                    );
                }}
                selectable={bulkFactoryIds.length > 0}
                bulkActions={(selectedIds, { clearSelection }) =>
                    bulkFactoryIds.map(factoryId => (
                        <React.Fragment key={factoryId}>
                            <SecondaryBoButton size="small" onClick={() => bulkSetProducible(factoryId, selectedIds, true, clearSelection)}>
                                Producible at {factoryId}
                            </SecondaryBoButton>
                            <SecondaryBoButton size="small" onClick={() => bulkSetProducible(factoryId, selectedIds, false, clearSelection)}>
                                Not producible at {factoryId}
                            </SecondaryBoButton>
                        </React.Fragment>
                    ))
                }
            />
            <ExcelImportDialog
                open={isImportOpen}
                title="Import producible SKUs"
                description="Upload an .xlsx file with columns SKU (UUID, alias or Site SKU), Factory ID and Producible (Yes / No). Only rows of your own factory are accepted."
                onClose={() => setIsImportOpen(false)}
                onValidate={file => skuService.validateProducibleImport(file)}
                onConfirm={file => skuService.importProducible(file).then(refresh)}
            />
        </>
    );
};

export default FactorySharePage;
