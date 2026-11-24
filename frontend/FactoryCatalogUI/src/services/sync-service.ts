import { Api, catalogApi } from "../helpers/api-helper";
import { GetSyncRunApiUrl, GetSyncStatusApiUrl } from "../constants/urls";
import { SyncStatus } from "../entities/sync-status";

class SyncService {
    fetchStatus(syncType: string, api: Api = catalogApi): Promise<SyncStatus> {
        return api.get<SyncStatus>(GetSyncStatusApiUrl(syncType));
    }

    run(syncType: string, api: Api = catalogApi): Promise<void> {
        return api.post<void>(GetSyncRunApiUrl(syncType));
    }
}

const syncService = new SyncService();
export default syncService;
