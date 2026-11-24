import { ApiError } from "../entities/api-error";

export const getApiErrorStatus = (error: ApiError): number | null => error?.response?.status ?? null;

export const extractApiErrorMessages = (error: ApiError, fallback: string): string[] => {
    const apiError = error?.response?.data;
    if (typeof apiError === "string" && apiError.length > 0) {
        return [apiError];
    }
    if (typeof apiError === "object" && apiError?.errors) {
        const messages = Object.entries(apiError.errors).flatMap(([field, fieldMessages]) =>
            (Array.isArray(fieldMessages) ? fieldMessages : [fieldMessages]).map(message => `${field}: ${message}`),
        );
        if (messages.length > 0) {
            return messages;
        }
    }
    if (typeof apiError === "object" && apiError?.message) {
        return [apiError.message];
    }
    return [fallback];
};
