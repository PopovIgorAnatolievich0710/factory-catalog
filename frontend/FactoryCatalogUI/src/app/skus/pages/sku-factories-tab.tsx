import React from "react";
import Box from "@mui/material/Box";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import FactoryGrid from "../../../components/factory-grid";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import { StatusChip, statusColors } from "../../../components/status-chips";
import { SecondaryBoButton } from "../../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { Factory } from "../../../entities/factory";
import { MutedText } from "../components/sku-chips";
import ShareCell from "../components/share-cell";
import ShareTotalCheck from "../components/share-total-check";
import { useShareEditing } from "../components/use-share-editing";
import { formatPct, SkuDetails, SkuFactoryShare, SkuShareSources } from "../entities";

const NO_FACTORY_TEXT = "No factory";

const SourceLabel: React.FC<{ share: SkuFactoryShare }> = ({ share }) => {
    if (share.source === SkuShareSources.NotProducible) return <MutedText>Not producible</MutedText>;
    if (share.source === SkuShareSources.Override) return <StatusChip label="Override" colors={statusColors.info} small />;
    return (
        <StatusChip
            label={share.source === SkuShareSources.Rebalanced ? "Rebalanced" : "Default"}
            colors={statusColors.neutral}
            small
        />
    );
};

const SkuFactoriesTab: React.FC<{ sku: SkuDetails }> = ({ sku }) => {
    const { canEditProducible, canEditShare, changeProducible, changeShare, resettableFactories, resetToDefault } =
        useShareEditing();

    const find = (factory: Factory) => sku.factories.find(share => share.factoryId === factory.id);
    const canEditAny = sku.factories.some(share => canEditProducible(share.factoryId) || canEditShare(share.factoryId));
    const hasProducible = sku.factories.some(share => share.producible);

    return (
        <SectionCard
            title="Factories"
            subtitle="Producible on/off and share per factory. Grey = default balance, blue = override. Changes are saved automatically."
            headerRight={
                <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1.25 }}>
                    <ShareTotalCheck totalPct={sku.shareTotalPct} emptyText={hasProducible ? null : NO_FACTORY_TEXT} />
                    {canEditAny ? (
                        <SecondaryBoButton
                            size="small"
                            startIcon={<RestartAltIcon />}
                            disabled={resettableFactories(sku.factories).length === 0}
                            onClick={() => resetToDefault(sku.skuUuid, sku.factories)}>
                            Reset to default
                        </SecondaryBoButton>
                    ) : (
                        <LockTag text="Factory manager edits" />
                    )}
                </Box>
            }
            noPadding>
            <FactoryGrid
                isRowHighlighted={factory => canEditProducible(factory.id) || canEditShare(factory.id)}
                columns={[
                    {
                        key: "share",
                        label: "Producible · share %",
                        width: 220,
                        render: factory => {
                            const share = find(factory);
                            return share ? (
                                <ShareCell
                                    share={share}
                                    canEditProducible={canEditProducible(factory.id)}
                                    canEditShare={canEditShare(factory.id)}
                                    onProducibleChange={producible => changeProducible(sku.skuUuid, factory.id, producible)}
                                    onShareChange={sharePct => changeShare(sku.skuUuid, factory.id, sharePct)}
                                />
                            ) : null;
                        },
                    },
                    {
                        key: "source",
                        label: "Source",
                        width: 160,
                        render: factory => {
                            const share = find(factory);
                            return share ? <SourceLabel share={share} /> : null;
                        },
                    },
                    {
                        key: "default",
                        label: "Default balance",
                        render: factory => {
                            const share = find(factory);
                            return share ? <MutedText>{formatPct(share.defaultSharePct)}</MutedText> : null;
                        },
                    },
                ]}
            />
        </SectionCard>
    );
};

export default SkuFactoriesTab;
