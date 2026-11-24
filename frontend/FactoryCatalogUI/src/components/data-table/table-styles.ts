export const tableStyles = {
    rowHeight: 48,
    headCell: {
        padding: "10px 14px",
        borderBottom: "1px solid rgba(0, 0, 0, 0.12)",
        fontWeight: 500,
        fontSize: 12,
        color: "rgba(0, 0, 0, 0.6)",
        textTransform: "uppercase",
        letterSpacing: "0.04em",
        whiteSpace: "nowrap",
        background: "#fafafa",
    },
    bodyCell: {
        padding: "10px 14px",
        borderBottom: "1px solid rgba(0, 0, 0, 0.06)",
        verticalAlign: "middle",
        color: "rgba(0, 0, 0, 0.87)",
        fontSize: 14,
    },
    emptyCell: {
        padding: "24px",
        color: "rgba(0, 0, 0, 0.5)",
        fontSize: 14,
    },
    monospace: {
        fontFamily: "Roboto Mono, monospace",
        fontSize: 12.5,
    },
    actionRequiredCell: {
        borderLeft: "3px solid #d32f2f",
    },
    footer: {
        padding: "8px 16px",
        fontSize: 13,
        color: "rgba(0, 0, 0, 0.6)",
        display: "flex",
        justifyContent: "flex-end",
        alignItems: "center",
        gap: "14px",
    },
} as const;
