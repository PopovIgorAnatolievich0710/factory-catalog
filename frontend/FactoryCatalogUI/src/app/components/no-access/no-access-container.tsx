import React from "react";
import Alert from "@mui/material/Alert";
import AlertTitle from "@mui/material/AlertTitle";

const NoAccessContainer = () => {
    return (
        <Alert severity="warning" sx={{ margin: "30px" }}>
            <AlertTitle>No access</AlertTitle>
            Your account has no Factory Catalog role. Ask the factory catalog admin to add you to one of the access groups.
        </Alert>
    );
};

export default NoAccessContainer;
