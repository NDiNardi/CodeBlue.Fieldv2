using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public interface IFieldDataService
{
    Task<SyncSnapshot> GetSyncSnapshotAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FieldTechnicianOption>> GetTechniciansAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderSummary>> GetWorkOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSummary>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimSummary>> GetClaimsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingChange>> GetPendingChangesAsync(CancellationToken cancellationToken = default);
    Task<OutboundSyncState> GetOutboundSyncStateAsync(CancellationToken cancellationToken = default);
    Task ApplySnapshotAsync(FieldSnapshotDto snapshot, CancellationToken cancellationToken = default);
    Task QueueNewClaimAsync(NewClaimDraft draft, CancellationToken cancellationToken = default);
    Task QueueServiceRequestOfficeActionAsync(Guid workOrderId, DateOnly scheduledDate, Guid assignedToUserId, CancellationToken cancellationToken = default);
    Task UpdateWorkOrderScheduleAsync(Guid workOrderId, DateOnly? scheduledDate, CancellationToken cancellationToken = default);
    Task ReorderScheduledWorkOrderAsync(Guid workOrderId, int direction, CancellationToken cancellationToken = default);
    Task QueueWorkOrderCompletionAsync(Guid workOrderId, string completedBy, DateOnly completedOn, CancellationToken cancellationToken = default);
    Task ClearCompletedSyncAsync(OutboundSyncState syncedState, CancellationToken cancellationToken = default);
}
