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

builder.Services.AddControllers()
    .AddNewtonsoftJson();
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
builder.Services.AddScoped<ITropicalStormsRepository, TropicalStormsRepository>();
builder.Services.AddScoped<ITropicalStormsEmailSender, SmtpTropicalStormsEmailSender>();
builder.Services.AddScoped<ITropicalStormsFacade, TropicalStormsFacade>();
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