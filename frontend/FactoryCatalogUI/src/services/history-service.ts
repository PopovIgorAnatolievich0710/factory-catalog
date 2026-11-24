import { Api, catalogApi } from "../helpers/api-helper";
import { GetHistoryApiUrl } from "../constants/urls";
import { HistoryEntry } from "../entities/history";
import { ListResponse } from "../entities/list";

class HistoryService {
    fetch(
        entityType: string,
        entityId: string,
        page: number,
        pageSize: number,
        api: Api = catalogApi,
    ): Promise<ListResponse<HistoryEntry>> {
        return api.get<ListResponse<HistoryEntry>>(GetHistoryApiUrl(entityType, entityId), { page, pageSize });
    }
}

const historyService = new HistoryService();
export default historyService;
