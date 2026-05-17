using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using SoapCore;
using TTENET.TTEBusiness.Core.Models;
using TTENET.TTEBusiness.Core.Services;
using TropicalStorms.Api.LegacyCompat;
using TropicalStorms.Api.Services;
using TropicalStorms.Api.Telemetry;

var builder = WebApplication.CreateBuilder(args);
var legacySoapOptions = builder.Configuration.GetSection("TropicalStorms:LegacySoapShim").Get<LegacySoapOptions>() ?? new LegacySoapOptions();
var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
	?? builder.Configuration["ApplicationInsights:ConnectionString"];
var websiteAllowedOrigins = builder.Configuration.GetSection("TropicalStorms:Website:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddCors(options =>
{
	options.AddPolicy("WebsiteFrontend", policy =>
	{
		if (websiteAllowedOrigins.Length == 0)
		{
			return;
		}

		policy.WithOrigins(websiteAllowedOrigins)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});
builder.Services.AddSoapCore();
if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
{
	builder.Services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
	{
		ConnectionString = applicationInsightsConnectionString,
		EnableAdaptiveSampling = true
	});
	builder.Services.AddHttpContextAccessor();
	builder.Services.AddSingleton<ITelemetryInitializer, TropicalStormsRequestTelemetryInitializer>();
}
builder.Services.Configure<TteDataOptions>(options =>
{
	options.SqlConnectionString = builder.Configuration.GetConnectionString("TTE")
		?? builder.Configuration["TTE:SqlConnectionString"]
		?? builder.Configuration["NHCParser:SqlConnectionString"];
});
builder.Services.Configure<LegacySoapOptions>(builder.Configuration.GetSection("TropicalStorms:LegacySoapShim"));
builder.Services.Configure<WebsiteAcsEmailOptions>(builder.Configuration.GetSection("TropicalStorms:Website:AcsEmail"));
builder.Services.PostConfigure<WebsiteAcsEmailOptions>(options =>
{
	options.Enabled = GetBooleanConfigurationValue(builder.Configuration, options.Enabled, "TropicalStorms:Website:AcsEmail:Enabled", "TropicalStorms__Website__AcsEmail__Enabled");
	options.ConnectionString = GetConfigurationValue(builder.Configuration, options.ConnectionString, "TropicalStorms:Website:AcsEmail:ConnectionString", "TropicalStorms__Website__AcsEmail__ConnectionString");
	options.SenderAddress = GetConfigurationValue(builder.Configuration, options.SenderAddress, "TropicalStorms:Website:AcsEmail:SenderAddress", "TropicalStorms__Website__AcsEmail__SenderAddress");
	options.SenderDisplayName = GetConfigurationValue(builder.Configuration, options.SenderDisplayName, "TropicalStorms:Website:AcsEmail:SenderDisplayName", "TropicalStorms__Website__AcsEmail__SenderDisplayName");
	options.AdminAddress = GetConfigurationValue(builder.Configuration, options.AdminAddress, "TropicalStorms:Website:AcsEmail:AdminAddress", "TropicalStorms__Website__AcsEmail__AdminAddress");
});
builder.Services.Configure<WebsitePricingOptions>(builder.Configuration.GetSection("TropicalStorms:Website:Pricing"));
builder.Services.Configure<WebsitePayPalOptions>(builder.Configuration.GetSection("TropicalStorms:Website:PayPal"));
builder.Services.AddScoped<ITropicalStormsRepository, TropicalStormsRepository>();
builder.Services.AddScoped<AcsWebsiteRegistrationRecoverySender>();
builder.Services.AddScoped<ITropicalStormsEmailSender>(static provider => provider.GetRequiredService<AcsWebsiteRegistrationRecoverySender>());
builder.Services.AddScoped<IWebsiteRegistrationRecoverySender>(static provider => provider.GetRequiredService<AcsWebsiteRegistrationRecoverySender>());
builder.Services.AddScoped<ITropicalStormsFacade, TropicalStormsFacade>();
builder.Services.AddHttpClient<IWebsitePaymentsClient, PayPalWebsitePaymentsClient>();
builder.Services.AddScoped<IWebsiteExperienceService, WebsiteExperienceService>();
builder.Services.AddScoped<ILegacyTropicalStormsSoapService, LegacyTropicalStormsSoapService>();

var app = builder.Build();
var legacyWsdlPath = Path.Combine(app.Environment.ContentRootPath, "LegacyCompat", "TropicalStorms.Legacy.wsdl");

app.Use(async (context, next) =>
{
	var allowHttpForLegacySoap = legacySoapOptions.Enabled
		&& !context.Request.IsHttps
		&& context.Request.Path.Equals(legacySoapOptions.Path, StringComparison.OrdinalIgnoreCase)
		&& legacySoapOptions.AllowHttpHostNames.Contains(context.Request.Host.Host, StringComparer.OrdinalIgnoreCase);

	if (!allowHttpForLegacySoap && !context.Request.IsHttps)
	{
		var httpsUrl = $"https://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
		context.Response.Redirect(httpsUrl, permanent: true);
		return;
	}

	if (legacySoapOptions.Enabled
		&& HttpMethods.IsGet(context.Request.Method)
		&& context.Request.Path.Equals(legacySoapOptions.Path, StringComparison.OrdinalIgnoreCase)
		&& context.Request.Query.ContainsKey("wsdl")
		&& File.Exists(legacyWsdlPath))
	{
		var currentServiceAddress = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}";
		var wsdl = await File.ReadAllTextAsync(legacyWsdlPath);
		wsdl = wsdl.Replace("https://webservice.hurricanesoftware.com/Services/TropicalStorms.asmx", currentServiceAddress, StringComparison.OrdinalIgnoreCase);

		context.Response.ContentType = "text/xml; charset=utf-8";
		await context.Response.WriteAsync(wsdl);
		return;
	}

	await next();
});
app.UseRouting();
if (websiteAllowedOrigins.Length > 0)
{
	app.UseCors("WebsiteFrontend");
}
app.UseEndpoints(endpoints =>
{
	endpoints.MapControllers();

	if (legacySoapOptions.Enabled)
	{
		endpoints.UseSoapEndpoint<ILegacyTropicalStormsSoapService>(
			legacySoapOptions.Path,
			new SoapEncoderOptions(),
			SoapSerializer.XmlSerializer,
			caseInsensitivePath: true);
	}
});

app.Run();

static string GetConfigurationValue(IConfiguration configuration, string currentValue, params string[] keys)
{
	foreach (var key in keys)
	{
		var value = configuration[key];
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
	}

	return currentValue;
}

static bool GetBooleanConfigurationValue(IConfiguration configuration, bool currentValue, params string[] keys)
{
	foreach (var key in keys)
	{
		if (bool.TryParse(configuration[key], out var parsedValue))
		{
			return parsedValue;
		}
	}

	return currentValue;
}