namespace CodeBlue.Field.App.Models;

public sealed class PlaceSelection
{
    public string Street1 { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Zip { get; init; } = string.Empty;
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
