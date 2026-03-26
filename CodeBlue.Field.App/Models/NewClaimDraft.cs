namespace CodeBlue.Field.App.Models;

public sealed class NewClaimDraft
{
    public Guid? WorkOrderId { get; init; }
    public required Guid CustomerId { get; init; }
    public string WorkOrderNumber { get; init; } = string.Empty;
    public required string CustomerName { get; init; }
    public required string ServiceAddress { get; init; }
    public required string ContactName { get; init; }
    public required string ContactPhone { get; init; }
    public required string OriginalInstallerDealer { get; init; }
    public required DateOnly? OriginalInstallationDate { get; init; }
    public required string Equipment { get; init; }
    public DateOnly? FailureDate { get; init; }
    public required DateOnly RepairDate { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ModelNumber { get; init; } = string.Empty;
    public string IdSerialNumber { get; init; } = string.Empty;
    public string ComponentSerialNumber { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string CompletedBy { get; init; } = string.Empty;
    public ClaimImagePayload? Serial1Photo { get; init; }
    public ClaimImagePayload? Serial2Photo { get; init; }
}
