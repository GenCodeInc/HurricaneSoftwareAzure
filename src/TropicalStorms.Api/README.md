# TropicalStorms.Api

ASP.NET Core Web API replacement for the legacy TropicalStorms ASMX service.

## What this is

This project now exposes two surfaces from the same host:

- JSON Web API for new clients
- temporary legacy SOAP compatibility shim for older ASMX-style clients

The compatibility shim exists only to reduce client churn during migration. It is isolated under `LegacyCompat` so it is easy to remove later.

## Endpoints

### Web API

Base route:

```powershell
http://localhost:5085/api/tropical-storms
```

Examples:

```powershell
http://localhost:5085/api/tropical-storms/StormNames?username=demo&password=demo&region=All&activeOnly=true
http://localhost:5085/api/tropical-storms/GetStorm?username=demo&password=demo&stormID=8136&withImageLinks=true
http://localhost:5085/api/tropical-storms/GetCoordinates?username=demo&password=demo&stormID=8136
```

### Legacy SOAP shim

Base path:

```powershell
http://localhost:5085/Services/TropicalStorms.asmx
```

WSDL:

```powershell
http://localhost:5085/Services/TropicalStorms.asmx?wsdl
```

This shim is intended for temporary compatibility with older generated ASMX clients.

## How legacy clients use it

Older clients can keep using the service as a SOAP endpoint and point their web reference or generated proxy to:

```powershell
https://<your-app-name>.azurewebsites.net/Services/TropicalStorms.asmx?wsdl
```

That lets older clients continue to call methods like:

- `HelloWorld`
- `GetGISData`
- `StormNames`
- `GetStorm`
- `GetCoordinates`
- `ImageLinks`
- `Storms`
- `GetStormNames`
- `GetStormsDataset`

The backing implementation is now the ASP.NET Core API plus the isolated shim in `LegacyCompat`.

## Easy removal of the legacy shim

When you no longer need old clients, remove the shim by deleting these pieces:

1. Delete the `LegacyCompat` folder.
2. Remove the `SoapCore` package reference from `TropicalStorms.Api.csproj`.
3. Remove the SOAP registrations from `Program.cs`.
4. Remove the `TropicalStorms:LegacySoapShim` config section.

That returns this host to a plain JSON Web API.

## Local run

From the repo root:

```powershell
dotnet build .\NHCParser.Azure.sln
$env:NHCParser__SqlConnectionString='Server=tcp:<your-sql-server>.database.windows.net,1433;Initial Catalog=TTE;Persist Security Info=False;User ID=<your-user>;Password=<your-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
dotnet run --project .\src\TropicalStorms.Api\TropicalStorms.Api.csproj --urls http://127.0.0.1:5085
```

## Smoke test examples

JSON API:

```powershell
Invoke-RestMethod 'http://127.0.0.1:5085/api/tropical-storms/StormNames?username=demo&password=demo&region=All&activeOnly=true'
```

SOAP WSDL:

```powershell
Invoke-WebRequest 'http://127.0.0.1:5085/Services/TropicalStorms.asmx?wsdl' -UseBasicParsing
```

## Configuration

Important settings:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`
- `ConnectionStrings__TTE`
- `NHCParser__SqlConnectionString`
- `TropicalStorms__LegacySoapShim__Enabled`
- `TropicalStorms__LegacySoapShim__Path`
- `TropicalStorms__Email__*`

The app reads `ConnectionStrings:TTE` first, then falls back to `TTE:SqlConnectionString`, then `NHCParser:SqlConnectionString`.

If `APPLICATIONINSIGHTS_CONNECTION_STRING` or `ApplicationInsights:ConnectionString` is present, the API emits standard Application Insights request telemetry with adaptive sampling left on to limit ingestion cost.
JSON requests show up by request path, and SOAP requests are renamed by SOAP operation so you can tell `StormNames` from `GetCoordinates` instead of seeing one flat ASMX path.

## Azure hosting

Practical Azure target:

- Azure App Service Web App
- separate App Service plan from the Function App, because the existing Function plan is `Y1` Consumption and cannot host a standard Web App
- low-cost baseline: Linux `B1`

Current deployed app:

- Web App name: `api-tropicalstorms-linux-cu66c7`
- Public base URL: `https://webservice.hurricanesoftware.com`
- Public JSON API base: `https://webservice.hurricanesoftware.com/api/tropical-storms`
- Public SOAP WSDL: `https://webservice.hurricanesoftware.com/Services/TropicalStorms.asmx?wsdl`
- Direct Azure host for app-service-only checks: `https://api-tropicalstorms-linux-cu66c7.azurewebsites.net`

Public exposure status:

- the App Service is currently internet-accessible on its `azurewebsites.net` hostname
- HTTPS-only is enabled
- current access restriction rule is `Allow all access`

Database exposure status:

- the API is public, but the SQL database is not opened to the whole internet by this README
- current deployment uses App Service VNet integration plus the SQL private endpoint so the web app can connect without App Service public firewall IP rules
- current firewall rules should allow only:
	- legacy host IP `184.73.224.130`
	- current client IPs you intentionally add for administration
- `AllowAzureServices` should stay off
- a tighter long-term setup is still a private endpoint plus managed identity

Publish with:

```powershell
.\scripts\deploy-tropicalstorms-api.ps1
```

The deployment script also ensures the Azure SQL `AllowAzureServices` firewall rule exists so the App Service host can reach the `TTE` database after deployment.
The deployment script now keeps the App Service on VNet integration so it can use the existing SQL private endpoint path instead of App Service outbound IP firewall rules.
If `APPLICATIONINSIGHTS_CONNECTION_STRING` exists in `.env`, the deployment script also writes it into the App Service settings so request telemetry starts flowing without another portal step.

Tail logs with:

```powershell
az webapp log tail --name <api app name> --resource-group <resource group>
```
