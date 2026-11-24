import { balancerApi } from "../../../helpers/api-helper";
import { DownloadedFile, ListRequest, ListResponse } from "../../../entities/list";
import {
    Allocation,
    DailyPlan,
    DashboardFilter,
    OpenItem,
    RoutingDashboard,
    ShadowComparison,
    TransferItem,
    TransferLogEntry,
    TransferRequest,
    TransferResult,
} from "../entities";

const toDashboardParams = (filter: DashboardFilter): Record<string, unknown> => ({
    from: filter.from,
    to: filter.to,
    ...(filter.factoryId ? { factoryId: filter.factoryId } : {}),
    ...(filter.country ? { country: filter.country } : {}),
});

class BalancerService {
    fetchRoutingDashboard(filter: DashboardFilter): Promise<RoutingDashboard> {
        return balancerApi.get<RoutingDashboard>("v1/dashboard/routing", toDashboardParams(filter));
    }

    fetchShadowComparison(filter: DashboardFilter): Promise<ShadowComparison> {
        return balancerApi.get<ShadowComparison>("v1/dashboard/shadow-comparison", toDashboardParams(filter));
    }

    exportDimCost(request: ListRequest & { columns: string[] }): Promise<DownloadedFile> {
        return balancerApi.download("v1/dim-cost/export", request);
    }

    searchAllocations(request: ListRequest): Promise<ListResponse<Allocation>> {
        return balancerApi.post<ListResponse<Allocation>>("v1/allocations/search", request);
    }

    fetchDailyPlan(factoryId: string, date: string): Promise<DailyPlan> {
        return balancerApi.get<DailyPlan>("v1/daily-plan", { factoryId, date });
    }

    searchOpenItems(request: ListRequest): Promise<ListResponse<OpenItem>> {
        return balancerApi.post<ListResponse<OpenItem>>("v1/transfers/open-items/search", request);
    }

    fetchOpenItemIds(request: ListRequest): Promise<string[]> {
        return balancerApi.post<string[]>("v1/transfers/open-items/ids", request);
    }

    fetchPages(factoryId: string): Promise<string[]> {
        return balancerApi.get<string[]>("v1/transfers/pages", { factoryId });
    }

    previewTransfer(request: TransferRequest): Promise<TransferItem[]> {
        return balancerApi.post<TransferItem[]>("v1/transfers/preview", request);
    }

    transfer(request: TransferRequest): Promise<TransferResult> {
        return balancerApi.post<TransferResult>("v1/transfers", request);
    }

    searchTransferLog(request: ListRequest): Promise<ListResponse<TransferLogEntry>> {
        return balancerApi.post<ListResponse<TransferLogEntry>>("v1/transfers/search", request);
    }
}

const balancerService = new BalancerService();
export default balancerService;
