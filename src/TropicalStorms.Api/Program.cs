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
builder.Services.Configure<TropicalStormsEmailOptions>(builder.Configuration.GetSection("TropicalStorms:Email"));
builder.Services.Configure<WebsiteAcsEmailOptions>(builder.Configuration.GetSection("TropicalStorms:Website:AcsEmail"));
builder.Services.PostConfigure<TropicalStormsEmailOptions>(options =>
{
	options.Enabled = GetBooleanConfigurationValue(builder.Configuration, options.Enabled, "TropicalStorms:Email:Enabled", "TropicalStorms__Email__Enabled");
	options.Host = GetConfigurationValue(builder.Configuration, options.Host, "TropicalStorms:Email:Host", "TropicalStorms__Email__Host");
	options.Port = GetIntegerConfigurationValue(builder.Configuration, options.Port, "TropicalStorms:Email:Port", "TropicalStorms__Email__Port");
	options.UseSsl = GetBooleanConfigurationValue(builder.Configuration, options.UseSsl, "TropicalStorms:Email:UseSsl", "TropicalStorms__Email__UseSsl");
	options.UserName = GetConfigurationValue(builder.Configuration, options.UserName, "TropicalStorms:Email:UserName", "TropicalStorms__Email__UserName");
	options.Password = GetConfigurationValue(builder.Configuration, options.Password, "TropicalStorms:Email:Password", "TropicalStorms__Email__Password");
	options.FromAddress = GetConfigurationValue(builder.Configuration, options.FromAddress, "TropicalStorms:Email:FromAddress", "TropicalStorms__Email__FromAddress");
	options.FromName = GetConfigurationValue(builder.Configuration, options.FromName, "TropicalStorms:Email:FromName", "TropicalStorms__Email__FromName");
	options.AdminAddress = GetConfigurationValue(builder.Configuration, options.AdminAddress, "TropicalStorms:Email:AdminAddress", "TropicalStorms__Email__AdminAddress");
});
builder.Services.Configure<WebsitePricingOptions>(builder.Configuration.GetSection("TropicalStorms:Website:Pricing"));
builder.Services.Configure<WebsitePayPalOptions>(builder.Configuration.GetSection("TropicalStorms:Website:PayPal"));
builder.Services.AddScoped<ITropicalStormsRepository, TropicalStormsRepository>();
builder.Services.AddScoped<ITropicalStormsEmailSender, SmtpTropicalStormsEmailSender>();
builder.Services.AddScoped<IWebsiteRegistrationRecoverySender, AcsWebsiteRegistrationRecoverySender>();
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

static int GetIntegerConfigurationValue(IConfiguration configuration, int currentValue, params string[] keys)
{
	foreach (var key in keys)
	{
		if (int.TryParse(configuration[key], out var parsedValue))
		{
			return parsedValue;
		}
	}

	return currentValue;
}