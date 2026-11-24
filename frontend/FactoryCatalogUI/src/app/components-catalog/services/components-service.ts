import { catalogApi } from "../../../helpers/api-helper";
import listService from "../../../services/list-service";
import { CONSTANTS } from "../../../core/CONSTANTS";
import { ComponentDetails, ComponentFilterOptions, ComponentListItem, ComponentStatuses } from "../entities/component";
import {
    COMPONENT_FILTER_OPTIONS_API_URL,
    COMPONENTS_RESOURCE,
    GetAlternativeApiUrl,
    GetAlternativesApiUrl,
    GetComponentApiUrl,
    GetCriticalLevelApiUrl,
    GetCriticalLevelOverrideApiUrl,
    GetManualOosApiUrl,
} from "../constants";

class ComponentsService {
    fetch(erpId: string): Promise<ComponentDetails> {
        return catalogApi.get<ComponentDetails>(GetComponentApiUrl(erpId));
    }

    fetchFilterOptions(): Promise<ComponentFilterOptions> {
        return catalogApi.get<ComponentFilterOptions>(COMPONENT_FILTER_OPTIONS_API_URL);
    }

    searchActive(search: string): Promise<ComponentListItem[]> {
        return listService
            .search<ComponentListItem>(COMPONENTS_RESOURCE, {
                page: CONSTANTS.PAGING.DEFAULT_PAGE,
                pageSize: CONSTANTS.PAGING.DEFAULT_PAGE_SIZE,
                search,
                sortBy: "erpId",
                sortDirection: "asc",
                filters: { componentStatus: { values: [ComponentStatuses.Active] } },
            })
            .then(response => response.items);
    }

    setManualOos(erpId: string, factoryId: string, active: boolean, note: string | null): Promise<void> {
        return catalogApi.put<void>(GetManualOosApiUrl(erpId, factoryId), { active, note });
    }

    setCriticalLevel(erpId: string, level: number | null): Promise<void> {
        return catalogApi.put<void>(GetCriticalLevelApiUrl(erpId), { level });
    }

    setCriticalLevelOverride(erpId: string, factoryId: string, level: number | null): Promise<void> {
        return catalogApi.put<void>(GetCriticalLevelOverrideApiUrl(erpId, factoryId), { level });
    }

    addAlternative(erpId: string, alternativeErpId: string): Promise<void> {
        return catalogApi.post<void>(GetAlternativesApiUrl(erpId), {
            alternativeErpId,
            forBalancer: true,
            forPlanner: true,
        });
    }

    setAlternativeUse(erpId: string, alternativeErpId: string, forBalancer: boolean, forPlanner: boolean): Promise<void> {
        return catalogApi.put<void>(GetAlternativeApiUrl(erpId, alternativeErpId), { forBalancer, forPlanner });
    }

    removeAlternative(erpId: string, alternativeErpId: string): Promise<void> {
        return catalogApi.delete<void>(GetAlternativeApiUrl(erpId, alternativeErpId));
    }
}

const componentsService = new ComponentsService();
export default componentsService;
