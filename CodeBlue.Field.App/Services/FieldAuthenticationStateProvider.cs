using System.Security.Claims;
using System.Text.Json;
using CodeBlue.Field.App.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBlue.Field.App.Services;

public sealed class FieldAuthenticationStateProvider(
    IFieldAccountClient accountClient,
    IBrowserStorageService storage) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private const string SessionStorageKey = "field-auth-session";
    private FieldAuthSession? _session;
    private bool _isValidating;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_session is null)
        {
            var storedSession = await GetStoredSessionAsync();

            if (storedSession?.IsAuthenticated == true)
            {
                var liveSession = await SafeGetSessionAsync();
                _session = liveSession.IsAuthenticated ? liveSession : new FieldAuthSession();
                await PersistSessionAsync(_session);
            }
            else
            {
                _session = await SafeGetSessionAsync();
                await PersistSessionAsync(_session);
            }
        }

        return new AuthenticationState(BuildPrincipal(_session));
    }

    public async Task<FieldLoginResult> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var result = await accountClient.LoginAsync(new FieldLoginRequest
        {
            Username = username.Trim(),
            Password = password
        }, cancellationToken);

        _session = result.Session ?? new FieldAuthSession();
        await PersistSessionAsync(_session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(_session))));
        return result;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await accountClient.LogoutAsync(cancellationToken);
        }
        catch
        {
        }

        _session = new FieldAuthSession();
        await PersistSessionAsync(_session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _session = await SafeGetSessionAsync(cancellationToken);
        await PersistSessionAsync(_session);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(_session))));
    }

    private async Task ValidateSessionAsync()
    {
        if (_isValidating)
        {
            return;
        }

        _isValidating = true;
        try
        {
            var liveSession = await SafeGetSessionAsync();
            if (SessionsEqual(_session, liveSession))
            {
                return;
            }

            _session = liveSession;
            await PersistSessionAsync(_session);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(_session))));
        }
        finally
        {
            _isValidating = false;
        }
    }

    private async Task<FieldAuthSession> SafeGetSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await accountClient.GetSessionAsync(cancellationToken);
        }
        catch
        {
            return new FieldAuthSession();
        }
    }

    private async Task<FieldAuthSession?> GetStoredSessionAsync()
    {
        try
        {
            var json = await storage.GetStringAsync(SessionStorageKey);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<FieldAuthSession>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task PersistSessionAsync(FieldAuthSession? session)
    {
        try
        {
            if (session is null || !session.IsAuthenticated || string.IsNullOrWhiteSpace(session.Username))
            {
                await storage.RemoveStringAsync(SessionStorageKey);
                return;
            }

            await storage.SetStringAsync(SessionStorageKey, JsonSerializer.Serialize(session));
        }
        catch
        {
        }
    }

    private static bool SessionsEqual(FieldAuthSession? left, FieldAuthSession? right)
        => string.Equals(left?.Username, right?.Username, StringComparison.Ordinal)
           && string.Equals(left?.RolesCsv, right?.RolesCsv, StringComparison.Ordinal)
           && left?.IsAuthenticated == right?.IsAuthenticated
           && left?.MustChangePassword == right?.MustChangePassword;

    private static ClaimsPrincipal BuildPrincipal(FieldAuthSession? session)
    {
        if (session is null || !session.IsAuthenticated || string.IsNullOrWhiteSpace(session.Username))
        {
            return Anonymous;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, session.Username)
        };

        foreach (var role in (session.RolesCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "FieldCookie"));
    }
}
