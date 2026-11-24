import React from "react";
import SectionCard from "../../../../components/section-card";
import LockTag from "../../../../components/lock-tag";
import SpecificationsTable from "../../../../components/specifications-table";
import { ComponentDetails } from "../../entities/component";
import { formatQuantity } from "../../utils/format";

interface SpecificationsTabProps {
    component: ComponentDetails;
}

const joinParts = (...parts: (string | null)[]): string | null => parts.filter(Boolean).join(" ") || null;

const SpecificationsTab: React.FC<SpecificationsTabProps> = ({ component }) => {
    const { measure, unitOfMeasure, unitCost } = component;

    return (
        <SectionCard title="Specifications" headerRight={<LockTag text="Read only · Source: SAP" />} noPadding>
            <SpecificationsTable
                rows={[
                    { label: "ERP ID", value: component.erpId },
                    { label: "Name", value: component.name },
                    { label: "Family", value: component.family },
                    { label: "Material", value: component.material },
                    {
                        label: "Measure",
                        value: joinParts(measure.value === null ? null : formatQuantity(measure.value), measure.unit),
                    },
                    { label: "Measured by", value: measure.measuredBy },
                    {
                        label: "Unit of measure",
                        value: joinParts(
                            unitOfMeasure.quantity === null ? null : formatQuantity(unitOfMeasure.quantity),
                            unitOfMeasure.unit,
                            unitOfMeasure.type ? `(${unitOfMeasure.type})` : null,
                        ),
                    },
                    {
                        label: "Unit cost",
                        value:
                            unitCost && unitCost.amount !== null
                                ? joinParts(formatQuantity(unitCost.amount), unitCost.currency)
                                : null,
                    },
                ]}
            />
        </SectionCard>
    );
};

export default SpecificationsTab;
