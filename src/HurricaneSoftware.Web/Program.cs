using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using HurricaneSoftware.Web;
using HurricaneSoftware.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
var fallbackApiBaseUrl = builder.HostEnvironment.BaseAddress.Contains("localhost", StringComparison.OrdinalIgnoreCase)
	|| builder.HostEnvironment.BaseAddress.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
	? "http://127.0.0.1:5085/"
	: "https://webservice.hurricanesoftware.com/";
var apiBaseUrl = new Uri(string.IsNullOrWhiteSpace(configuredApiBaseUrl) ? fallbackApiBaseUrl : configuredApiBaseUrl, UriKind.Absolute);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUrl });
builder.Services.AddScoped<WebsiteApiClient>();

await builder.Build().RunAsync();
