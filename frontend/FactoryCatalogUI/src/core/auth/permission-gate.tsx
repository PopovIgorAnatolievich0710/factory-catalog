import React from "react";
import { Tooltip } from "@mui/material";

interface PermissionGateProps {
    allowed: boolean;
    message?: string;
    children: React.ReactElement;
}

const PermissionGate: React.FC<PermissionGateProps> = ({
    allowed,
    message = "You don't have permission to perform this action",
    children,
}) => {
    if (allowed) return children;
    return (
        <Tooltip title={message}>
            <span style={{ display: "inline-flex" }}>
                {React.cloneElement(children, { disabled: true })}
            </span>
        </Tooltip>
    );
};

export default PermissionGate;
