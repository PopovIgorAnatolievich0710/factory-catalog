import React, {forwardRef, Fragment} from 'react';
import Typography from '@mui/material/Typography';
import Container from "@mui/material/Container";
import Box from "@mui/material/Box";
import Snackbar from "@mui/material/Snackbar";
import MuiAlert, { AlertProps } from '@mui/material/Alert';
import {resetNotification} from "./notifications/actions";
import NotificationType from "../enums/notification-type";
import {useDispatch} from "react-redux";
import {useRootStateSelector} from "../store/selectors/rootStateSelector";

const Alert = forwardRef<HTMLDivElement, AlertProps>(function Alert(props, ref) {
    return <MuiAlert elevation={6} variant="filled" ref={ref} {...props} />;
});

function Copyright() {
    return (
        <Typography variant="body2" color="textSecondary" align="center">
            {'Copyright © '}
            <a target="blank" href={'https://www.tenengroup.com/' }>Tenengroup</a>
            {' '}
            {new Date().getFullYear()}
            {'.'}
        </Typography>
    );
}

export const ConnectedApp = () => {
    const dispatch = useDispatch();

    const handleNotificationClose = () => {
        dispatch(resetNotification());
    };

    const notification = useRootStateSelector(state => state.notification);

    return (
        <div>
            <main>
                <Container maxWidth="xl" >
                    <Box pt={1}>
                        <Copyright />
                    </Box>
                </Container>
            </main>

            {notification != null &&
            (<Snackbar
                open
                autoHideDuration={notification.type === NotificationType.Success? 3000 : 7000}
                onClose={handleNotificationClose}
                anchorOrigin={{vertical: 'top', horizontal: 'center'}}>
                <Alert severity={notification.type as AlertProps["severity"]}>
                    {
                        notification.message.map((row, index) => {
                            return (<Fragment key={`frg${index}`}> <span>{row}</span><br/></Fragment>);
                        })
                    }
                </Alert>
            </Snackbar>)
            }
        </div>
    );
};
