# ReadMeInsights

This file is the practical guide for seeing what the TropicalStorms API and parser are doing in Azure without turning telemetry into a cost problem.

## Cost-first guidance

- The API now emits Application Insights request telemetry only when `APPLICATIONINSIGHTS_CONNECTION_STRING` is configured on the App Service.
- The API uses standard request telemetry only. It does not add a separate custom event stream for every request.
- Adaptive sampling stays on, which keeps ingestion cost lower.
- When you query request counts, use `sum(itemCount)` instead of `count()` so sampled data still reflects the real hit volume.

## What the API sends

When Application Insights is enabled for the API:

- JSON requests appear as normal request telemetry for `/api/tropical-storms/...`
- SOAP requests are renamed to the SOAP operation name, for example `POST SOAP StormNames`
- WSDL requests appear as `GET SOAP WSDL`
- request telemetry also includes these custom dimensions:
  - `TropicalStorms.Surface` = `JSON` or `SOAP`
  - `TropicalStorms.OperationName` for SOAP requests when the operation can be inferred from `SOAPAction`

## Where to look in Azure Portal

API request telemetry:

- Azure Portal > Application Insights > your API Insights resource > Logs
- Azure Portal > Application Insights > your API Insights resource > Live metrics
- Azure Portal > Application Insights > your API Insights resource > Failures
- Azure Portal > Application Insights > your API Insights resource > Performance

Parser telemetry:

- Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Logs
- Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Live metrics

## Fast checks

If you want to confirm the API App Service has the telemetry hook configured, check for this setting:

```powershell
az webapp config appsettings list --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --query "[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING']"
```

If that returns nothing, the API code is ready, but telemetry is not turned on yet.

## Useful KQL queries

### Top API endpoints by hit count

```kusto
requests
| where timestamp > ago(24h)
| where url contains "/api/tropical-storms" or url contains "/Services/TropicalStorms.asmx"
| summarize Hits = sum(itemCount), Failures = sumif(itemCount, success == false), P95Ms = percentile(duration, 95) by name
| order by Hits desc
```

### JSON endpoint usage only

```kusto
requests
| where timestamp > ago(24h)
| where tostring(customDimensions["TropicalStorms.Surface"]) == "JSON"
| summarize Hits = sum(itemCount), P95Ms = percentile(duration, 95) by name
| order by Hits desc
```

### SOAP operation usage only

```kusto
requests
| where timestamp > ago(24h)
| where tostring(customDimensions["TropicalStorms.Surface"]) == "SOAP"
| summarize Hits = sum(itemCount), Failures = sumif(itemCount, success == false), P95Ms = percentile(duration, 95) by name, SoapOperation = tostring(customDimensions["TropicalStorms.OperationName"])
| order by Hits desc
```

### Slowest endpoints

```kusto
requests
| where timestamp > ago(24h)
| where url contains "/api/tropical-storms" or url contains "/Services/TropicalStorms.asmx"
| summarize AvgMs = avg(duration), P95Ms = percentile(duration, 95), Hits = sum(itemCount) by name
| order by P95Ms desc
```

### Failed requests

```kusto
requests
| where timestamp > ago(24h)
| where success == false
| where url contains "/api/tropical-storms" or url contains "/Services/TropicalStorms.asmx"
| project timestamp, name, resultCode, duration, url
| order by timestamp desc
```

## When to turn it on

Turn API Insights on when you need to answer questions like:

- which API endpoints are actually being used
- which SOAP methods are still active
- which calls are slow or failing

Leave it off if you do not need that visibility right now and want to minimize ingestion cost.

## How to turn it on later

1. Create or choose an Application Insights resource for the API.
2. Put its connection string in `.env` as `APPLICATIONINSIGHTS_CONNECTION_STRING`.
3. Redeploy the API with `./scripts/deploy-tropicalstorms-api.ps1`.
4. Open Logs or Live metrics in the Application Insights resource.