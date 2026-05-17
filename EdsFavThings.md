# Ed's Favorite Things

Quick operator reference for the TropicalStorms Azure app, API, and SQL firewall.

## Most Common Website Commands

These are the three commands you will use most often for a simple HurricaneSoftware website update.

### 1. Replace the Windows installer zip

```powershell
Copy-Item "D:\GenCode Main Development\www\hurricanesoftware.com\test\setuptte.zip" ".\src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip" -Force
```

### 2. Publish the website

```powershell
dotnet publish .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj -c Release
```

### 3. Deploy the website

```powershell
npx @azure/static-web-apps-cli deploy .\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot --deployment-token "<token>" --app-name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise --env production
```

## HurricaneSoftware Website

### Replace the Windows installer zip

When you build a new Windows installer, overwrite the hosted file here:

```powershell
.\src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip
```

Example from the old website download location:

```powershell
Copy-Item "D:\GenCode Main Development\www\hurricanesoftware.com\test\setuptte.zip" ".\src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip" -Force
```

That file is what the public website serves from:

```powershell
https://www.hurricanesoftware.com/downloads/setuptte.zip
```

### OnlyDeploySetupZip

Short answer: not safely by itself.

Azure Static Web Apps manual deploys publish a folder that represents the site output. There is not a normal "upload just one production file" feature for this setup.

Important note:

- If you try to deploy a folder that contains only `setuptte.zip`, you risk replacing the live site with an incomplete deployment.
- The safe way is still: replace `wwwroot\downloads\setuptte.zip`, publish the website, then deploy the full published `wwwroot` output.

Safe CLI way when only `setuptte.zip` changed:

```powershell
Copy-Item "D:\GenCode Main Development\www\hurricanesoftware.com\test\setuptte.zip" ".\src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip" -Force
dotnet publish .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj -c Release
npx @azure/static-web-apps-cli deploy .\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot --deployment-token "<token>" --app-name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise --env production
```

Portal UI way:

Azure Static Web Apps does not have a normal portal screen where you browse to one file and upload only `setuptte.zip` into production.

Use the portal for the token and verification, then do the actual deploy from your machine:

1. Azure Portal > Static Web Apps > `stapp-hurricanesoftware-cu66c7`
2. On `Overview`, select `Manage deployment token`
3. Copy the deployment token
4. On your machine, replace `setuptte.zip`, publish the website, and run the `npx @azure/static-web-apps-cli deploy ...` command
5. Back in the portal, open the site and verify `/downloads/setuptte.zip`

Bottom line:

- `OnlyDeploySetupZip` is not a safe standalone production deploy method for this Static Web App
- `Replace zip -> publish site -> deploy published wwwroot` is the safe method

### Build the Blazor website

Publishes the static HurricaneSoftware website payload that Azure Static Web Apps serves.

```powershell
dotnet publish .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj -c Release
```

Published files end up under:

```powershell
.\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot
```

### Static Web App details

Current Azure Static Web App:

- Name: `stapp-hurricanesoftware-cu66c7`
- Resource group: `rg-eus2-gencode-enterprise`
- Default hostname: `https://red-ocean-0a1dd550f.7.azurestaticapps.net`

### Get the website deployment token

Use this when you need to deploy the published Blazor files by hand.

```powershell
az staticwebapp secrets list --name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

### Deploy the website to Azure Static Web Apps

After `dotnet publish`, deploy the built `wwwroot` output folder.

```powershell
npx @azure/static-web-apps-cli deploy .\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot --deployment-token "<token>" --app-name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise --env production
```

If you want to pull the deployment token from Azure CLI first:

```powershell
az staticwebapp secrets list --name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

The token is the `properties.apiKey` value.

### Update only the zip file and redeploy

This is the exact CLI sequence when the only thing you changed is the Windows installer zip:

```powershell
Copy-Item "D:\GenCode Main Development\www\hurricanesoftware.com\test\setuptte.zip" ".\src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip" -Force
dotnet build .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj
dotnet publish .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj -c Release
npx @azure/static-web-apps-cli deploy .\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot --deployment-token "<token>" --app-name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise --env production
```

### Portal UI way

Azure Static Web Apps does not give you a simple browser upload button for replacing files in production. The portal UI is mainly used to get the deployment token and verify the deployment.

Portal steps:

1. Azure Portal > Static Web Apps > `stapp-hurricanesoftware-cu66c7`
2. On `Overview`, select `Manage deployment token`
3. Copy the deployment token
4. Back on your machine, run the CLI deploy command above using that token
5. In Azure Portal, return to the Static Web App and verify the site is serving the new file

Portal verification:

1. Azure Portal > Static Web Apps > `stapp-hurricanesoftware-cu66c7`
2. `Overview` shows the default hostname
3. Open the site and test `/downloads/setuptte.zip`
4. If you are using the custom domain, also test `https://www.hurricanesoftware.com/downloads/setuptte.zip`

### Website update checklist

Use this order for simple website-only changes:

```powershell
dotnet build .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj
dotnet publish .\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj -c Release
npx @azure/static-web-apps-cli deploy .\src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot --deployment-token "<token>" --app-name stapp-hurricanesoftware-cu66c7 --resource-group rg-eus2-gencode-enterprise --env production
```

Notes:

- Static files live in `src\HurricaneSoftware.Web\wwwroot`
- The Windows installer now lives at `src\HurricaneSoftware.Web\wwwroot\downloads\setuptte.zip`
- Menu changes are in `src\HurricaneSoftware.Web\Layout\MainLayout.razor`
- Download page content is in `src\HurricaneSoftware.Web\Pages\Download.razor`
- Static Web Apps manual deployments in this repo use `npx @azure/static-web-apps-cli deploy`
- The Azure Portal is used to copy/reset the deployment token and verify the live site, not to directly upload the website files

## App Service Monitoring

### Tail live app logs

Streams live application and web server logs from the Azure Web App.

```powershell
az webapp log tail --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Monitoring > Log stream

### Turn on App Service filesystem logging

Enables application and web server log capture so tailing works.

```powershell
az webapp log config --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --application-logging filesystem --level information --web-server-logging filesystem
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > App Service logs

### Turn off App Service filesystem logging

Disables application and web server filesystem logging when troubleshooting is finished.

```powershell
az webapp log config --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --application-logging off --web-server-logging off
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > App Service logs > Turn both logging options off > Save

### Show basic web app details

Returns hostname, state, runtime, and other top-level app settings.

```powershell
az webapp show --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Overview

### Show app settings

Lists the current app configuration values stored in App Service.

```powershell
az webapp config appsettings list --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Environment variables

### Show access restrictions

Lists the inbound allow or deny rules on the public web app.

```powershell
az webapp config access-restriction show --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Networking > Access restrictions

### Show outbound IPs

Lists the App Service outbound and possible outbound IPs used for SQL firewall rules.

```powershell
az webapp show --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --query "{outboundIpAddresses: outboundIpAddresses, possibleOutboundIpAddresses: possibleOutboundIpAddresses}"
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Properties

## Azure Function Monitoring

### Open simple parser logs

Opens the simple live `[Information]`, `[Verbose]`, and `[Warning]` stream for the timer function.

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Functions > NHCParserTimer > Logs

### Open Function App logs

Opens searchable parser traces and exceptions in Application Insights.

UI:
Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Logs

### Open near-real-time Function telemetry

Use this when you want near-real-time parser activity without waiting on a full log query.

UI:
Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Live metrics

### Important note about Function log streaming

For this Flex Consumption Function App, the simple log view is under the function itself at `NHCParserTimer > Logs`. The old app-level App Service `Log stream` is not the right primary view here.

### Show Function App details

Returns hostname, state, and other top-level Function App settings.

```powershell
az functionapp show --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Overview

### Show Function App settings

Lists the current configuration values stored in the Function App.

```powershell
az functionapp config appsettings list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Settings > Environment variables

### Show Function App functions

Lists the functions currently deployed in the Function App.

```powershell
az functionapp function list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Functions

### Show timer schedule setting

Reads the parser timer schedule from app settings.

```powershell
az functionapp config appsettings list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise --query "[?name=='NHC_TIMER_SCHEDULE']"
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Settings > Environment variables

## SETTINGS

This is the quick-change section for the parser settings you are most likely to touch in Azure.

### Show the main parser settings

Returns the current timer and common parser toggles in one call.

```powershell
az functionapp config appsettings list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise --query "[?name=='NHC_TIMER_SCHEDULE' || name=='NHCParser__CurrentYearOnly' || name=='NHCParser__LogSuccessfulRuns' || name=='NHCParser__ProbeDatabaseOnStartup' || name=='NHCParser__SourceTimeoutSeconds']"
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Settings > Environment variables

### Set parser timer to every 5 minutes

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHC_TIMER_SCHEDULE="0 */5 * * * *"
```

### Set parser timer to every 15 minutes

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHC_TIMER_SCHEDULE="0 */15 * * * *"
```

### Set parser timer to every 1 minute

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHC_TIMER_SCHEDULE="0 */1 * * * *"
```

### Show CurrentYearOnly setting

Reads whether the parser skips persistence for non-current-year advisories.

```powershell
az functionapp config appsettings list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise --query "[?name=='NHCParser__CurrentYearOnly']"
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Settings > Environment variables

### Turn CurrentYearOnly off

Lets the parser persist older advisories, which is useful when testing older storms.

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__CurrentYearOnly=false
```

### Turn CurrentYearOnly on

Restores the normal behavior so non-current-year advisories are skipped.

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__CurrentYearOnly=true
```

### Turn successful run logging on

Makes the parser log successful source processing, which is the closest live "verbose" toggle for parser activity.

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__LogSuccessfulRuns=true
```

### Turn successful run logging off

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__LogSuccessfulRuns=false
```

### Turn database probe on at startup

Runs the advisory URL probe when the Function App starts.

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__ProbeDatabaseOnStartup=true
```

### Turn database probe off at startup

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__ProbeDatabaseOnStartup=false
```

### Set source timeout to 30 seconds

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__SourceTimeoutSeconds=30
```

### Set source timeout to 60 seconds

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHCParser__SourceTimeoutSeconds=60
```

### Restart the Function App after changing settings

Use this if you want the new settings picked up immediately.

```powershell
az functionapp restart --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

### Turn filesystem logging on for live troubleshooting

This is not the primary live-view path for the Flex Function App. Use Application Insights `Logs` or `Live metrics` first.

```powershell
az webapp log config --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise --application-logging filesystem --level information --web-server-logging filesystem
```

### Turn filesystem logging off after troubleshooting

```powershell
az webapp log config --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise --application-logging off --web-server-logging off
```

### Important note about host log verbosity

The Function host log levels themselves currently come from `src/NHCParser.Function/host.json`.
If you want to change `Warning` to `Information` for host categories, that is a code change plus deploy, not just an Azure setting toggle.

## API Smoke Tests

### Smoke test the JSON API

Calls a DB-backed endpoint to confirm the app and database path are working.

```powershell
Invoke-WebRequest 'https://webservice.hurricanesoftware.com/api/tropical-storms/StormNames?username=demo&password=demo&region=All&activeOnly=true' -UseBasicParsing
```

### Smoke test the SOAP WSDL

Confirms the legacy SOAP shim is responding on the public host.

```powershell
Invoke-WebRequest 'https://webservice.hurricanesoftware.com/Services/TropicalStorms.asmx?wsdl' -UseBasicParsing
```

## Azure SQL Firewall

### Show SQL public network access state

Shows whether the SQL logical server is publicly reachable at all.

```powershell
az sql server show --resource-group rg-eus2-gencode-enterprise --name sql-gencode-cu-66c7 --query publicNetworkAccess -o tsv
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Public access

### Turn SQL public network access off

Disables all public SQL access. Private endpoint traffic still works.

```powershell
az sql server update --resource-group rg-eus2-gencode-enterprise --name sql-gencode-cu-66c7 --set publicNetworkAccess=Disabled
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Public access > Public network access = Disabled > Save

### Turn SQL public network access on

Re-enables public SQL access so you can temporarily use firewall rules for direct admin access.

```powershell
az sql server update --resource-group rg-eus2-gencode-enterprise --name sql-gencode-cu-66c7 --set publicNetworkAccess=Enabled
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Public access > Public network access = Enabled > Save

### List SQL firewall rules

Shows every allowed IP entry on the SQL server.

```powershell
az sql server firewall-rule list --resource-group rg-eus2-gencode-enterprise --server sql-gencode-cu-66c7 -o table
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules

### Show one firewall rule

Returns a single firewall entry if you want to verify one rule by name.

```powershell
az sql server firewall-rule show --resource-group rg-eus2-gencode-enterprise --server sql-gencode-cu-66c7 --name HurricaneSoftware
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules

### Add one firewall rule

Allows one specific public IP to connect to the SQL server.

```powershell
az sql server firewall-rule create --resource-group rg-eus2-gencode-enterprise --server sql-gencode-cu-66c7 --name MyNewRule --start-ip-address 1.2.3.4 --end-ip-address 1.2.3.4
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules > Add your client IPv4 address or Add firewall rule

### Update one firewall rule

Changes the start or end IP for an existing firewall entry.

```powershell
az sql server firewall-rule update --resource-group rg-eus2-gencode-enterprise --server sql-gencode-cu-66c7 --name MyNewRule --start-ip-address 5.6.7.8 --end-ip-address 5.6.7.8
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules > Edit the rule > Save

### Remove one firewall rule

Deletes a specific allowed IP entry from the SQL server.

```powershell
az sql server firewall-rule delete --resource-group rg-eus2-gencode-enterprise --server sql-gencode-cu-66c7 --name MyNewRule
```

UI:
Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules > Delete the rule > Save

## Current Known SQL Firewall Entries

- `HurricaneSoftware` = `184.73.224.130`
- `LocalClientIp` = `47.197.47.39`
- no `TropicalStormsApp-xxx` rules should be needed after the API is on the private SQL path

## Current SQL Network Posture

- parser Function App `func-nhcparser-flex-cu66c7` uses VNet integration on `vnet-nhcparser-cu66c7`
- API App Service `api-tropicalstorms-linux-cu66c7` uses VNet integration on `vnet-nhcparser-cu66c7`
- the VNet `vnet-nhcparser-cu66c7` currently encompasses three subnets:
- `snet-nhcparser-functions` = parser Function App integration subnet
- `snet-nhcparser-private-endpoints` = SQL private endpoint subnet
- `snet-tropicalstorms-api-linux` = API App Service integration subnet
- SQL private endpoint DNS zone is `privatelink.database.windows.net`
- temporary public SQL admin access is controlled from Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Public access
- when direct admin work is finished, preferred steady state is `Public network access = Disabled`

## Useful Portal Pages

- App Service logs: Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Monitoring > Log stream
- Simple parser logs: Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Functions > NHCParserTimer > Logs
- Function App logs: Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Logs
- Function App live telemetry: Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Live metrics
- App Service configuration: Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Environment variables
- App Service access restrictions: Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Networking > Access restrictions
- SQL firewall rules: Azure Portal > SQL servers > sql-gencode-cu-66c7 > Networking > Firewall rules

## Deploymment

### Show API deployment history

Lists recent deployments for the web app.

```powershell
az webapp deployment list --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Deployment Center

### Deploy the API to Azure

Use this after changing the API or shared API-facing code such as `TTEBusiness.Core`.

```powershell
.\scripts\deploy-tropicalstorms-api.ps1
```

Validate the deploy prerequisites without publishing:

```powershell
.\scripts\deploy-tropicalstorms-api.ps1 -ValidateOnly
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Deployment Center

### Deploy only the Function code to Azure

Use this after parser code changes, including when NHC changes the source document format and `NHCParser.Function` or `NHCParser.Core` needs updates.

```powershell
.\scripts\deploy-azure-stack.ps1 -SkipSql -SkipFunctionInfra
```

Validate the deploy prerequisites without publishing:

```powershell
.\scripts\deploy-azure-stack.ps1 -ValidateOnly
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Deployment Center

### Deploy the full Function stack

Use this if you need to recreate or update the Function infrastructure plus publish the Function code.

```powershell
.\scripts\deploy-azure-stack.ps1
```

Useful flags:

```powershell
.\scripts\deploy-azure-stack.ps1 -SkipSql
.\scripts\deploy-azure-stack.ps1 -SkipFunctionInfra
.\scripts\deploy-azure-stack.ps1 -SkipFunctionPublish
.\scripts\deploy-azure-stack.ps1 -SkipBuild
```

### Change the Function timer in Azure

Use this when you want to change how often the parser runs without redeploying code.

```powershell
az functionapp config appsettings set --resource-group rg-eus2-gencode-enterprise --name func-nhcparser-flex-cu66c7 --settings NHC_TIMER_SCHEDULE="0 */5 * * * *"
```

Common schedules:

- every 5 minutes: `0 */5 * * * *`
- every 1 minute: `0 */1 * * * *`
- every 15 minutes: `0 */15 * * * *`
- every hour: `0 0 * * * *`

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Settings > Environment variables

### Restart the Function App after config changes

Use this after changing timer settings or other app settings if you want the change to apply immediately.

```powershell
az functionapp restart --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

UI:
Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Overview > Restart

### Tail logs right after deploy

Use these after publishing to confirm startup and DB connectivity.

```powershell
az webapp log tail --name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

For the parser Function App after deploy, use:

- Azure Portal > Function App > func-nhcparser-flex-cu66c7 > Functions > NHCParserTimer > Logs
- Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Live metrics
- Azure Portal > Application Insights > func-nhcparser-flex-cu66c7 > Logs

### Production custom domain binding

The production custom domain now belongs on the Linux App Service.

Add the hostname binding with Azure CLI:

```powershell
az webapp config hostname add --webapp-name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --hostname webservice.hurricanesoftware.com
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Custom domains > Add custom domain

After the hostname is present, bind the managed certificate with SNI SSL in the portal.

Quick production checks:

```powershell
Invoke-WebRequest 'https://webservice.hurricanesoftware.com/api/tropical-storms/HelloWorld' -UseBasicParsing
Invoke-WebRequest 'https://webservice.hurricanesoftware.com/Services/TropicalStorms.asmx?wsdl' -UseBasicParsing
```

Remove the hostname binding if you ever need to detach the custom domain:

```powershell
az webapp config hostname delete --webapp-name api-tropicalstorms-linux-cu66c7 --resource-group rg-eus2-gencode-enterprise --hostname webservice.hurricanesoftware.com
```

UI:
Azure Portal > App Services > api-tropicalstorms-linux-cu66c7 > Settings > Custom domains > Remove
