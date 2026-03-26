using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public interface IFieldSyncService
{
    Task<SyncResult> SyncWithServerAsync(CancellationToken cancellationToken = default);
    Task<SyncResult> PullLatestAsync(CancellationToken cancellationToken = default);
    Task<SyncResult> PushPendingAsync(CancellationToken cancellationToken = default);
    Task<FieldSyncStatusDto> GetServerStatusAsync(CancellationToken cancellationToken = default);
}
