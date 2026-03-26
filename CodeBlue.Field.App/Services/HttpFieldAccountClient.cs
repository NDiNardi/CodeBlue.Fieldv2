using System.Net.Http.Json;
using CodeBlue.Field.App.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace CodeBlue.Field.App.Services;

public sealed class HttpFieldAccountClient(HttpClient httpClient) : IFieldAccountClient
{
    public async Task<FieldAuthSession> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/field/auth/session");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FieldAuthSession>(cancellationToken: cancellationToken)
            ?? new FieldAuthSession();
    }

    public async Task<FieldLoginResult> LoginAsync(FieldLoginRequest requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/field/auth/login")
        {
            Content = JsonContent.Create(requestModel)
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<FieldLoginResult>(cancellationToken: cancellationToken)
            ?? new FieldLoginResult { Success = false, Message = "Login failed." };

        if (!response.IsSuccessStatusCode)
        {
            return result;
        }

        return result;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/field/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<FieldAccountProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/field/account");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FieldAccountProfile>(cancellationToken: cancellationToken)
            ?? new FieldAccountProfile();
    }

    public async Task<FieldAccountActionResult> ChangePasswordAsync(FieldChangePasswordRequest requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/field/account/change-password")
        {
            Content = JsonContent.Create(requestModel)
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<FieldAccountActionResult>(cancellationToken: cancellationToken)
            ?? new FieldAccountActionResult { Success = false, Message = "Password change failed." };

        if (!response.IsSuccessStatusCode)
        {
            return result;
        }

        return result;
    }
}
