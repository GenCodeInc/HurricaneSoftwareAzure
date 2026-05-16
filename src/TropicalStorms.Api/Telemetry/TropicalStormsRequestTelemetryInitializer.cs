using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace TropicalStorms.Api.Telemetry;

internal sealed class TropicalStormsRequestTelemetryInitializer(IHttpContextAccessor httpContextAccessor) : ITelemetryInitializer
{
	private static readonly PathString JsonApiPathPrefix = new("/api/tropical-storms");
	private static readonly PathString SoapPath = new("/Services/TropicalStorms.asmx");

	public void Initialize(ITelemetry telemetry)
	{
		if (telemetry is not RequestTelemetry requestTelemetry)
		{
			return;
		}

		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext is null)
		{
			return;
		}

		if (httpContext.Request.Path.StartsWithSegments(JsonApiPathPrefix))
		{
			SetProperty(requestTelemetry, "TropicalStorms.Surface", "JSON");
			return;
		}

		if (!httpContext.Request.Path.Equals(SoapPath, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		SetProperty(requestTelemetry, "TropicalStorms.Surface", "SOAP");

		var operationName = ResolveSoapOperationName(httpContext);
		if (string.IsNullOrWhiteSpace(operationName))
		{
			return;
		}

		requestTelemetry.Name = $"{httpContext.Request.Method.ToUpperInvariant()} SOAP {operationName}";
		SetProperty(requestTelemetry, "TropicalStorms.OperationName", operationName);
	}

	private static string? ResolveSoapOperationName(HttpContext httpContext)
	{
		if (HttpMethods.IsGet(httpContext.Request.Method) && httpContext.Request.Query.ContainsKey("wsdl"))
		{
			return "WSDL";
		}

		var soapAction = httpContext.Request.Headers["SOAPAction"].FirstOrDefault();
		var normalizedSoapAction = NormalizeSoapAction(soapAction);
		if (!string.IsNullOrWhiteSpace(normalizedSoapAction))
		{
			return normalizedSoapAction;
		}

		var contentType = httpContext.Request.ContentType;
		if (string.IsNullOrWhiteSpace(contentType))
		{
			return null;
		}

		const string actionPrefix = "action=";
		var actionIndex = contentType.IndexOf(actionPrefix, StringComparison.OrdinalIgnoreCase);
		if (actionIndex < 0)
		{
			return null;
		}

		var actionValue = contentType[(actionIndex + actionPrefix.Length)..].Split(';', 2)[0].Trim();
		return NormalizeSoapAction(actionValue);
	}

	private static string? NormalizeSoapAction(string? soapAction)
	{
		if (string.IsNullOrWhiteSpace(soapAction))
		{
			return null;
		}

		var value = soapAction.Trim().Trim('"').Trim('\'');
		if (Uri.TryCreate(value, UriKind.Absolute, out var actionUri))
		{
			value = actionUri.Segments.LastOrDefault()?.Trim('/');
		}

		return string.IsNullOrWhiteSpace(value) ? null : value;
	}

	private static void SetProperty(RequestTelemetry requestTelemetry, string key, string value)
	{
		requestTelemetry.Properties[key] = value;
	}
}