namespace CodeBlue.Field.App.Models;

public sealed class ClaimSummary
{
    public Guid Id { get; init; }
    public Guid? WorkOrderId { get; init; }
    public Guid? CustomerId { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ServiceAddress { get; init; } = string.Empty;
    public string Street1 { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string OriginalInstallerDealer { get; init; } = string.Empty;
    public DateOnly? OriginalInstallationDate { get; init; }
    public string Equipment { get; init; } = string.Empty;
    public DateOnly? FailureDate { get; init; }
    public DateOnly? RepairDate { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string CompletedBy { get; init; } = string.Empty;
    public DateOnly? CompletedOn { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ComponentCode { get; init; } = string.Empty;
    public string ModelNumber { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string IdSerialNumber { get; init; } = string.Empty;
    public string ComponentSerialNumber { get; init; } = string.Empty;
    public string ProblemComplaintReported { get; init; } = string.Empty;
    public string ProblemFound { get; init; } = string.Empty;
    public string RepairsPerformed { get; init; } = string.Empty;
    public string SerialNumber1StorageKey { get; init; } = string.Empty;
    public string SerialNumber2StorageKey { get; init; } = string.Empty;
    public bool HasPendingUpload { get; init; }
}
