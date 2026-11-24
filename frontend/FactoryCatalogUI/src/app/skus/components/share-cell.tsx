import React, { useState } from "react";
import Box from "@mui/material/Box";
import Tooltip from "@mui/material/Tooltip";
import GatedCheckbox from "../../../components/gated-checkbox";
import { formatPct, FULL_SHARE_PCT, SkuFactoryShare, SkuShareSources } from "../entities";

const OVERRIDE_COLOR = "#1565c0";
const OVERRIDE_BACKGROUND = "rgba(25, 118, 210, 0.08)";
const OVERRIDE_BORDER = "rgba(25, 118, 210, 0.5)";
const DEFAULT_COLOR = "rgba(0, 0, 0, 0.5)";
const DEFAULT_BACKGROUND = "#fafafa";
const DEFAULT_BORDER = "rgba(0, 0, 0, 0.2)";
const VALUE_WIDTH = 68;

interface ShareCellProps {
    share: SkuFactoryShare;
    canEditProducible: boolean;
    canEditShare: boolean;
    centered?: boolean;
    onProducibleChange: (producible: boolean) => void;
    onShareChange: (sharePct: number | null) => void;
}

const sourceHint = (share: SkuFactoryShare): string => {
    if (share.source === SkuShareSources.Override) return `Override${share.overrideBy ? ` by ${share.overrideBy}` : ""}`;
    if (share.source === SkuShareSources.Rebalanced) return "Rebalanced after another factory's override";
    return "Default balance";
};

const ShareCell: React.FC<ShareCellProps> = ({
    share,
    canEditProducible,
    canEditShare,
    centered = false,
    onProducibleChange,
    onShareChange,
}) => {
    const [draft, setDraft] = useState<string | null>(null);
    const isOverride = share.source === SkuShareSources.Override;

    const commit = () => {
        const raw = draft;
        setDraft(null);
        if (raw === null) return;
        if (raw.trim() === "") {
            if (isOverride) onShareChange(null);
            return;
        }
        const value = Number.parseFloat(raw);
        if (Number.isNaN(value) || value < 0 || value > FULL_SHARE_PCT || value === share.effectiveSharePct) return;
        onShareChange(value);
    };

    const valueStyle = {
        width: VALUE_WIDTH,
        height: 30,
        boxSizing: "border-box" as const,
        padding: "0 8px",
        borderRadius: 4,
        textAlign: "right" as const,
        fontWeight: 500,
        fontSize: 14,
        fontFamily: "inherit",
        color: isOverride ? OVERRIDE_COLOR : DEFAULT_COLOR,
    };

    return (
        <Box
            onClick={event => event.stopPropagation()}
            sx={{ display: "flex", alignItems: "center", gap: 1, justifyContent: centered ? "center" : "flex-start", height: 32 }}>
            <GatedCheckbox
                allowed={canEditProducible}
                checked={share.producible}
                onChange={onProducibleChange}
                deniedMessage="Factory managers edit their own factory"
            />
            {!share.producible && (
                <Box component="span" sx={{ width: VALUE_WIDTH, textAlign: "right", color: "rgba(0, 0, 0, 0.3)" }}>
                    -
                </Box>
            )}
            {share.producible && canEditShare && (
                <Tooltip title={sourceHint(share)}>
                    <input
                        type="number"
                        min={0}
                        max={FULL_SHARE_PCT}
                        step="any"
                        aria-label={`Share of factory ${share.factoryId}`}
                        value={draft ?? String(share.effectiveSharePct)}
                        onChange={event => setDraft(event.target.value)}
                        onBlur={commit}
                        onKeyDown={event => {
                            if (event.key === "Enter") event.currentTarget.blur();
                        }}
                        style={{
                            ...valueStyle,
                            background: isOverride ? OVERRIDE_BACKGROUND : DEFAULT_BACKGROUND,
                            border: `1px solid ${isOverride ? OVERRIDE_BORDER : DEFAULT_BORDER}`,
                        }}
                    />
                </Tooltip>
            )}
            {share.producible && !canEditShare && (
                <Tooltip title={sourceHint(share)}>
                    <Box
                        component="span"
                        sx={{
                            ...valueStyle,
                            display: "inline-flex",
                            alignItems: "center",
                            justifyContent: "flex-end",
                            background: isOverride ? OVERRIDE_BACKGROUND : "transparent",
                        }}>
                        {formatPct(share.effectiveSharePct)}
                    </Box>
                </Tooltip>
            )}
        </Box>
    );
};

export default ShareCell;
