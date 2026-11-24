import { Api, catalogApi } from "../helpers/api-helper";
import { GetResourceExportApiUrl, GetResourceSearchApiUrl } from "../constants/urls";
import { DownloadedFile, ListRequest, ListResponse } from "../entities/list";

class ListService {
    search<T>(resource: string, request: ListRequest, api: Api = catalogApi): Promise<ListResponse<T>> {
        return api.post<ListResponse<T>>(GetResourceSearchApiUrl(resource), request);
    }

    export(resource: string, request: ListRequest, columns: string[], api: Api = catalogApi): Promise<DownloadedFile> {
        return api.download(GetResourceExportApiUrl(resource), { ...request, columns });
    }
}

const listService = new ListService();
export default listService;
