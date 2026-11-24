import React from "react";
import Alert from "@mui/material/Alert";
import AlertTitle from "@mui/material/AlertTitle";
import Box from "@mui/material/Box";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import SpecificationsTable, { SpecificationRow } from "../../../components/specifications-table";
import { tableStyles } from "../../../components/data-table";
import { formatDateTime } from "../../../utils/format-date";
import { MutedText } from "../components/sku-chips";
import { SkuDetails, SkuValidationStatuses } from "../entities";

const SkuOverviewTab: React.FC<{ sku: SkuDetails }> = ({ sku }) => {
    const isValid = sku.validation.status === SkuValidationStatuses.Valid;
    const issueCount = sku.validation.issues.length;

    const rows: SpecificationRow[] = [
        { label: "Name", value: sku.name },
        { label: "UUID", value: <Box component="span" sx={tableStyles.monospace}>{sku.skuUuid}</Box> },
        { label: "Site SKU", value: <Box component="span" sx={tableStyles.monospace}>{sku.siteSku}</Box> },
        {
            label: "Factory SKU",
            value:
                sku.aliases.length === 0 ? (
                    <MutedText>None</MutedText>
                ) : (
                    <Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
                        {sku.aliases.map(alias => (
                            <Box
                                key={alias.id}
                                component="span"
                                sx={{
                                    display: "inline-flex",
                                    alignItems: "center",
                                    gap: 1,
                                    padding: "2px 8px",
                                    borderRadius: "5px",
                                    border: "1px solid rgba(0, 0, 0, 0.1)",
                                }}>
                                <Box
                                    component="span"
                                    sx={{
                                        ...tableStyles.monospace,
                                        color: alias.deprecated ? "rgba(0, 0, 0, 0.45)" : "inherit",
                                        textDecoration: alias.deprecated ? "line-through" : "none",
                                    }}>
                                    {alias.id}
                                </Box>
                                {alias.deprecated && (
                                    <Box
                                        component="span"
                                        sx={{
                                            fontSize: 10,
                                            padding: "1px 6px",
                                            borderRadius: "3px",
                                            background: "rgba(0, 0, 0, 0.07)",
                                            color: "rgba(0, 0, 0, 0.55)",
                                            textTransform: "uppercase",
                                            letterSpacing: "0.04em",
                                        }}>
                                        Deprecated alias
                                    </Box>
                                )}
                            </Box>
                        ))}
                    </Box>
                ),
        },
        { label: "Type", value: sku.productType },
        { label: "Status", value: sku.status },
    ];
    if (sku.dropshipper) {
        rows.push(
            { label: "Dropshipper", value: [sku.dropshipper.name, sku.dropshipper.dropshipperId].filter(Boolean).join(" · ") },
            {
                label: "Total cost",
                value: sku.dropshipper.totalCost
                    ? `${sku.dropshipper.totalCost.amount.toFixed(2)} ${sku.dropshipper.totalCost.currency ?? ""}`
                    : "-",
            },
        );
    }
    rows.push({ label: "Last master catalog sync", value: formatDateTime(sku.lastMasterSyncAt) });

    return (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <Alert severity={isValid ? "success" : "error"}>
                <AlertTitle sx={{ marginBottom: isValid ? 0 : undefined }}>
                    {isValid
                        ? sku.dropshipper
                            ? "Valid - dropshipper product, excluded from factory routing"
                            : "Valid - ready for routing"
                        : `Invalid - ${issueCount} issue${issueCount === 1 ? "" : "s"}, excluded from quotes`}
                </AlertTitle>
                {!isValid && (
                    <>
                        <Box component="ul" sx={{ margin: "4px 0", paddingLeft: "18px" }}>
                            {sku.validation.issues.map(issue => (
                                <li key={issue}>{issue}</li>
                            ))}
                        </Box>
                        Fix the source data in the master catalog, SAP or the process mapping; the next sync or import
                        re-validates.
                    </>
                )}
            </Alert>
            <SectionCard title="Overview" headerRight={<LockTag text="Read only · Source: Master catalog" />} noPadding>
                <SpecificationsTable rows={rows} />
            </SectionCard>
        </Box>
    );
};

export default SkuOverviewTab;
