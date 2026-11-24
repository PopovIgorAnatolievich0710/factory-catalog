export const AllocationModes = {
    Live: "Live",
    Shadow: "Shadow",
    Fallback: "Fallback",
} as const;

export type AllocationMode = (typeof AllocationModes)[keyof typeof AllocationModes];

export type AllocationStatus = "Allocated" | "NotRouted" | "Produced";

export type FulfillmentType = "Factory" | "Virtual" | "Dropshipper";

export interface Candidate {
    factoryId: string;
    quoteId: string | null;
    productionCost: number | null;
    shippingCost: number | null;
    totalCost: number | null;
    currency: string | null;
    etaFrom: string | null;
    etaTo: string | null;
    transitDaysMax: number | null;
    sharePct: number;
    capacityPct: number | null;
    effectiveSharePct: number | null;
    score: number | null;
    meetsPromise: boolean | null;
    meetsSla: boolean | null;
    excludedReason: string | null;
    isWinner: boolean;
}

export interface Allocation {
    id: string;
    orderId: string;
    orderItemId: string;
    skuUuid: string;
    quantity: number;
    fulfillmentType: FulfillmentType;
    destinationCountry: string;
    mode: AllocationMode;
    status: AllocationStatus;
    fallbackReason: string | null;
    quoteKey: string | null;
    selectedQuoteId: string | null;
    factoryId: string | null;
    originalFactoryId: string | null;
    packingFacilityId: string | null;
    productionPage: string | null;
    productionCost: number | null;
    shippingCost: number | null;
    totalCost: number | null;
    currency: string | null;
    etaFrom: string | null;
    etaTo: string | null;
    dueDate: string | null;
    promisedDeliveryDate: string | null;
    slaDeliveryDate: string | null;
    isEventOrder: boolean;
    meetsPromise: boolean | null;
    isSplit: boolean;
    decisionReason: string;
    candidates: Candidate[];
    legacyFactoryId: string | null;
    transferCount: number;
    decidedAt: string;
    producedAt: string | null;
}

export interface RoutedPerDay {
    date: string;
    factoryId: string;
    items: number;
}

export interface FactoryShare {
    factoryId: string;
    targetPct: number;
    actualPct: number;
    gapPct: number;
    isGapHighlighted: boolean;
}

export interface RoutingDashboard {
    itemsRouted: number;
    ordersRouted: number;
    itemsTransferred: number;
    transferRatePct: number | null;
    onTimePct: number | null;
    avgCostVsCheapestPct: number | null;
    fallbackCount: number;
    shareGapThresholdPct: number;
    routedPerDay: RoutedPerDay[];
    shares: FactoryShare[];
    countries: string[];
}

export interface DisagreementReason {
    reason: string;
    count: number;
    sharePct: number;
}

export interface ShadowComparison {
    compared: number;
    agreed: number;
    agreementRatePct: number | null;
    estimatedSavings: number;
    currency: string | null;
    disagreementReasons: DisagreementReason[];
}

export interface DashboardFilter {
    factoryId: string | null;
    country: string | null;
    from: string;
    to: string;
}

export interface DailyPlanItem {
    allocationId: string;
    orderId: string;
    orderItemId: string;
    skuUuid: string;
    quoteId: string | null;
    dueDate: string | null;
    quantity: number;
}

export interface DailyPlanProcess {
    processId: string;
    name: string;
    type: string | null;
    items: number;
    quantity: number;
    done: number | null;
    available: number | null;
    loadPct: number | null;
    isOverCapacity: boolean;
    orderItems: DailyPlanItem[];
}

export interface DailyPlan {
    factoryId: string;
    date: string;
    itemsToProduce: number;
    capacityKnown: boolean;
    processes: DailyPlanProcess[];
}

export interface OpenItem {
    allocationId: string;
    orderId: string;
    orderItemId: string;
    skuUuid: string;
    factoryId: string;
    productionPage: string | null;
    eventName: string | null;
    site: string | null;
    dueDate: string | null;
    quoteId: string | null;
    canTransfer: boolean;
}

export const TransferReasons = {
    ProcessOffline: "ProcessOffline",
    ComponentOos: "ComponentOos",
    OverCapacity: "OverCapacity",
    Quality: "Quality",
    CustomerRequest: "CustomerRequest",
    Other: "Other",
} as const;

export type TransferReason = (typeof TransferReasons)[keyof typeof TransferReasons];

export const transferReasonLabels: Record<TransferReason, string> = {
    ProcessOffline: "Process offline",
    ComponentOos: "Component OOS",
    OverCapacity: "Over capacity",
    Quality: "Quality",
    CustomerRequest: "Customer request",
    Other: "Other",
};

export interface TransferRequest {
    allocationIds: string[];
    toFactoryId: string;
    toPage: string;
    reason: TransferReason;
    note: string | null;
}

export interface TransferItem {
    allocationId: string;
    orderId: string;
    orderItemId: string;
    fromFactoryId: string;
    fromPage: string | null;
    toFactoryId: string;
    toPage: string;
    oldQuoteId: string | null;
    newQuoteId: string | null;
    costDelta: number | null;
    currency: string | null;
    targetCapacityPct: number | null;
    oldEta: string | null;
    newEta: string | null;
    skipped: boolean;
    problem: string | null;
}

export interface TransferResult {
    transferred: number;
    items: TransferItem[];
}

export interface TransferLogEntry {
    id: number;
    transferredAt: string;
    transferredBy: string;
    initiator: "User" | "Balancer";
    orderId: string;
    orderItemId: string;
    skuUuid: string;
    fromFactoryId: string;
    fromPage: string | null;
    toFactoryId: string;
    toPage: string;
    reason: TransferReason;
    note: string | null;
    oldQuoteId: string | null;
    newQuoteId: string | null;
    costDelta: number | null;
    currency: string | null;
    oldEta: string | null;
    newEta: string | null;
}

export interface OpenItemsQuery {
    orderId: string;
    factoryId: string;
    productionPage: string;
    eventName: string;
    site: string;
    dueDate: string;
}
