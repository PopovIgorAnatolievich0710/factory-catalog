import { catalogApi } from "../../../helpers/api-helper";
import { ImportValidationResult } from "../../../entities/import";
import {
    CapacityRow,
    CreateProcessRequest,
    ProcessDetails,
    ProcessFactorySetup,
    ProcessImportResult,
    ProcessSpecification,
    SetCapacityRequest,
} from "../entities";

export const PROCESSES_RESOURCE = "processes";

const PROCESSES_API_URL = "v1/processes";
const PROCESS_CAPACITY_API_URL = "v1/process-capacity";
const IMPORT_FILE_FIELD = "file";

const getProcessApiUrl = (processId: string) => `${PROCESSES_API_URL}/${encodeURIComponent(processId)}`;

const getFactoryCapacityApiUrl = (factoryId: string) =>
    `${PROCESS_CAPACITY_API_URL}/factories/${encodeURIComponent(factoryId)}`;

const toFormData = (file: File): FormData => {
    const formData = new FormData();
    formData.append(IMPORT_FILE_FIELD, file);
    return formData;
};

class ProcessService {
    fetch(processId: string): Promise<ProcessDetails> {
        return catalogApi.get<ProcessDetails>(getProcessApiUrl(processId));
    }

    create(request: CreateProcessRequest): Promise<ProcessDetails> {
        return catalogApi.post<ProcessDetails>(PROCESSES_API_URL, request);
    }

    updateSpecification(processId: string, specification: ProcessSpecification): Promise<ProcessDetails> {
        return catalogApi.put<ProcessDetails>(getProcessApiUrl(processId), specification);
    }

    updateFactorySetup(processId: string, factoryId: string, setup: ProcessFactorySetup): Promise<ProcessDetails> {
        return catalogApi.put<ProcessDetails>(
            `${getProcessApiUrl(processId)}/factories/${encodeURIComponent(factoryId)}`,
            setup,
        );
    }

    validateImport(file: File): Promise<ImportValidationResult> {
        return catalogApi.postForm<ImportValidationResult>(`${PROCESSES_API_URL}/import/validate`, toFormData(file));
    }

    import(file: File): Promise<ProcessImportResult> {
        return catalogApi.postForm<ProcessImportResult>(`${PROCESSES_API_URL}/import`, toFormData(file));
    }

    fetchCapacityOverview(): Promise<CapacityRow[]> {
        return catalogApi.get<CapacityRow[]>(PROCESS_CAPACITY_API_URL);
    }

    fetchFactoryCapacity(factoryId: string): Promise<CapacityRow[]> {
        return catalogApi.get<CapacityRow[]>(getFactoryCapacityApiUrl(factoryId));
    }

    setCapacity(factoryId: string, processId: string, request: SetCapacityRequest): Promise<CapacityRow | null> {
        return catalogApi.put<CapacityRow | null>(
            `${getFactoryCapacityApiUrl(factoryId)}/processes/${encodeURIComponent(processId)}`,
            request,
        );
    }
}

const processService = new ProcessService();
export default processService;
