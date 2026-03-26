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
var localHttpsApiBaseUrl = builder.Configuration["LocalHttpsApiBaseUrl"];
if (appBaseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
    && appBaseUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
    && !string.IsNullOrWhiteSpace(localHttpsApiBaseUrl))
{
    apiBaseUrl = localHttpsApiBaseUrl;
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
