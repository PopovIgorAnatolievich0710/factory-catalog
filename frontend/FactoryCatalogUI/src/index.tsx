import React from 'react';
import { createRoot } from 'react-dom/client';
import App from './app';
import { Provider } from 'react-redux'
import {reducerMap} from './store/rootReducer';
import {configureStore} from "../bo-frontend/src/helpers/reducerFactory";

export const store = configureStore(reducerMap)

const root = createRoot(document.getElementById('root'));
root.render(
	<Provider store={store}>
		<App />
	</Provider>
);
