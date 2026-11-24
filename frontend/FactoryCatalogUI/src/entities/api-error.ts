export interface ApiErrorBody {
    message?: string;
    errors?: Record<string, string[]>;
}

export interface ApiError {
    response?: {
        status: number;
        data?: ApiErrorBody | string;
    };
    error?: {
        message?: string;
    };
}
