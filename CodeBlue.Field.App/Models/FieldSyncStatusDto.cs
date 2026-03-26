namespace CodeBlue.Field.App.Models;

public sealed class FieldSyncStatusDto
{
    public DateTimeOffset CheckedAtUtc { get; init; }
    public int CustomerCount { get; init; }
    public int WorkOrderCount { get; init; }
    public int ClaimCount { get; init; }
    public int DeletedCustomerCount { get; init; }
    public int DeletedWorkOrderCount { get; init; }
    public int DeletedClaimCount { get; init; }
    public int TotalChangeCount { get; init; }
    public bool HasRemoteChanges { get; init; }
}
