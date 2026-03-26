namespace CodeBlue.Field.App.Models;

public sealed class CustomerSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Zip { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string GateCodes { get; init; } = string.Empty;
    public bool AnimalsPresent { get; init; }
    public string OriginalInstallerDealer { get; init; } = string.Empty;
    public DateOnly? StartupDate { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int OpenWorkOrderCount { get; init; }
}
