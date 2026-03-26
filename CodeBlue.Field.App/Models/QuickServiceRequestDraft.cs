namespace CodeBlue.Field.App.Models;

public sealed class QuickServiceRequestDraft
{
    public string Street1 { get; init; } = string.Empty;
    public string? Street2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Zip { get; init; } = string.Empty;
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string ServiceContactName { get; init; } = string.Empty;
    public string ServiceContactPhone { get; init; } = string.Empty;
    public string GateCodes { get; init; } = string.Empty;
    public bool AnimalsPresent { get; init; }
    public string BuilderCompanyName { get; init; } = string.Empty;
    public string BuilderContactName { get; init; } = string.Empty;
    public string BuilderContactPhone { get; init; } = string.Empty;
    public string BuilderEmail { get; init; } = string.Empty;
    public DateOnly? StartupDate { get; init; }
    public string ProblemDescription { get; init; } = string.Empty;
}
