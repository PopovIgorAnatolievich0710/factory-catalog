import React, { useMemo, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import SectionCard from "../../../../components/section-card";
import LockTag from "../../../../components/lock-tag";
import FactoryGrid from "../../../../components/factory-grid";
import GatedCheckbox from "../../../../components/gated-checkbox";
import { tableStyles } from "../../../../components/data-table";
import { Permissions, usePermissions } from "../../../../core/auth/permissions";
import { useFactories } from "../../../../store/selectors/factoriesSelector";
import { Factory } from "../../../../entities/factory";
import { formatDateTime } from "../../../../utils/format-date";
import { setManualOos } from "../../store/actions";
import ComponentStockStatus from "../component-stock-status";
import LevelSourceTag from "../level-source-tag";
import OosNoteCell from "./oos-note-cell";
import { ComponentDetails, ComponentFactoryStock } from "../../entities/component";
import { formatQuantity, getFactoryCode } from "../../utils/format";

interface StockByFactoryTabProps {
    component: ComponentDetails;
}

const NOT_STOCKED = "—";

const oosCheckboxColors = {
    "& .MuiCheckbox-root.Mui-checked": { color: "#d32f2f" },
    "& .MuiCheckbox-root.Mui-checked.Mui-disabled": { color: "rgba(211, 47, 47, 0.45)" },
};

const StockByFactoryTab: React.FC<StockByFactoryTabProps> = ({ component }) => {
    const dispatch = useDispatch();
    const { canInFactory } = usePermissions();
    const { factories } = useFactories();
    const [editingFactoryId, setEditingFactoryId] = useState<string | null>(null);

    const stockByFactoryId = useMemo(
        () => new Map(component.factories.map(stock => [stock.factoryId, stock] as [string, ComponentFactoryStock])),
        [component.factories],
    );
    const rows = useMemo(
        () => factories.filter(factory => factory.isActive || stockByFactoryId.has(factory.id)),
        [factories, stockByFactoryId],
    );

    const canEdit = (factory: Factory) => canInFactory(Permissions.Components.EditManualOos, factory.id);
    const code = (factory: Factory) => getFactoryCode(factories, factory.id);

    const onToggle = async (factory: Factory, active: boolean) => {
        const saved = await dispatch(
            setManualOos(
                component.erpId,
                factory.id,
                active,
                null,
                active
                    ? `${component.erpId} marked out of stock at ${code(factory)}`
                    : `Out-of-stock flag cleared at ${code(factory)}`,
            ) as any,
        );
        setEditingFactoryId(saved && active ? factory.id : null);
    };

    const onSaveNote = async (factory: Factory, note: string | null) => {
        const saved = await dispatch(setManualOos(component.erpId, factory.id, true, note, "Note saved") as any);
        if (saved) setEditingFactoryId(null);
    };

    const quantityColumn = (key: string, label: string, getValue: (stock: ComponentFactoryStock) => number, bold = false) => ({
        key,
        label,
        width: 110,
        align: "right" as const,
        render: (factory: Factory) => {
            const stock = stockByFactoryId.get(factory.id);
            return (
                <Box component="span" sx={{ ...tableStyles.monospace, fontWeight: bold ? 600 : 400 }}>
                    {stock ? formatQuantity(getValue(stock)) : NOT_STOCKED}
                </Box>
            );
        },
    });

    return (
        <SectionCard
            title="Stock by factory"
            subtitle={`Last SAP sync: ${formatDateTime(component.lastSapSyncAt)}`}
            headerRight={<LockTag text="Source: SAP. Factory manager edits." />}
            noPadding>
            <FactoryGrid
                factories={rows}
                isRowHighlighted={canEdit}
                columns={[
                    quantityColumn("warehouse", "Warehouse", stock => stock.warehouseStock),
                    quantityColumn("floor", "Floor", stock => stock.productionFloorStock),
                    quantityColumn("total", "Total", stock => stock.totalStock, true),
                    {
                        key: "criticalLevel",
                        label: "Critical level",
                        width: 170,
                        render: factory => {
                            const stock = stockByFactoryId.get(factory.id);
                            const level = stock ? stock.criticalLevel : component.criticalStockLevel;
                            return (
                                <>
                                    <Box component="span" sx={tableStyles.monospace}>
                                        {formatQuantity(level)}
                                    </Box>
                                    {level !== null && (
                                        <LevelSourceTag isOverride={(stock?.criticalLevelOverride ?? null) !== null} />
                                    )}
                                </>
                            );
                        },
                    },
                    {
                        key: "status",
                        label: "Status",
                        width: 140,
                        render: factory => {
                            const stock = stockByFactoryId.get(factory.id);
                            return stock ? <ComponentStockStatus status={stock.status} /> : NOT_STOCKED;
                        },
                    },
                    {
                        key: "oosFlag",
                        label: "OOS flag",
                        width: 110,
                        render: factory => (
                            <Box sx={oosCheckboxColors}>
                                <GatedCheckbox
                                    label="OOS"
                                    allowed={canEdit(factory)}
                                    checked={stockByFactoryId.get(factory.id)?.manualOos.active ?? false}
                                    onChange={active => onToggle(factory, active)}
                                    deniedMessage="Only this factory's manager can change it"
                                />
                            </Box>
                        ),
                    },
                    {
                        key: "note",
                        label: "Note",
                        render: factory => (
                            <OosNoteCell
                                manualOos={stockByFactoryId.get(factory.id)?.manualOos ?? null}
                                canEdit={canEdit(factory)}
                                isEditing={editingFactoryId === factory.id}
                                onStartEditing={() => setEditingFactoryId(factory.id)}
                                onSave={note => onSaveNote(factory, note)}
                            />
                        ),
                    },
                ]}
            />
        </SectionCard>
    );
};

export default StockByFactoryTab;
