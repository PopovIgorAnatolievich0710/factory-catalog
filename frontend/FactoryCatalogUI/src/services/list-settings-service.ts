import { catalogApi } from "../helpers/api-helper";
import { GetListSettingsApiUrl } from "../constants/urls";
import { ListSettings } from "../entities/list";

class ListSettingsService {
    fetch(listKey: string): Promise<ListSettings> {
        return catalogApi.get<ListSettings>(GetListSettingsApiUrl(listKey));
    }

    save(listKey: string, columns: string[]): Promise<ListSettings> {
        return catalogApi.put<ListSettings>(GetListSettingsApiUrl(listKey), { columns });
    }
}

const listSettingsService = new ListSettingsService();
export default listSettingsService;
