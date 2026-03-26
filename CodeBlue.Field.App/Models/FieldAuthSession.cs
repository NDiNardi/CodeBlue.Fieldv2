namespace CodeBlue.Field.App.Models;

public sealed class FieldAuthSession
{
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; } = string.Empty;
    public string RolesCsv { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}
