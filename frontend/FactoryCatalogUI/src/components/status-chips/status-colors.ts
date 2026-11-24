import { StatusChipColors } from "./status-chip";

export const statusColors: Record<"success" | "warning" | "danger" | "dangerSolid" | "conflict" | "info" | "neutral", StatusChipColors> = {
    success: { background: "rgba(46, 125, 50, 0.12)", text: "#1b5e20" },
    warning: { background: "rgba(237, 108, 2, 0.14)", text: "#b45309" },
    danger: { background: "rgba(211, 47, 47, 0.12)", text: "#c62828" },
    dangerSolid: { background: "#d32f2f", text: "#ffffff" },
    conflict: { background: "#ffffff", text: "#c62828", border: "1px dashed #d32f2f" },
    info: { background: "rgba(25, 118, 210, 0.1)", text: "#1565c0" },
    neutral: { background: "rgba(0, 0, 0, 0.06)", text: "rgba(0, 0, 0, 0.6)" },
};
