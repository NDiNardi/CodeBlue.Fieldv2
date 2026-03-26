namespace CodeBlue.Field.App.Models;

public sealed class SyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset AttemptedAtUtc { get; init; }
    public string SyncKind { get; init; } = string.Empty;
    public int CustomerCount { get; init; }
    public int WorkOrderCount { get; init; }
    public int ClaimCount { get; init; }
    public int DeletedCustomerCount { get; init; }
    public int DeletedWorkOrderCount { get; init; }
    public int DeletedClaimCount { get; init; }
}
