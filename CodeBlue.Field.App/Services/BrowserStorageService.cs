using Microsoft.JSInterop;

namespace CodeBlue.Field.App.Services;

public sealed class BrowserStorageService(IJSRuntime jsRuntime) : IBrowserStorageService
{
    public ValueTask<string?> GetStringAsync(string key)
        => jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask SetStringAsync(string key, string value)
        => jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    public ValueTask RemoveStringAsync(string key)
        => jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
}
