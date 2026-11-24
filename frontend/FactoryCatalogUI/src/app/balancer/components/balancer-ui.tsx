import React, { useMemo } from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import BoltIcon from "@mui/icons-material/Bolt";
import VisibilityOutlinedIcon from "@mui/icons-material/VisibilityOutlined";
import UndoIcon from "@mui/icons-material/Undo";
import { FactoryTag, StatusChip, statusColors } from "../../../components/status-chips";
import { useFactories } from "../../../store/selectors/factoriesSelector";
import { AllocationMode, AllocationModes } from "../entities";

const NO_VALUE = "—";

const currencySymbols: Record<string, string> = { USD: "$", EUR: "€", GBP: "£" };

export const formatMoney = (amount: number | null | undefined, currency: string | null | undefined = "USD"): string => {
    if (amount === null || amount === undefined) return NO_VALUE;
    const symbol = currencySymbols[currency ?? ""] ?? "";
    const formatted = Math.abs(amount).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const sign = amount < 0 ? "-" : "";
    return symbol ? `${sign}${symbol}${formatted}` : `${sign}${formatted} ${currency ?? ""}`.trim();
};

export const formatSignedMoney = (amount: number | null | undefined, currency: string | null | undefined): string =>
    amount === null || amount === undefined ? NO_VALUE : `${amount > 0 ? "+" : ""}${formatMoney(amount, currency)}`;

export const formatPercent = (value: number | null | undefined, digits = 1): string =>
    value === null || value === undefined ? NO_VALUE : `${Number(value.toFixed(digits))}%`;

export const deltaColor = (amount: number | null | undefined): string =>
    !amount ? "rgba(0, 0, 0, 0.6)" : amount > 0 ? "#c62828" : "#2e7d32";

export const monoSx = { fontFamily: "Roboto Mono, monospace", fontSize: 12.5 };

export const useFactoryCodes = () => {
    const { factories } = useFactories();
    return useMemo(() => {
        const codes = new Map(factories.map(factory => [String(factory.id), factory.countryCode || String(factory.id)]));
        return (factoryId: string | null | undefined) => (factoryId ? codes.get(factoryId) ?? factoryId : NO_VALUE);
    }, [factories]);
};

export const useActiveFactories = () => {
    const { factories, isLoading } = useFactories();
    const active = useMemo(() => factories.filter(factory => factory.isActive), [factories]);
    return { factories: active, isLoading };
};

export const Factory: React.FC<{ id: string | null | undefined; dimmed?: boolean }> = ({ id, dimmed }) => {
    const codeOf = useFactoryCodes();
    return id ? <FactoryTag code={codeOf(id)} dimmed={dimmed} /> : <>{NO_VALUE}</>;
};

const modeView: Record<AllocationMode, { colors: typeof statusColors.success; icon: React.ReactNode }> = {
    [AllocationModes.Live]: { colors: statusColors.success, icon: <BoltIcon sx={{ fontSize: 14 }} /> },
    [AllocationModes.Shadow]: {
        colors: { background: "rgba(81, 45, 168, 0.1)", text: "#512da8" },
        icon: <VisibilityOutlinedIcon sx={{ fontSize: 14 }} />,
    },
    [AllocationModes.Fallback]: { colors: statusColors.warning, icon: <UndoIcon sx={{ fontSize: 14 }} /> },
};

export const ModeChip: React.FC<{ mode: AllocationMode }> = ({ mode }) => {
    const view = modeView[mode] ?? modeView[AllocationModes.Live];
    return <StatusChip label={mode} colors={view.colors} icon={view.icon} small />;
};

export const PageTitle: React.FC<{ title: string; children?: React.ReactNode }> = ({ title, children }) => (
    <Box sx={{ display: "flex", alignItems: "center", gap: 1.5, flexWrap: "wrap" }}>
        <Typography component="h1" sx={{ fontSize: 22, fontWeight: 500, color: "rgba(0, 0, 0, 0.87)" }}>
            {title}
        </Typography>
        <Box sx={{ flex: 1 }} />
        {children}
    </Box>
);

export const Caption: React.FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => (
    <Box>
        <Box sx={{ fontSize: 10, textTransform: "uppercase", letterSpacing: "0.07em", color: "rgba(0, 0, 0, 0.5)" }}>
            {label}
        </Box>
        <Box sx={{ fontSize: 14 }}>{children}</Box>
    </Box>
);

export const PageTag: React.FC<{ page: string | null | undefined }> = ({ page }) =>
    page ? (
        <Box component="span" sx={{ ...monoSx, fontSize: 12, padding: "1px 6px", borderRadius: "4px", background: "rgba(0, 0, 0, 0.05)" }}>
            {page}
        </Box>
    ) : null;

export const pageSx = { padding: "24px", display: "flex", flexDirection: "column", gap: 2 } as const;
