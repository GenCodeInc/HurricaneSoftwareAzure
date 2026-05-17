# Azure SQL deployment for DACPACs

This workspace still contains three DACPACs in `DAC Packages`:

- `GenCode.dacpac`
- `Push.dacpac`
- `TTE.dacpac`

Only `TTE` is part of the live Azure SQL footprint now. `GenCode` and `Push` remain in the repo as archive DACPACs unless you decide to remove them later.

## Local .NET 8 migration scaffold

The new NHC parser migration work now also lives directly in this workspace so you can test it here before moving it elsewhere later.

- solution: `NHCParser.Azure.sln`
- reusable library: `src/TTEBusiness.Core`
- Azure Functions app: `src/NHCParser.Function`
- Web API host: `src/TropicalStorms.Api`
- standalone public website front-end: `src/HurricaneSoftware.Web`
- website-originated email now runs on Azure Communication Services for lost-registration, contact, and order confirmations

The old SMTP attempt is no longer part of the supported deployment path.

Build from the repo root:

```powershell
dotnet build .\NHCParser.Azure.sln
```

The included PowerShell script creates an Azure SQL logical server in the configured resource group, defaults to the `TTE` database only, and publishes the selected DACPACs with `SqlPackage`.

## Cheapest practical hosting choice

For this workload, the lowest-friction low-cost option is a single Azure SQL logical server with one `TTE` database on the `Basic` service objective and `Local` backup storage redundancy.

- No failover group is created.
- No zone redundancy is enabled.
- Azure SQL still includes built-in backups; they cannot be fully disabled.

If these databases will sit idle for long stretches and you want to optimize harder for pause-and-resume cost, you can switch the script to a serverless SKU later. For now, `Basic` is the simplest cheap baseline.

## Important limitation

`DACPAC` deploys schema and programmable objects. It does not move table row data. If your AWS export needs the actual records copied over, you will also need one of these:

- `BACPAC` files and `az sql db import`
- flat-file exports plus a load process
- SQL insert scripts or ETL

## Prerequisites

Install these on the machine where you run the deployment:

1. Azure CLI
2. SqlPackage

Then sign in:

```powershell
az login
```

## Configure

1. Copy values from `.env.example` into `.env`.
2. Set a globally unique value for `AZURE_SQL_SERVER_NAME`.
3. Set `AZURE_SQL_ADMIN_USER` and `AZURE_SQL_ADMIN_PASSWORD`.

## Validate only

This checks env values, Azure CLI access, SqlPackage discovery, and DACPAC presence without creating resources:

```powershell
.\scripts\deploy-azure-sql.ps1 -ValidateOnly
```

## Deploy

```powershell
.\scripts\deploy-azure-sql.ps1
```

To deploy additional archived DACPACs explicitly:

```powershell
.\scripts\deploy-azure-sql.ps1 -DatabaseNames TTE,GenCode,Push
```

## Deploy the full Azure stack

If you want to recreate the whole setup in another subscription from this repo, including Azure SQL, DACPAC publish, Function App infrastructure, code publish, and live log configuration, use:

```powershell
.\scripts\deploy-azure-stack.ps1
```

This script reuses the SQL deployment already in the repo, deploys the Function App infrastructure with Azure CLI and Bicep, publishes `src/NHCParser.Function`, and enables the cheap live log stream path.

Validate everything without creating resources:

```powershell
.\scripts\deploy-azure-stack.ps1 -ValidateOnly
```

Useful flags:

```powershell
.\scripts\deploy-azure-stack.ps1 -SkipSql
.\scripts\deploy-azure-stack.ps1 -SkipFunctionInfra
.\scripts\deploy-azure-stack.ps1 -SkipFunctionPublish
.\scripts\deploy-azure-stack.ps1 -SkipBuild
```

For the public website API host and its ACS email settings, use:

```powershell
.\scripts\deploy-tropicalstorms-api.ps1
```

If your `.env` contains `AZURE_WEBSITE_ACS_COMMUNICATION_SERVICE_NAME` plus `AZURE_WEBSITE_ACS_SENDER_ADDRESS`, the API deployment script will also wire the website ACS settings into the live App Service.
The same deployment script now deletes the old `TropicalStorms__Email__*` App Service settings so the abandoned SMTP path is removed cleanly.

## Optional flags

Skip DACPAC publish and only create the Azure SQL server and databases:

```powershell
.\scripts\deploy-azure-sql.ps1 -SkipPublish
```

Skip firewall rule creation:

```powershell
.\scripts\deploy-azure-sql.ps1 -SkipFirewall
```

Pass an explicit SqlPackage path:

```powershell
.\scripts\deploy-azure-sql.ps1 -SqlPackagePath "C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe"
```

## Firewall changes when your IP changes

The live server created here is:

- resource group: `<resource group>`
- SQL server: `<sql server>`

Current live firewall posture:

- the API and parser reach SQL through VNet integration plus the SQL private endpoint
- only explicitly named machine IPs should remain on the public SQL firewall
- broad Azure-wide access has been removed

This is the cheapest minimal lock-down that still lets you connect directly from your own machine.

Important for the future website/backend:

- the current Azure API and parser already use the private SQL path
- future Azure backends should join the same VNet/private-endpoint pattern instead of adding more public SQL firewall IP rules

For one-time data loading from a source SQL Server into Azure `TTE`, there is also a helper script in this repo:

```powershell
.\scripts\import-local-tte-to-azure.ps1 -TargetPassword "<pwd>"
```

Run that on the source server that can reach `localhost` for the source `TTE` database and also reach Azure SQL.

To attempt a one-by-one stored procedure migration from local `TTE` to Azure `TTE` and log Azure-incompatible failures:

```powershell
.\scripts\import-local-tte-procedures-to-azure.ps1 -TargetPassword "<pwd>"
```

If your public IP changes, update the `ClientIp` firewall rule with these exact commands.

Fastest repeatable option from this repo:

```powershell
.\scripts\update-firewall-ip.ps1
```

That script detects your current public IP, updates the `ClientIp` firewall rule, and prints the final rule list.

### In the Azure portal UI

Use this click path in the portal:

1. Go to `https://portal.azure.com`.
2. Open `Resource groups`.
3. Open `<resource group>`.
4. Open the SQL server `<sql server>`.
5. In the left menu, open `Networking`.
6. In the firewall section, find the `ClientIp` rule.

When your public IP changes:

1. Edit or delete the existing `ClientIp` rule.
2. Add a rule named `ClientIp`.
3. Set both `Start IP` and `End IP` to your current public IP.
4. Click `Save`.

To find your current public IP:

1. Open a browser tab.
2. Search `what is my ip`.
3. Copy the IPv4 address shown.
4. Paste that value into both `Start IP` and `End IP`.

If you want Azure-hosted services to keep being allowed:

1. Leave `Allow Azure services and resources to access this server` turned on.

If you want to lock the server down more tightly:

1. Turn off `Allow Azure services and resources to access this server`.
2. Keep only your `ClientIp` rule.
3. Click `Save`.

After saving, test your connection again from your current machine.

### In Azure CLI

First, make sure you are signed in and on the right subscription:

```powershell
az account set --subscription <sub id>
```

List the current firewall rules:

```powershell
az sql server firewall-rule list --resource-group <resource group> --server <sql server> -o table
```

Get your current public IP:

```powershell
(Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
```

Replace the current `ClientIp` rule with your new IP:

```powershell
$ip = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
az sql server firewall-rule create --resource-group <resource group> --server <sql server> --name ClientIp --start-ip-address $ip --end-ip-address $ip
```

That command is safe to rerun. If the rule already exists, Azure updates it.

You can do the same thing with the helper script:

```powershell
.\scripts\update-firewall-ip.ps1
```

If you want to supply the IP explicitly:

```powershell
.\scripts\update-firewall-ip.ps1 -SubscriptionId "<sub id>" -ResourceGroupName "<resource group>" -SqlServerName "<sql server>" -IpAddress "<your public ip>"
```

If you want to remove your client IP rule entirely:

```powershell
az sql server firewall-rule delete --resource-group <resource group> --server <sql server> --name ClientIp
```

If you want to stop allowing broad Azure-hosted access, delete the `AllowAzureServices` rule:

```powershell
az sql server firewall-rule delete --resource-group <resource group> --server <sql server> --name AllowAzureServices
```

If you later want to restore Azure-hosted access:

```powershell
az sql server firewall-rule create --resource-group <resource group> --server <sql server> --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

## Public SQL access on and off

The current preferred posture for this workspace is:

- the NHC parser Function App and TropicalStorms API reach SQL through VNet integration plus a SQL private endpoint
- temporary direct public SQL access from your machine should be enabled only when needed
- after direct admin work is done, public SQL access should usually be turned back off

### In the Azure portal UI

Use this click path:

1. Go to `https://portal.azure.com`.
2. Open `Resource groups`.
3. Open `<resource group>`.
4. Open the SQL server `<sql server>`.
5. In the left menu, open `Networking`.
6. Open the `Public access` tab.

To turn public SQL access off completely:

1. Set `Public network access` to `Disabled`.
2. Click `Save`.

To turn public SQL access back on temporarily:

1. Set `Public network access` to `Enabled`.
2. Click `Save`.
3. Add or update a firewall rule for your current public IP.

If you only want to remove your own temporary access while keeping public access enabled for other allowed IPs:

1. Leave `Public network access` as `Enabled`.
2. Remove the `ClientIp` or `LocalClientIp` firewall rule.
3. Click `Save`.

### In Azure CLI

Disable all public SQL access:

```powershell
az sql server update --resource-group <resource group> --name <sql server> --set publicNetworkAccess=Disabled
```

Enable public SQL access again:

```powershell
az sql server update --resource-group <resource group> --name <sql server> --set publicNetworkAccess=Enabled
```

Show the current setting:

```powershell
az sql server show --resource-group <resource group> --name <sql server> --query publicNetworkAccess -o tsv
```

If public access is enabled and you need to allow only your current machine temporarily:

```powershell
$ip = (Invoke-RestMethod -Uri "https://api.ipify.org").Trim()
az sql server firewall-rule create --resource-group <resource group> --server <sql server> --name LocalClientIp --start-ip-address $ip --end-ip-address $ip
```

When you are done, remove that temporary client rule:

```powershell
az sql server firewall-rule delete --resource-group <resource group> --server <sql server> --name LocalClientIp
```

To verify the server is still public and check the final rule set:

```powershell
az sql server show --resource-group <resource group> --name <sql server> --query "{publicNetworkAccess:publicNetworkAccess,fullyQualifiedDomainName:fullyQualifiedDomainName}" -o json
az sql server firewall-rule list --resource-group <resource group> --server <sql server> -o table
```

## Terraform

A reusable Terraform version of this deployment is available in `terraform`.

```powershell
cd terraform
terraform init
terraform plan
terraform apply
```

Terraform now covers both the Azure SQL layer and the Function App infrastructure. The repo-level stack deployment script still handles the actual Function App package publish and live log configuration.

See `terraform/README.md` for the reusable inputs and the optional DACPAC publish step."# HurricaneSoftwareAzure" 
