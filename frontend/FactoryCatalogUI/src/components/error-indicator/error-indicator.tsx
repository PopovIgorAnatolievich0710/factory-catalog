import React from "react";
import Alert from "@mui/material/Alert";

const ErrorIndicator = () => {
    return (
        <Alert severity="error" sx={{ margin: "30px" }}>
            Something went wrong. Reload the page and try again.
        </Alert>
    );
};

export default ErrorIndicator;
