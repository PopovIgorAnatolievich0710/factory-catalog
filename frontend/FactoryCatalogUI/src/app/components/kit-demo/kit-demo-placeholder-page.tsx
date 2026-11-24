import React from "react";
import Box from "@mui/material/Box";
import Typography from "@mui/material/Typography";
import SectionCard from "../../../components/section-card";
import LockTag from "../../../components/lock-tag";

interface KitDemoPlaceholderPageProps {
    title: string;
}

const KitDemoPlaceholderPage: React.FC<KitDemoPlaceholderPageProps> = ({ title }) => (
    <Box sx={{ padding: "24px", display: "flex", flexDirection: "column", gap: 2 }}>
        <Typography component="h1" sx={{ fontSize: 22, fontWeight: 500 }}>
            {title}
        </Typography>
        <SectionCard title={title} subtitle="Placeholder page" headerRight={<LockTag text="Not implemented yet" />}>
            This page is delivered by its module.
        </SectionCard>
    </Box>
);

export default KitDemoPlaceholderPage;
