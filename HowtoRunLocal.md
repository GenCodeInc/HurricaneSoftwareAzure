# How to Run Locally

This guide shows how to run both services on this machine so you can step through the code in Visual Studio or VS Code.

## What you can run here

- `TropicalStorms.Api` runs locally as a normal ASP.NET Core app.
- `NHCParser.Function` runs locally with Azure Functions Core Tools.
- `HurricaneSoftware.Web` runs locally as a standalone Blazor WebAssembly front-end for the public website migration.
- Both services need a SQL connection string if you want database-backed behavior.

## Prerequisites

Confirmed in this workspace:

- `.NET SDK` is installed
- `Azure Functions Core Tools` is installed
- the solution builds successfully

Not currently confirmed:

- `Azurite` is not on `PATH`

That only affects the Function App if you want to use `AzureWebJobsStorage=UseDevelopmentStorage=true`.

## Build the solution

From the repo root:

```powershell
dotnet build .\NHCParser.Azure.sln
```

## Run the API locally

The API project is:

- `src/TropicalStorms.Api`

It reads the SQL connection string from these keys, in this order:

1. `ConnectionStrings__TTE`
2. `TTE__SqlConnectionString`
3. `NHCParser__SqlConnectionString`

Optional telemetry key:

- `ApplicationInsights__ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING`

Optional website ACS email keys for local website email testing:

- `TropicalStorms__Website__AcsEmail__Enabled`
- `TropicalStorms__Website__AcsEmail__ConnectionString`
- `TropicalStorms__Website__AcsEmail__SenderAddress`
- `TropicalStorms__Website__AcsEmail__SenderDisplayName`

### Quick run

From the repo root:

```powershell
$env:ConnectionStrings__TTE='Server=tcp:sql-gencode-cu-66c7.database.windows.net,1433;Initial Catalog=TTE;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
dotnet run --project .\src\TropicalStorms.Api\TropicalStorms.Api.csproj --urls http://127.0.0.1:5085
```

### Local URLs

- JSON API base: `http://127.0.0.1:5085/api/tropical-storms`
- Website-only API base: `http://127.0.0.1:5085/api/website`
- SOAP shim WSDL: `http://127.0.0.1:5085/Services/TropicalStorms.asmx?wsdl`

### Smoke tests

```powershell
Invoke-RestMethod 'http://127.0.0.1:5085/api/tropical-storms/StormNames?username=demo&password=demo&region=All&activeOnly=true'
Invoke-WebRequest 'http://127.0.0.1:5085/Services/TropicalStorms.asmx?wsdl' -UseBasicParsing
```

### API local settings file

If you prefer not to set the connection string in the terminal each time:

1. Copy `src/TropicalStorms.Api/appsettings.Development.example.json` to `src/TropicalStorms.Api/appsettings.Development.json`
2. Replace the placeholder SQL credentials
3. Run the API with `ASPNETCORE_ENVIRONMENT=Development`

`appsettings.Development.json` is ignored by git so local secrets stay local.

If you want local requests to appear in Application Insights too, set `ApplicationInsights__ConnectionString` in that file or export `APPLICATIONINSIGHTS_CONNECTION_STRING` in the terminal before starting the API.

If you want the website lost-registration, contact, or order emails to use ACS locally, add the `TropicalStorms:Website:AcsEmail:*` settings to that same `appsettings.Development.json` file.

### Debugging

- Open `NHCParser.Azure.sln`
- Set `TropicalStorms.Api` as the startup project in Visual Studio, or run/debug `src/TropicalStorms.Api/TropicalStorms.Api.csproj` in VS Code
- Put breakpoints in controllers, repository code, or the legacy SOAP shim

## Run the Function locally

The Function project is:

- `src/NHCParser.Function`

### Local settings file

From `src/NHCParser.Function`:

1. Copy `local.settings.json.example` to `local.settings.json`
2. Put in a real SQL connection string
3. Choose one storage option below

Example `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "NHC_TIMER_SCHEDULE": "0 */5 * * * *",
    "NHCParser__CurrentYearOnly": "true",
    "NHCParser__SqlConnectionString": "Server=tcp:sql-gencode-cu-66c7.database.windows.net,1433;Initial Catalog=TTE;Persist Security Info=False;User ID=<user>;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### Storage options for local Function runs

Option 1: install and run Azurite

- keep `AzureWebJobsStorage=UseDevelopmentStorage=true`
- install Azurite separately if you want local storage emulation

Option 2: use a real Azure Storage connection string

- replace `UseDevelopmentStorage=true` with a real storage connection string
- this avoids needing Azurite on this machine

### Start the Function

From `src/NHCParser.Function`:

```powershell
func start
```

### Debugging

- Open `NHCParser.Azure.sln`
- Start the Function project under the debugger, or attach to the running Functions host
- Put breakpoints in `Functions/NhcParserTimerFunction.cs` and parser/service classes
- In VS Code, start the `run NHCParser.Function` task and then use `Attach to NHCParser.Function`

### Useful local settings

- every 1 minute: `NHC_TIMER_SCHEDULE=0 */1 * * * *`
- every 5 minutes: `NHC_TIMER_SCHEDULE=0 */5 * * * *`
- every 15 minutes: `NHC_TIMER_SCHEDULE=0 */15 * * * *`
- allow older advisories: `NHCParser__CurrentYearOnly=false`

## Recommended local workflow

If you want the simplest step-through loop:

1. Start the API first and verify it can hit SQL.
2. Start `HurricaneSoftware.Web` if you want to exercise the migrated public website routes or website checkout/contact flows.
3. Start the Function with a short timer schedule if you are testing parser ingestion too.
4. Use a reduced source list in `src/NHCParser.Function/appsettings.json` if you only want to test one advisory.
5. Set breakpoints and watch the console output from the API and `func start`.

## Run the website locally

The website project is:

- `src/HurricaneSoftware.Web`

From the repo root:

```powershell
dotnet run --project .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj
```

Important local behavior:

- when the website runs on `localhost`, it defaults its API base URL to `http://127.0.0.1:5085/`
- if you want a different API host, change `src/HurricaneSoftware.Web/wwwroot/appsettings.json`
- the API must be running for contact, registration recovery, confirmation, pricing, and checkout calls to work

## Common problems

### `terraform plan` worked but local Function does not start

That is usually storage setup, not Terraform. The Function host needs either:

- Azurite running locally, or
- a real Azure Storage connection string

### API starts but database calls fail

Check that your local connection string is set and that your client IP is allowed to reach Azure SQL if you are using the public SQL endpoint.

### Function parses but skips persistence

If you are testing old advisories, set:

```text
NHCParser__CurrentYearOnly=false
```

## Files to look at while debugging

- `src/TropicalStorms.Api/Program.cs`
- `src/TropicalStorms.Api/appsettings.Development.example.json`
- `src/HurricaneSoftware.Web/Program.cs`
- `src/HurricaneSoftware.Web/wwwroot/appsettings.json`
- `src/HurricaneSoftware.Web/wwwroot/staticwebapp.config.json`
- `src/NHCParser.Function/Program.cs`
- `src/NHCParser.Function/Functions/NhcParserTimerFunction.cs`
- `src/NHCParser.Function/appsettings.json`
- `src/NHCParser.Function/local.settings.json.example`

## VS Code helpers

Checked-in workspace files now exist for local runs:

- `.vscode/tasks.json`
- `.vscode/launch.json`

Practical usage:

1. Run `build solution` once.
2. Launch `Launch TropicalStorms.Api` to debug the API.
3. Run `run NHCParser.Function` in the terminal.
4. Use `Attach to NHCParser.Function` to debug the Function worker.