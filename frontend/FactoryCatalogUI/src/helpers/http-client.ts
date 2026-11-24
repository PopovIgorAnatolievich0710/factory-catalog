import axios, { AxiosInstance } from "axios";
import { MsalAuthService } from "../../bo-frontend/src/utils/msal-auth-service";
import { CONSTANTS } from "../core/CONSTANTS";

const auth: { service: MsalAuthService | null } = {
    service: null,
};

export const init = (msalService: MsalAuthService) => {
    auth.service = msalService;
};

const createHttpClient = (baseURL: string): AxiosInstance => {
    const httpClient = axios.create({ baseURL });

    httpClient.interceptors.request.use(async config => {
        const token = await auth.service?.getAuthToken([]);
        if (token) {
            config.headers = { ...config.headers, Authorization: "Bearer " + token };
        }
        return config;
    });

    httpClient.interceptors.response.use(
        response => response,
        error => Promise.reject({ response: error.response, error: error }),
    );

    return httpClient;
};

export const catalogHttpClient = createHttpClient(CONSTANTS.ENDPOINTS.FACTORY_CATALOG_BASE_URL);
export const balancerHttpClient = createHttpClient(CONSTANTS.ENDPOINTS.BALANCER_BASE_URL);
