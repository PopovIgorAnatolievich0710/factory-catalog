import React from "react";
import { useDispatch } from "react-redux";
import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import LockTag from "../../../components/lock-tag";
import SectionCard from "../../../components/section-card";
import SpecificationsTable from "../../../components/specifications-table";
import { Permissions, usePermissions } from "../../../core/auth/permissions";
import CommitTextField from "./commit-text-field";
import { ProcessStatusChip, ProcessTypeChip } from "./process-chips";
import { saveProcessSpecification } from "../store/actions";
import {
    getUnitLabel,
    ProcessDetails,
    ProcessSpecification,
    ProcessStatuses,
    ProcessTypes,
    processUnits,
} from "../entities";

const selectStyle = { minWidth: 180 };

const ProcessSpecificationsTab: React.FC<{ process: ProcessDetails }> = ({ process }) => {
    const dispatch = useDispatch();
    const { can } = usePermissions();
    const canEdit = can(Permissions.Processes.Edit);

    const save = (patch: Partial<ProcessSpecification>) => {
        dispatch(
            saveProcessSpecification(process.processId, {
                name: process.name,
                type: process.type,
                measuredIn: process.measuredIn,
                status: process.status,
                description: process.description,
                ...patch,
            }) as any,
        );
    };

    const select = (field: keyof ProcessSpecification, value: string, options: { value: string; label: string }[]) => (
        <TextField
            select
            size="small"
            value={value}
            onChange={event => save({ [field]: event.target.value })}
            sx={selectStyle}>
            {options.map(option => (
                <MenuItem key={option.value} value={option.value}>
                    {option.label}
                </MenuItem>
            ))}
        </TextField>
    );

    const rows = canEdit
        ? [
              {
                  label: "Name",
                  value: (
                      <CommitTextField
                          value={process.name}
                          onCommit={name => name.length > 0 && save({ name })}
                          sx={{ width: "100%", maxWidth: 380 }}
                      />
                  ),
              },
              {
                  label: "Type",
                  value: select("type", process.type, [
                      { value: ProcessTypes.Machine, label: "Machine" },
                      { value: ProcessTypes.Work, label: "Work" },
                  ]),
              },
              { label: "Measured in", value: select("measuredIn", process.measuredIn, processUnits) },
              {
                  label: "Status",
                  value: select("status", process.status, [
                      { value: ProcessStatuses.Active, label: "Active" },
                      { value: ProcessStatuses.Inactive, label: "Inactive" },
                  ]),
              },
              {
                  label: "Description",
                  value: (
                      <CommitTextField
                          multiline
                          minRows={3}
                          value={process.description ?? ""}
                          onCommit={description => save({ description: description || null })}
                          sx={{ width: "100%", maxWidth: 520 }}
                      />
                  ),
              },
          ]
        : [
              { label: "Name", value: process.name },
              { label: "Type", value: <ProcessTypeChip type={process.type} /> },
              { label: "Measured in", value: getUnitLabel(process.measuredIn) },
              { label: "Status", value: <ProcessStatusChip status={process.status} /> },
              { label: "Description", value: process.description || "-" },
          ];

    return (
        <SectionCard
            title="Specifications"
            headerRight={!canEdit && <LockTag text="Factory catalog admin edits" />}
            noPadding>
            <SpecificationsTable rows={rows} />
        </SectionCard>
    );
};

export default ProcessSpecificationsTab;
