using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public sealed class FieldSyncService(IFieldSyncClient syncClient, IFieldDataService fieldDataService) : IFieldSyncService
{
    public async Task<FieldSyncStatusDto> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        var lastSync = await fieldDataService.GetSyncSnapshotAsync(cancellationToken);
        DateTimeOffset? sinceUtc = lastSync.HasCompletedSync ? lastSync.LastSuccessfulSync : null;
        return await syncClient.CheckStatusAsync(sinceUtc, cancellationToken);
    }

    public async Task<SyncResult> SyncWithServerAsync(CancellationToken cancellationToken = default)
    {
        var outboundState = await fieldDataService.GetOutboundSyncStateAsync(cancellationToken);
        var hasPendingOutbound = outboundState.ClaimsToCreate.Count > 0
            || outboundState.ServiceRequestOfficeActions.Count > 0
            || outboundState.WorkOrdersToComplete.Count > 0;

        return hasPendingOutbound
            ? await PushPendingAsync(cancellationToken)
            : await PullLatestAsync(cancellationToken);
    }

    public async Task<SyncResult> PullLatestAsync(CancellationToken cancellationToken = default)
    {
        var lastSync = await fieldDataService.GetSyncSnapshotAsync(cancellationToken);
        DateTimeOffset? sinceUtc = lastSync.HasCompletedSync ? lastSync.LastSuccessfulSync : null;
        var snapshot = await syncClient.PullSnapshotAsync(sinceUtc, cancellationToken);
        await fieldDataService.ApplySnapshotAsync(snapshot, cancellationToken);

        return new SyncResult
        {
            Success = true,
            Message = snapshot.IsDelta
                ? "Synced with server and merged changed rows into the local cache."
                : "Synced with server and stored a full local snapshot.",
            AttemptedAtUtc = snapshot.GeneratedAtUtc,
            SyncKind = snapshot.IsDelta ? "Delta sync" : "Full sync",
            CustomerCount = snapshot.Customers.Count,
            WorkOrderCount = snapshot.WorkOrders.Count,
            ClaimCount = snapshot.Claims.Count,
            DeletedCustomerCount = snapshot.DeletedCustomerIds.Count,
            DeletedWorkOrderCount = snapshot.DeletedWorkOrderIds.Count,
            DeletedClaimCount = snapshot.DeletedClaimIds.Count
        };
    }

    public async Task<SyncResult> PushPendingAsync(CancellationToken cancellationToken = default)
    {
        var outboundState = await fieldDataService.GetOutboundSyncStateAsync(cancellationToken);
        var queuedClaimCount = outboundState.ClaimsToCreate.Count;
        var queuedOfficeActionCount = outboundState.ServiceRequestOfficeActions.Count;
        var queuedWorkOrderCount = outboundState.WorkOrdersToComplete.Count;

        if (queuedClaimCount == 0 && queuedOfficeActionCount == 0 && queuedWorkOrderCount == 0)
        {
            return await PullLatestAsync(cancellationToken);
        }

        var pushResult = await syncClient.PushChangesAsync(outboundState, cancellationToken);
        await fieldDataService.ClearCompletedSyncAsync(outboundState, cancellationToken);

        var snapshot = await syncClient.PullSnapshotAsync(null, cancellationToken);
        await fieldDataService.ApplySnapshotAsync(snapshot, cancellationToken);

        return new SyncResult
        {
            Success = true,
            Message = $"Synced with server: pushed {pushResult.CreatedClaimCount} claim(s), {pushResult.ScheduledWorkOrderCount} scheduled service request(s), and {pushResult.CompletedWorkOrderCount} completed service request update(s), then refreshed local cache.",
            AttemptedAtUtc = pushResult.ProcessedAtUtc,
            SyncKind = snapshot.IsDelta ? "Push + delta sync" : "Push + full sync",
            CustomerCount = snapshot.Customers.Count,
            WorkOrderCount = snapshot.WorkOrders.Count,
            ClaimCount = snapshot.Claims.Count,
            DeletedCustomerCount = snapshot.DeletedCustomerIds.Count,
            DeletedWorkOrderCount = snapshot.DeletedWorkOrderIds.Count,
            DeletedClaimCount = snapshot.DeletedClaimIds.Count
        };
    }
}
