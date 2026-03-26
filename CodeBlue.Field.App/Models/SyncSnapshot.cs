namespace CodeBlue.Field.App.Models;

public sealed class SyncSnapshot
{
    public required DateTimeOffset LastSuccessfulSync { get; init; }
    public required int PendingUploadCount { get; init; }
    public required bool IsOfflineModeEnabled { get; init; }
    public bool HasCompletedSync { get; init; }
}
