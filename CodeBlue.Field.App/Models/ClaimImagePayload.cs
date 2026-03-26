namespace CodeBlue.Field.App.Models;

public sealed class ClaimImagePayload
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string Base64Data { get; init; } = string.Empty;
}
