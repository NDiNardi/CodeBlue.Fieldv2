namespace CodeBlue.Field.App.Models;

public sealed class FieldLoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public FieldAuthSession Session { get; set; } = new();
}
