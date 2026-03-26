namespace CodeBlue.Field.App.Models;

public sealed class PendingChange
{
    public Guid Id { get; init; }
    public string CorrelationKey { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityIdentifier { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTimeOffset QueuedAt { get; init; }
}
