using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CodeBlue.Field.App;
using CodeBlue.Field.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    apiBaseUrl = builder.HostEnvironment.BaseAddress;
}

var appBaseUri = new Uri(builder.HostEnvironment.BaseAddress, UriKind.Absolute);
var localHttpApiBaseUrl = builder.Configuration["LocalHttpApiBaseUrl"];
var isLocalDevHost =
    appBaseUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
    || appBaseUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

if (isLocalDevHost
    && builder.HostEnvironment.IsDevelopment())
{
    if (!string.IsNullOrWhiteSpace(localHttpApiBaseUrl))
    {
        apiBaseUrl = localHttpApiBaseUrl;
    }
}

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)
});
builder.Services.AddScoped<IBrowserStorageService, BrowserStorageService>();
builder.Services.AddScoped<IFieldDataService, LocalFieldDataService>();
builder.Services.AddScoped<IFieldSyncClient, HttpFieldSyncClient>();
builder.Services.AddScoped<IFieldSyncService, FieldSyncService>();
builder.Services.AddScoped<IFieldAccountClient, HttpFieldAccountClient>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<FieldAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<FieldAuthenticationStateProvider>());

await builder.Build().RunAsync();
