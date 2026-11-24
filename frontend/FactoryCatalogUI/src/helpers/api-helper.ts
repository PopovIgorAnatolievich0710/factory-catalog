import { AxiosInstance } from "axios";
import { balancerHttpClient, catalogHttpClient } from "./http-client";
import { DownloadedFile } from "../entities/list";

export interface Api {
    get<T>(url: string, params?: Record<string, unknown>): Promise<T>;
    post<T>(url: string, data?: unknown): Promise<T>;
    put<T>(url: string, data?: unknown): Promise<T>;
    delete<T>(url: string): Promise<T>;
    postForm<T>(url: string, formData: FormData): Promise<T>;
    download(url: string, data: unknown): Promise<DownloadedFile>;
}

const getFileName = (contentDisposition: string | undefined): string | null => {
    if (!contentDisposition) return null;

    const encoded = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
    if (encoded) return decodeURIComponent(encoded[1]);

    const plain = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return plain ? plain[1] : null;
};

const createApi = (httpClient: AxiosInstance): Api => ({
    get: (url, params) => httpClient.get(url, { params }).then(response => response.data),
    post: (url, data) => httpClient.post(url, data).then(response => response.data),
    put: (url, data) => httpClient.put(url, data).then(response => response.data),
    delete: url => httpClient.delete(url).then(response => response.data),
    postForm: (url, formData) => httpClient.post(url, formData).then(response => response.data),
    download: (url, data) =>
        httpClient.post(url, data, { responseType: "blob" }).then(response => ({
            blob: response.data as Blob,
            fileName: getFileName(response.headers["content-disposition"]),
        })),
});

export const catalogApi = createApi(catalogHttpClient);
export const balancerApi = createApi(balancerHttpClient);
