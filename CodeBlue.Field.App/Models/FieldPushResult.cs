namespace CodeBlue.Field.App.Models;

public sealed class FieldPushResult
{
    public DateTimeOffset ProcessedAtUtc { get; init; }
    public int CreatedClaimCount { get; init; }
    public int ScheduledWorkOrderCount { get; init; }
    public int CompletedWorkOrderCount { get; init; }
}
