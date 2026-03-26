namespace CodeBlue.Field.App.Models;

public sealed class QueuedServiceRequestOfficeAction
{
    public Guid WorkOrderId { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public Guid AssignedToUserId { get; init; }
    public string AssignedToUsername { get; init; } = string.Empty;
    public decimal? EstimatedHours { get; init; }
    public string JobCategory { get; init; } = string.Empty;
    public DateTimeOffset QueuedAtUtc { get; init; }
}
