namespace CodeBlue.Field.App.Models;

public sealed class QuickClaimEntryDraft
{
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Zip { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string OriginalInstallerDealer { get; init; } = string.Empty;
    public DateOnly? OriginalInstallationDate { get; init; }
    public string Equipment { get; init; } = string.Empty;
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
}
