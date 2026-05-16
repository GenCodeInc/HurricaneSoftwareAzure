# NHCParser.Function

This project is the Azure Functions host for the new NHC parser.

It is:

- .NET 8
- Azure Functions v4 isolated worker
- timer-driven
- configured to run every 5 minutes by default
- intended to run on Azure Functions Flex Consumption with one `Always Ready` timer instance when deployed to Azure

This README is the practical guide for using it.

## What this app does

On each timer run, the function:

1. starts the timer function
2. optionally probes the database for advisory URLs
3. downloads each configured NHC source URL
4. classifies the advisory type
5. parses it
6. writes advisory, storm, coordinate, forecast, or points-of-interest data into TTE using the existing stored procedures
7. writes log messages through `ILogger`

It also applies the storm lifecycle rules now used by the live system:

- a final storm advisory keeps the main storm active for 24 hours, then deactivates it
- a `_Forecast` storm stays active until 12 hours after its latest forecast coordinate, then deactivates it
- expired forecast storms are also cleaned up at the end of each parser cycle even if that storm is no longer being parsed

The timer entry point is `NHCParserTimer`.

## The files that matter most

- `Functions/NhcParserTimerFunction.cs`: the timer-triggered Azure Function entry point
- `appsettings.json`: the default parser settings and source URL list
- `local.settings.json`: your local-only runtime settings and secrets
- `host.json`: logging level settings for the Functions host

## Local prerequisites

To run this locally, you need:

1. .NET 8 SDK
2. Azure Functions Core Tools v4
3. Azurite if `AzureWebJobsStorage=UseDevelopmentStorage=true`

If you already ran this repo locally before, you likely already have these.

## Local setup

From `src/NHCParser.Function`:

1. copy `local.settings.json.example` to `local.settings.json`
2. put the real SQL username and password into `NHCParser__SqlConnectionString`
3. make sure Azurite is running if you are using `UseDevelopmentStorage=true`

Example local settings:

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

## Run locally

From `src/NHCParser.Function`:

```powershell
func start
```

You can also build first from the repo root:

```powershell
dotnet build .\NHCParser.Azure.sln
```

When the timer fires, you should see messages like:

- `NHC parser timer started`
- source success or failure logs
- `NHC parser timer completed`

## How to change the timer

The timer schedule is controlled by:

- `NHC_TIMER_SCHEDULE`

The current default is:

```text
0 */5 * * * *
```

That means every 5 minutes.

Common schedules:

- every 5 minutes: `0 */5 * * * *`
- every 1 minute: `0 */1 * * * *`
- every 15 minutes: `0 */15 * * * *`
- every hour: `0 0 * * * *`

### Change the timer locally

Edit `local.settings.json`:

```json
"NHC_TIMER_SCHEDULE": "0 */1 * * * *"
```

Then stop and restart `func start`.

### Change the timer in Azure

Use Azure CLI:

```powershell
az functionapp config appsettings set --resource-group <resource group> --name <function app name> --settings NHC_TIMER_SCHEDULE="0 */5 * * * *"
```

You can also change it in the Azure portal under:

`Function App -> Settings -> Environment variables` or `Configuration`, depending on the portal view.

## How settings work

Configuration is loaded in this order:

1. `appsettings.json`
2. environment variables

That means:

- `appsettings.json` is the default config file
- `local.settings.json` supplies environment variables for local runs
- Azure Function App application settings supply environment variables in Azure

In practice:

- use `appsettings.json` for the default source list
- use `local.settings.json` for local-only overrides and secrets
- use Azure app settings for cloud overrides

## The main parser settings

These settings are under the `NHCParser` section.

- `SourceTimeoutSeconds`: HTTP timeout per source
- `LogSuccessfulRuns`: logs successful source processing when true
- `CurrentYearOnly`: skips persistence for non-current-year advisories when true
- `ProbeDatabaseOnStartup`: runs the advisory URL probe once when the app starts
- `DatabaseProbeMaxUrlsToLog`: how many sample DB advisory URLs to log
- `DatabaseProbeRegions`: which region IDs to probe from the advisory stored procedure
- `Sources`: the list of advisory URLs to fetch and parse

## How to test a single parse URL

The simplest way is to temporarily make the source list contain only one URL and run the timer every minute locally.

### Step 1: edit `appsettings.json`

Replace the `Sources` array with a single source.

Example using one known advisory URL:

```json
"Sources": [
	{
		"Name": "Single Test URL",
		"Url": "https://tgftp.nws.noaa.gov/data/raw/wt/wtpz25.knhc.tcm.ep5.txt",
		"ReportFailures": true
	}
]
```

### Step 2: make the timer run every minute locally

In `local.settings.json`:

```json
"NHC_TIMER_SCHEDULE": "0 */1 * * * *"
```

### Step 3: if you are testing an older advisory, disable the year guard

In `local.settings.json`:

```json
"NHCParser__CurrentYearOnly": "false"
```

This matters for sample advisories from an older season. Without this change, the parser may parse the advisory but skip persistence.

### Step 4: run the function locally

```powershell
func start
```

### Step 5: watch the console output

You should see either:

- a success log for the source
- or an exception with the source name and URL

When you are done testing, put the full source list back and restore the timer to every 5 minutes.

## How to test without touching Azure

For local parser testing, the safest loop is:

1. use one source URL in `appsettings.json`
2. set the timer to every minute in `local.settings.json`
3. set `CurrentYearOnly=false` if the advisory is old
4. run `func start`
5. watch the console logs

That avoids repeated Azure deploys while you are tuning parser behavior.

## Local logs

Local logs appear directly in the terminal where you ran:

```powershell
func start
```

Useful messages include:

- timer started
- advisory database probe results
- per-source success
- per-source failure with URL
- timer completed

## Azure logs

On Flex Consumption, the old dedicated-plan filesystem tail is no longer the primary monitoring path.

The most useful runtime checks are:

```powershell
az functionapp scale config show --name <function app name> --resource-group <resource group>
```

If you need durable searchable runtime logs, add Application Insights to the Function App.

This repo's Azure deployment paths now provision workspace-based Application Insights by default and attach it to the Function App with:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`
- `APPINSIGHTS_INSTRUMENTATIONKEY`

The default host logging is intentionally narrow:

- warnings and errors
- exceptions
- startup/restart signals
- not verbose per-run chatter

## Azure hosting requirement

If you want the timer to keep running reliably when the app would otherwise go idle, do not use a Windows Consumption `Y1` plan.

Use Flex Consumption and keep one `Always Ready` instance for `function:NHCParserTimer`.

This repo's infrastructure defaults were updated for that:

- the Bicep deployment now defaults to a Linux `FC1` Flex plan
- the PowerShell stack deployment script now passes through Flex memory, max-instance, and always-ready settings
- the Terraform deployment now uses `azurerm_function_app_flex_consumption`

If you deliberately switch back to `Y1` or remove `Always Ready`, Azure can cold start the timer host after idle periods.

Important:

- `Monitoring -> Logs` in the Azure portal is not the live console stream
- Flex Consumption does not use the old dedicated-plan `Always On` model
- if the timer is every 5 minutes, you may need to wait until the next `:00`, `:05`, `:10`, `:15`, and so on

If you connect to the log tail and see:

```text
No new trace in the past 1 min(s).
```

that usually just means the next timer run has not happened yet.

## Useful Azure settings to change

These are the most likely settings you will touch:

- `NHC_TIMER_SCHEDULE`
- `NHCParser__CurrentYearOnly`
- `NHCParser__ProbeDatabaseOnStartup`

Example:

```powershell
az functionapp config appsettings set --resource-group <resource group> --name <function app name> --settings NHCParser__CurrentYearOnly=false NHCParser__ProbeDatabaseOnStartup=true
```

## About the source list in Azure

The source list lives under `NHCParser:Sources` in `appsettings.json`.

That means the easiest way to test one URL is usually local testing by editing `appsettings.json` temporarily.

You can override the source list in Azure with environment variables too, but the key names are more awkward because arrays use indexed names such as:

- `NHCParser__Sources__0__Name`
- `NHCParser__Sources__0__Url`
- `NHCParser__Sources__0__ReportFailures`

For one-off parser testing, local editing is simpler and less error-prone.

## What to look for when something fails

If a source fails, the runner logs:

- source name
- source URL
- the exception

Common causes:

- bad URL
- NOAA source temporarily unavailable
- SQL connection string missing or wrong
- `CurrentYearOnly=true` while testing an older advisory
- advisory format changed and parsing needs an update

## Recommended test workflow

If you want to test parser behavior with the least friction:

1. set one URL in `appsettings.json`
2. set `NHC_TIMER_SCHEDULE` to every minute in `local.settings.json`
3. set `NHCParser__CurrentYearOnly=false` if needed
4. run `func start`
5. watch the local console logs
6. restore the normal source list and 5-minute timer after the test

## Current default behavior in this repo

Right now this project is wired to:

- run on a 5-minute timer by default
- load parser sources from `appsettings.json`
- use `ILogger` for logs
- persist into the TTE database through the existing stored procedures
- support live Azure log tail without requiring Application Insights