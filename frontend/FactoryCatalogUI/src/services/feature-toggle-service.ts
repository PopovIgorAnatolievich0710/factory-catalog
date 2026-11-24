import { catalogApi } from "../helpers/api-helper";
import { FEATURE_TOGGLE_API_URL } from "../constants/urls";
import { FeatureToggle, FeatureToggleNames } from "../entities/feature-toggle";

class FeatureToggleService {
    fetchAll(): Promise<FeatureToggle[]> {
        return catalogApi.get<{ data: FeatureToggle[] }>(FEATURE_TOGGLE_API_URL).then(response => response.data ?? []);
    }

    isPermissionsEnabled(toggles: FeatureToggle[]): boolean {
        return toggles.some(toggle => toggle.name === FeatureToggleNames.PermissionsEnableAll && toggle.isEnabled);
    }
}

const featureToggleService = new FeatureToggleService();
export default featureToggleService;
