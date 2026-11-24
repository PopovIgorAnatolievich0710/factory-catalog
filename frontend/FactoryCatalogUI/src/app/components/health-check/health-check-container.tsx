import React from "react";
import { makeStyles } from '@mui/styles';

const useStyles = makeStyles(() => ({
    root: {
        width: "100%",
        marginLeft: 30,
        marginTop: 30
    }
}));

const HealthCheckContainer = () => {
    const createClasses = useStyles();
    return (
        <div className={createClasses.root}>
            I'm ok
        </div>
    );
};

export default HealthCheckContainer;
