using CodeBlue.Field.App.Models;

namespace CodeBlue.Field.App.Services;

public interface IFieldAccountClient
{
    Task<FieldAuthSession> GetSessionAsync(CancellationToken cancellationToken = default);
    Task<FieldLoginResult> LoginAsync(FieldLoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<FieldAccountProfile> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<FieldAccountActionResult> ChangePasswordAsync(FieldChangePasswordRequest request, CancellationToken cancellationToken = default);
}
