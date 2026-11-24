envsubst '${FACTORY_CATALOG_BASE_FRONTEND_URL_ENV_VAR} ${FACTORY_CATALOG_BASE_API_URL_ENV_VAR} ${BALANCER_BASE_API_URL_ENV_VAR} ${AZURE_CLIENT_ID_ENV_VAR} ${AZURE_BACKEND_SCOPE_ENV_VAR} ${AZURE_TENANT_ID_ENV_VAR} ${SERVICES_CONFIG_URL_ENV_VAR}' < ./index.html > ./index.production.html
cp ./index.production.html ./index.html
nginx -g "daemon off;"
