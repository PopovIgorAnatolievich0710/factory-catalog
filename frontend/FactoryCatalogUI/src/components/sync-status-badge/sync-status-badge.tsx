import React, { useCallback, useEffect, useRef, useState } from "react";
import { useDispatch } from "react-redux";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Tooltip from "@mui/material/Tooltip";
import ScheduleIcon from "@mui/icons-material/Schedule";
import SyncIcon from "@mui/icons-material/Sync";
import { SecondaryBoButton } from "../../../bo-frontend/src/components/Buttons/SecondaryBoButton";
import { StatusChip, statusColors } from "../status-chips";
import syncService from "../../services/sync-service";
import { Api, catalogApi } from "../../helpers/api-helper";
import { getApiErrorStatus } from "../../helpers/api-error";
import { useErrorNotification } from "../../helpers/response-helper";
import { notificationWarning } from "../../app/notifications/actions";
import { SyncRunStatuses, SyncStatus } from "../../entities/sync-status";
import { CONSTANTS } from "../../core/CONSTANTS";
import { formatDateTime } from "../../utils/format-date";

const POLL_INTERVAL_MS = 3000;

export interface SyncStatusBadgeProps {
    syncType: string;
    label: string;
    api?: Api;
    canTrigger?: boolean;
    onSyncFinished?: (status: SyncStatus) => void;
}

const SyncStatusBadge: React.FC<SyncStatusBadgeProps> = ({
    syncType,
    label,
    api = catalogApi,
    canTrigger = false,
    onSyncFinished,
}) => {
    const dispatch = useDispatch();
    const notifyError = useErrorNotification();

    const [status, setStatus] = useState<SyncStatus | null>(null);
    const [isStarting, setIsStarting] = useState(false);
    const [finishedRun, setFinishedRun] = useState<SyncStatus | null>(null);
    const wasRunning = useRef(false);

    const refresh = useCallback(
        () => syncService.fetchStatus(syncType, api).then(setStatus).catch(notifyError),
        [syncType, api, notifyError],
    );

    useEffect(() => {
        wasRunning.current = false;
        setFinishedRun(null);
        refresh();
    }, [refresh]);

    const isRunning = status?.status === SyncRunStatuses.Running;

    useEffect(() => {
        if (isRunning) {
            wasRunning.current = true;
            const timer = window.setTimeout(refresh, POLL_INTERVAL_MS);
            return () => window.clearTimeout(timer);
        }
        if (wasRunning.current && status) {
            wasRunning.current = false;
            setFinishedRun(status);
            onSyncFinished?.(status);
        }
        return undefined;
    }, [status]);

    const onSyncNowClick = async () => {
        setIsStarting(true);
        setFinishedRun(null);
        try {
            await syncService.run(syncType, api);
        } catch (error) {
            if (getApiErrorStatus(error) === CONSTANTS.ERROR_CODES.CONFLICT) {
                dispatch(notificationWarning("A sync is already running."));
            } else {
                notifyError(error);
            }
        } finally {
            await refresh();
            setIsStarting(false);
        }
    };

    const isFailed = status?.status === SyncRunStatuses.Failed;

    return (
        <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1.25, flexWrap: "wrap" }}>
            <Box
                component="span"
                sx={{ display: "inline-flex", alignItems: "center", gap: "6px", fontSize: 12, color: "rgba(0, 0, 0, 0.6)" }}>
                <ScheduleIcon sx={{ fontSize: 15 }} />
                {label}:
                <Box component="b" sx={{ fontWeight: 500, color: "rgba(0, 0, 0, 0.8)" }}>
                    {status?.lastSuccessAt ? formatDateTime(status.lastSuccessAt) : "never"}
                </Box>
            </Box>
            {isRunning && (
                <StatusChip
                    label="Sync running"
                    colors={statusColors.info}
                    icon={<CircularProgress size={12} color="inherit" />}
                />
            )}
            {isFailed && (
                <Tooltip title={status.error ?? "The last sync run failed"}>
                    <span>
                        <StatusChip
                            label={`Last run failed · ${formatDateTime(status.finishedAt)}`}
                            colors={statusColors.danger}
                        />
                    </span>
                </Tooltip>
            )}
            {finishedRun?.status === SyncRunStatuses.Succeeded && (
                <StatusChip
                    label={`Added ${finishedRun.added} · Updated ${finishedRun.updated} · Failed ${finishedRun.failed}`}
                    colors={finishedRun.failed > 0 ? statusColors.warning : statusColors.success}
                />
            )}
            {canTrigger && (
                <SecondaryBoButton
                    startIcon={<SyncIcon />}
                    disabled={isRunning || isStarting || status === null}
                    onClick={onSyncNowClick}>
                    Sync now
                </SecondaryBoButton>
            )}
        </Box>
    );
};

export default SyncStatusBadge;
