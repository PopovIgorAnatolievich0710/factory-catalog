namespace Tenengroup.Balancer.Domains;

public enum BalancerMode
{
    Live,
    Shadow
}

public enum AllocationMode
{
    Live,
    Shadow,
    Fallback
}

public enum AllocationStatus
{
    Allocated,
    NotRouted,
    Produced
}

public enum FulfillmentType
{
    Factory,
    Virtual,
    Dropshipper
}

public enum CostLineKind
{
    Component,
    Process,
    Packing
}

public enum TransferReason
{
    ProcessOffline,
    ComponentOos,
    OverCapacity,
    Quality,
    CustomerRequest,
    Other
}

public enum TransferInitiator
{
    User,
    Balancer
}
