namespace CodeBlue.Field.App.Models;

public sealed class QueuedClaimCreate
{
    public Guid LocalClaimId { get; init; }
    public Guid? WorkOrderId { get; init; }
    public Guid CustomerId { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ServiceAddress { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string OriginalInstallerDealer { get; init; } = string.Empty;
    public DateOnly? OriginalInstallationDate { get; init; }
    public DateOnly? FailureDate { get; init; }
    public DateOnly RepairDate { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ModelNumber { get; init; } = string.Empty;
    public string IdSerialNumber { get; init; } = string.Empty;
    public string ComponentSerialNumber { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string CompletedBy { get; init; } = string.Empty;
    public ClaimImagePayload? Serial1Photo { get; init; }
    public ClaimImagePayload? Serial2Photo { get; init; }
    public DateTimeOffset QueuedAtUtc { get; init; }
}
