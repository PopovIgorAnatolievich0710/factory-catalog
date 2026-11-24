export const CONSTANTS = {
    ERROR_CODES: {
        UNAUTHORIZED: 401,
        FORBIDDEN: 403,
        NOT_FOUND: 404,
        CONFLICT: 409,
        VALIDATION: 422,
        ISE: 500,
    },
    ENDPOINTS: {
        FACTORY_CATALOG_BASE_URL: (window as any).env.FACTORY_CATALOG_BASE_API_URL as string,
        BALANCER_BASE_URL: (window as any).env.BALANCER_BASE_API_URL as string,
    },
    PAGING: {
        DEFAULT_PAGE: 1,
        DEFAULT_PAGE_SIZE: 25,
        PAGE_SIZE_OPTIONS: [25, 50, 100],
    },
    LIST: {
        MAX_OPTIONAL_COLUMNS: 8,
    },
    SERVICE_NAME: ((window as any).env.SERVICE_NAME || "") as string,
};
