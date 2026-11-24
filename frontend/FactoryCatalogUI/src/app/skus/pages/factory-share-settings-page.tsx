import React, { useEffect, useState } from "react";
import { useDispatch } from "react-redux";
import { Link as RouterLink } from "react-router-dom";
import Box from "@mui/material/Box";
import Link from "@mui/material/Link";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import FactoryGrid from "../../../components/factory-grid";
import HistoryTab from "../../../components/history-tab";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { PrimaryBoButton } from "../../../../bo-frontend/src/components/Buttons/PrimaryBoButton";
import { FACTORY_SHARE_URL } from "../../../constants/urls";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { useRootStateSelector } from "../../../store/selectors/rootStateSelector";
import { formatDateTime } from "../../../utils/format-date";
import ShareTotalCheck from "../components/share-total-check";
import { FULL_SHARE_PCT } from "../entities";
import { loadGlobalFactoryShare, saveGlobalFactoryShare } from "../store/actions";
import { GLOBAL_SHARE_HISTORY_ENTITY_ID, GLOBAL_SHARE_HISTORY_ENTITY_TYPE } from "../urls";

const PCT_SCALE = 100;

const FactoryShareSettingsPage: React.FC = () => {
    const dispatch = useDispatch();
    const { factories } = useFactories();
    const globalShare = useRootStateSelector(state => state.skus.globalShare);
    const [drafts, setDrafts] = useState<Record<string, string>>({});
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        dispatch(loadGlobalFactoryShare() as any);
    }, [dispatch]);

    useEffect(() => {
        setDrafts(Object.fromEntries((globalShare?.factoryShares ?? []).map(share => [share.factoryId, String(share.pct)])));
    }, [globalShare]);

    const activeFactories = factories.filter(factory => factory.isActive);
    const values = activeFactories.map(factory => ({
        factoryId: factory.id,
        pct: Number.parseFloat(drafts[factory.id] ?? "0"),
    }));
    const isValid = values.every(value => !Number.isNaN(value.pct) && value.pct >= 0 && value.pct <= FULL_SHARE_PCT);
    const totalPct = isValid ? Math.round(values.reduce((sum, value) => sum + value.pct, 0) * PCT_SCALE) / PCT_SCALE : Number.NaN;
    const isChanged = values.some(
        value => value.pct !== (globalShare?.factoryShares.find(share => share.factoryId === value.factoryId)?.pct ?? 0),
    );

    const onSaveClick = async () => {
        setIsSaving(true);
        await dispatch(saveGlobalFactoryShare(values) as any);
        setIsSaving(false);
    };

    return (
        <Box sx={{ padding: "24px", display: "flex", flexDirection: "column", gap: 2 }}>
            <Link
                component={RouterLink}
                to={FACTORY_SHARE_URL}
                underline="none"
                color="inherit"
                sx={{ display: "inline-flex", alignItems: "center", gap: "4px", fontSize: 13, color: "rgba(0, 0, 0, 0.6)" }}>
                <ArrowBackIcon sx={{ fontSize: 16 }} />
                Factory share
            </Link>
            <div>
                <Typography component="h1" sx={{ fontSize: 22, fontWeight: 500 }}>
                    Settings · Default balance
                </Typography>
                <Typography sx={{ fontSize: 13, color: "rgba(0, 0, 0, 0.6)", marginTop: "2px" }}>
                    Global share between all factories. Every SKU without an override uses it; saving updates all default
                    shares at once.
                </Typography>
            </div>
            <SectionCard
                title="Default balance"
                subtitle={
                    globalShare?.updatedAt
                        ? `Last changed by ${globalShare.updatedBy} on ${formatDateTime(globalShare.updatedAt)}`
                        : "Not set yet"
                }
                headerRight={
                    <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1.5 }}>
                        <LockTag text="Factory catalog admin edits" />
                        <ShareTotalCheck totalPct={totalPct} emptyText={isValid ? null : "Enter 0-100"} />
                        <PrimaryBoButton
                            disabled={!isValid || totalPct !== FULL_SHARE_PCT || !isChanged || isSaving}
                            onClick={onSaveClick}>
                            Save
                        </PrimaryBoButton>
                    </Box>
                }
                noPadding>
                <FactoryGrid
                    columns={[
                        {
                            key: "pct",
                            label: "Default share %",
                            render: factory => (
                                <TextField
                                    size="small"
                                    type="number"
                                    value={drafts[factory.id] ?? ""}
                                    onChange={event => setDrafts({ ...drafts, [factory.id]: event.target.value })}
                                    inputProps={{ min: 0, max: FULL_SHARE_PCT, step: "any", "aria-label": `Default share of ${factory.id}` }}
                                    sx={{ width: 120 }}
                                />
                            ),
                        },
                    ]}
                />
            </SectionCard>
            <HistoryTab entityType={GLOBAL_SHARE_HISTORY_ENTITY_TYPE} entityId={GLOBAL_SHARE_HISTORY_ENTITY_ID} />
        </Box>
    );
};

export default FactoryShareSettingsPage;
