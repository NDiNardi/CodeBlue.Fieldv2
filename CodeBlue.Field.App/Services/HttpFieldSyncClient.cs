using System.Net.Http.Json;
using CodeBlue.Field.App.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace CodeBlue.Field.App.Services;

public sealed class HttpFieldSyncClient(HttpClient httpClient) : IFieldSyncClient
{
    private static readonly TimeSpan StatusRequestTimeout = TimeSpan.FromSeconds(12);

    public async Task<FieldSnapshotDto> PullSnapshotAsync(DateTimeOffset? sinceUtc = null, CancellationToken cancellationToken = default)
    {
        var query = sinceUtc.HasValue
            ? $"/api/field/snapshot?sinceUtc={Uri.EscapeDataString(sinceUtc.Value.UtcDateTime.ToString("O"))}"
            : "/api/field/snapshot";
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var snapshot = await response.Content.ReadFromJsonAsync<FieldSnapshotDto>(cancellationToken: cancellationToken);
        return snapshot ?? throw new InvalidOperationException("Field snapshot response was empty.");
    }

    public async Task<FieldSyncStatusDto> CheckStatusAsync(DateTimeOffset? sinceUtc = null, CancellationToken cancellationToken = default)
    {
        var query = sinceUtc.HasValue
            ? $"/api/field/sync/status?sinceUtc={Uri.EscapeDataString(sinceUtc.Value.UtcDateTime.ToString("O"))}"
            : "/api/field/sync/status";
        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(StatusRequestTimeout);

        using var response = await httpClient.SendAsync(request, timeoutCts.Token);
        response.EnsureSuccessStatusCode();

        var status = await response.Content.ReadFromJsonAsync<FieldSyncStatusDto>(cancellationToken: timeoutCts.Token);
        return status ?? throw new InvalidOperationException("Field sync status response was empty.");
    }

    public async Task<FieldPushResult> PushChangesAsync(OutboundSyncState outboundSyncState, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/field/sync/push")
        {
            Content = JsonContent.Create(outboundSyncState)
        };
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FieldPushResult>(cancellationToken: cancellationToken);
        return result ?? new FieldPushResult { ProcessedAtUtc = DateTimeOffset.UtcNow };
    }
}
