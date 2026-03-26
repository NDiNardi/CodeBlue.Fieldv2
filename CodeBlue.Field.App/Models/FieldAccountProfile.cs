namespace CodeBlue.Field.App.Models;

public sealed class FieldAccountProfile
{
    public string Username { get; set; } = string.Empty;
    public string RolesCsv { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? PasswordChangedUtc { get; set; }
    public bool MustChangePassword { get; set; }
}
