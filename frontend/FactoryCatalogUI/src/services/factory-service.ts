import { catalogApi } from "../helpers/api-helper";
import { FACTORIES_API_URL } from "../constants/urls";
import { Factory } from "../entities/factory";

class FactoryService {
    fetchAll(): Promise<Factory[]> {
        return catalogApi.get<Factory[]>(FACTORIES_API_URL);
    }
}

const factoryService = new FactoryService();
export default factoryService;
