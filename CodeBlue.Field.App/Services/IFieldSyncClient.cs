using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public interface IFieldSyncClient
{
    Task<FieldSnapshotDto> PullSnapshotAsync(DateTimeOffset? sinceUtc = null, CancellationToken cancellationToken = default);
    Task<FieldSyncStatusDto> CheckStatusAsync(DateTimeOffset? sinceUtc = null, CancellationToken cancellationToken = default);
    Task<FieldPushResult> PushChangesAsync(OutboundSyncState outboundSyncState, CancellationToken cancellationToken = default);
}
