namespace CodeBlue.Field.App.Models;

public sealed class QueuedWorkOrderCompletion
{
    public Guid WorkOrderId { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string CompletedBy { get; init; } = string.Empty;
    public DateOnly CompletedOn { get; init; }
    public DateTimeOffset QueuedAtUtc { get; init; }
}
