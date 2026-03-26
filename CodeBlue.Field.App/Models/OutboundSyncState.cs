namespace CodeBlue.Field.App.Models;

public sealed class OutboundSyncState
{
    public IReadOnlyList<QueuedClaimCreate> ClaimsToCreate { get; init; } = [];
    public IReadOnlyList<QueuedServiceRequestOfficeAction> ServiceRequestOfficeActions { get; init; } = [];
    public IReadOnlyList<QueuedWorkOrderCompletion> WorkOrdersToComplete { get; init; } = [];
}
