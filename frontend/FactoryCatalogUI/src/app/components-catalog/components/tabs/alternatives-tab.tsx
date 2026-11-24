import React from "react";
import { useDispatch } from "react-redux";
import { Link as RouterLink } from "react-router-dom";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Link from "@mui/material/Link";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import SectionCard from "../../../../components/section-card";
import LockTag from "../../../../components/lock-tag";
import GatedCheckbox from "../../../../components/gated-checkbox";
import { StatusChip, statusColors } from "../../../../components/status-chips";
import { tableStyles } from "../../../../components/data-table";
import { COMPONENTS_URL } from "../../../../constants/urls";
import PermissionGate from "../../../../core/auth/permission-gate";
import { Permissions, usePermissions } from "../../../../core/auth/permissions";
import { useFactories } from "../../../../store/selectors/factoriesSelector";
import { addAlternative, removeAlternative, setAlternativeUse } from "../../store/actions";
import ComponentStockStatus from "../component-stock-status";
import AlternativePicker from "./alternative-picker";
import { ApiStockStatuses, ComponentAlternative, ComponentDetails, ComponentStatuses } from "../../entities/component";
import { formatQuantity, getFactoryCode } from "../../utils/format";

interface AlternativesTabProps {
    component: ComponentDetails;
}

const ROW_HEIGHT = 72;
const ERP_ID_WIDTH = 220;
const STOCK_WIDTH = 150;
const USE_WIDTH = 190;
const ACTIONS_WIDTH = 64;
const DENIED_MESSAGE = "Supply chain manager edits";

const AlternativesTab: React.FC<AlternativesTabProps> = ({ component }) => {
    const dispatch = useDispatch();
    const { can } = usePermissions();
    const { factories } = useFactories();
    const canEdit = can(Permissions.Components.EditAlternatives);
    const activeFactories = factories.filter(factory => factory.isActive);

    const onUseChange = (alternative: ComponentAlternative, forBalancer: boolean, forPlanner: boolean) =>
        dispatch(setAlternativeUse(component.erpId, alternative.erpId, forBalancer, forPlanner) as any);

    return (
        <SectionCard title="Approved alternatives" headerRight={<LockTag text={DENIED_MESSAGE} />} noPadding>
            <TableContainer>
                <Table sx={{ tableLayout: "fixed", minWidth: 900 }}>
                    <TableHead>
                        <TableRow>
                            <TableCell style={{ width: ERP_ID_WIDTH }} sx={tableStyles.headCell}>
                                ERP ID
                            </TableCell>
                            <TableCell sx={tableStyles.headCell}>Name</TableCell>
                            {activeFactories.map(factory => (
                                <TableCell key={factory.id} style={{ width: STOCK_WIDTH }} sx={tableStyles.headCell}>
                                    {getFactoryCode(factories, factory.id)} stock
                                </TableCell>
                            ))}
                            <TableCell style={{ width: USE_WIDTH }} sx={tableStyles.headCell}>
                                Use
                            </TableCell>
                            <TableCell style={{ width: ACTIONS_WIDTH }} sx={tableStyles.headCell} />
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {component.alternatives.length === 0 && (
                            <TableRow>
                                <TableCell colSpan={activeFactories.length + 4} align="center" sx={tableStyles.emptyCell}>
                                    No approved alternatives.
                                </TableCell>
                            </TableRow>
                        )}
                        {component.alternatives.map(alternative => (
                            <TableRow key={alternative.erpId} sx={{ height: ROW_HEIGHT }}>
                                <TableCell sx={{ ...tableStyles.bodyCell, ...tableStyles.monospace }}>
                                    <Link
                                        component={RouterLink}
                                        to={`${COMPONENTS_URL}/${encodeURIComponent(alternative.erpId)}`}
                                        underline="hover">
                                        {alternative.erpId}
                                    </Link>
                                </TableCell>
                                <TableCell sx={{ ...tableStyles.bodyCell, fontWeight: 500 }}>
                                    {alternative.name}
                                    {alternative.status === ComponentStatuses.Inactive && (
                                        <Box component="span" sx={{ marginLeft: "8px" }}>
                                            <StatusChip label="Inactive in SAP" colors={statusColors.neutral} small />
                                        </Box>
                                    )}
                                </TableCell>
                                {activeFactories.map(factory => {
                                    const stock = alternative.factories.find(x => x.factoryId === factory.id);
                                    return (
                                        <TableCell key={factory.id} sx={tableStyles.bodyCell}>
                                            <Box sx={{ display: "inline-flex", alignItems: "center", gap: "6px" }}>
                                                <Box component="span" sx={tableStyles.monospace}>
                                                    {stock ? formatQuantity(stock.totalStock) : "—"}
                                                </Box>
                                                {stock && stock.status !== ApiStockStatuses.Ok && (
                                                    <ComponentStockStatus status={stock.status} small />
                                                )}
                                            </Box>
                                        </TableCell>
                                    );
                                })}
                                <TableCell sx={tableStyles.bodyCell}>
                                    <Box sx={{ display: "flex", flexDirection: "column" }}>
                                        <GatedCheckbox
                                            label="For balancer"
                                            allowed={canEdit}
                                            checked={alternative.forBalancer}
                                            onChange={checked => onUseChange(alternative, checked, alternative.forPlanner)}
                                            deniedMessage={DENIED_MESSAGE}
                                        />
                                        <GatedCheckbox
                                            label="For S.C. planner"
                                            allowed={canEdit}
                                            checked={alternative.forPlanner}
                                            onChange={checked => onUseChange(alternative, alternative.forBalancer, checked)}
                                            deniedMessage={DENIED_MESSAGE}
                                        />
                                    </Box>
                                </TableCell>
                                <TableCell align="right" sx={tableStyles.bodyCell}>
                                    <PermissionGate allowed={canEdit} message={DENIED_MESSAGE}>
                                        <IconButton
                                            size="small"
                                            title="Remove alternative"
                                            onClick={() =>
                                                dispatch(removeAlternative(component.erpId, alternative.erpId) as any)
                                            }>
                                            <DeleteOutlineIcon sx={{ fontSize: 18 }} />
                                        </IconButton>
                                    </PermissionGate>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
            <AlternativePicker
                disabled={!canEdit}
                excludedErpIds={[component.erpId, ...component.alternatives.map(alternative => alternative.erpId)]}
                onAdd={alternativeErpId => dispatch(addAlternative(component.erpId, alternativeErpId) as any)}
            />
        </SectionCard>
    );
};

export default AlternativesTab;
