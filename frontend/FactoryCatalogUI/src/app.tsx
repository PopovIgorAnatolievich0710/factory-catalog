import React, { useEffect } from 'react';
import CssBaseline from '@mui/material/CssBaseline';
import { useDispatch } from 'react-redux';
import { MsalProvider } from "@azure/msal-react"
import AppRoutes from './router';
import AppLayout from './components/app-layout/app-layout';
import { useMsalService } from "../bo-frontend/src/hooks/useMsalHook"
import ErrorBoundry from "./components/hoc/error-boundry";
import {ConnectedApp} from "./app/connectedApp";
import NoAccessContainer from "./app/components/no-access/no-access-container";
import {useRootStateSelector} from "./store/selectors/rootStateSelector";
import { PermissionProvider } from './core/auth/permissionProvider';
import { usePermissions } from './core/auth/permissions';
import { useCurrentUser } from './core/auth/current-user';
import { init } from './helpers/http-client';
import { setAuth } from './app/auth/store/actions';
import { getNavSections } from './navigation';

const HEADER_TITLE = "TG BackOffice: Factory Catalog";

const AppShell = ({ headerUser, appLoading }) => {
  const { can, isLoading } = usePermissions();
  const { hasAccess } = useCurrentUser();

  const navSections = isLoading || !hasAccess ? [] : getNavSections(item => can(item.permission));

  return (
    <AppLayout navSections={navSections} title={HEADER_TITLE} user={headerUser} appLoading={appLoading || isLoading}>
      <ErrorBoundry>
        {!isLoading && (hasAccess ? <AppRoutes /> : <NoAccessContainer />)}
        <ConnectedApp />
      </ErrorBoundry>
    </AppLayout>
  );
};

const App = () => {
  const dispatch = useDispatch();
  const msal = useMsalService()

  useEffect(() => {
    if (!msal) return;

    const initializeAuth = async () => {
      await msal.MsalInstance.handleRedirectPromise()
      if (!msal.isAuthenticated()) {
        msal.signinRedirect()
      } else {
        init(msal);
        const currentUserName = msal.getCurrentUserName();
        dispatch(setAuth(currentUserName != null, currentUserName, true));
      }
    }
    initializeAuth()
  }, [msal])

  const {userName, isInit} = useRootStateSelector(state => state.auth);
  const appLoading = useRootStateSelector(state => state.loading);

  if (!isInit) {
    return (<></>);
  }

  return (
    <MsalProvider instance={msal!.MsalInstance}>
      <CssBaseline />
      <PermissionProvider>
        <AppShell headerUser={userName} appLoading={appLoading} />
      </PermissionProvider>
    </MsalProvider>
  )
}

export default App;
