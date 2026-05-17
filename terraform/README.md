# Terraform Azure SQL deployment

This folder recreates the working Azure SQL shape from this workspace in any Azure subscription:

- one Azure SQL logical server
- one or more Azure SQL databases from the selected `database_names` set
- cheapest practical baseline using `Basic` and `Local` backup redundancy
- optional local DACPAC publish with `SqlPackage`
- optional Azure Functions Flex Consumption infrastructure for the NHC parser
- optional Azure Static Web App infrastructure for the rebuilt HurricaneSoftware public website
- optional Azure Communication Services email infrastructure for website-originated mail

## What it builds

By default this creates:

- a logical server
- optional firewall access for Azure services, disabled by default
- one database: `TTE`

The archived `GenCode` and `Push` DACPACs can still be deployed later by explicitly setting `database_names`, but the live default is now `TTE` only.

When `deploy_function_app = true`, Terraform also creates:

- a storage account for the Function App
- a private blob container used by the Function App deployment package
- a Linux Flex Consumption plan for the Function App, defaulting to `FC1`
- a Log Analytics workspace for workspace-based Application Insights
- a workspace-based Application Insights resource attached to the Function App
- a .NET 8 isolated Azure Function App with the parser app settings wired in
- one `Always Ready` instance for `function:NHCParserTimer` by default
- `512 MB` instances by default to keep the monthly standing cost low
- a dedicated Function integration subnet in the VNet
- a dedicated private-endpoint subnet in the same VNet
- an Azure SQL private endpoint and `privatelink.database.windows.net` private DNS zone

When `deploy_static_web_app = true`, Terraform also creates:

- one separate Azure Static Web App for the public HurricaneSoftware website
- a standalone hostname that can be tested before moving the production CNAME
- Free tier hosting intended for the Blazor WebAssembly front-end in `src/HurricaneSoftware.Web`

When `deploy_website_acs_email = true`, Terraform also creates:

- one ACS Email Service for the public website email flows
- one Azure-managed ACS email domain, already verified by Azure
- one sender username, defaulting to `noreply`
- one linked ACS Communication Service that can provide the connection string used by `TropicalStorms.Api`

For the safer default posture, leave `allow_azure_services = false`, use the private SQL path for Azure apps, and keep `client_ip_address` only for temporary direct admin access.

If `publish_dacpacs = true`, Terraform will also run a local PowerShell publish step after the databases exist.

## Important limitations

- `DACPAC` deploys schema, not table data.
- The publish step uses Azure SQL compatibility workarounds that were required here.
- Stored procedures are excluded by default because one of the DACPACs contains local-file `BULK INSERT` logic that Azure SQL will not allow.
- The current Terraform shape models the parser on VNet integration plus a SQL private endpoint and private DNS zone instead of relying on backend public outbound IP rules.
- Terraform provisions the Function App infrastructure, but the repo-level PowerShell deployment script remains the simpler path for publishing the Function App code package.
- The Flex Consumption resource requires a recent AzureRM provider. This repo now pins `hashicorp/azurerm` to `~> 4.72`.

## Files

- `versions.tf`: provider and Terraform version requirements
- `variables.tf`: reusable input variables
- `main.tf`: resource creation and optional publish hook
- `outputs.tf`: resulting server and database outputs
- Function App outputs are also included when `deploy_function_app = true`
- Static Web App outputs are also included when `deploy_static_web_app = true`
- ACS email outputs are also included when `deploy_website_acs_email = true`
- `terraform.tfvars.example`: example values
- `scripts/publish-dacpacs.ps1`: local `SqlPackage` publish step used by Terraform

## Usage

1. Copy `terraform.tfvars.example` to `terraform.tfvars`.
2. Adjust values for the target subscription.
3. Authenticate with Azure CLI or set ARM environment variables.
4. Run Terraform.

```powershell
cd terraform
terraform init
terraform plan
terraform apply
```

Important:

- run these commands from the `terraform` folder, not the repo root
- if you run `terraform plan` from the repo root, Terraform will not see the `.tf` files in this folder

## Quick start with the example file

The fastest way to get a real plan is:

1. Copy `terraform.tfvars.example` to `terraform.tfvars`.
2. Replace `administrator_password` with a real strong password.
3. Adjust subscription, resource group, region, and names if needed.
4. Run `terraform init`, `terraform validate`, and `terraform plan` from this folder.

```powershell
cd terraform
copy terraform.tfvars.example terraform.tfvars
terraform init
terraform validate
terraform plan
```

If you do not want to create `terraform.tfvars`, you can also plan directly against the example file:

```powershell
cd terraform
terraform plan -input=false -var-file=terraform.tfvars.example
```

## Reuse in another subscription

At minimum update these values:

- `subscription_id`
- `resource_group_name`
- `location`
- `administrator_login`
- `administrator_password`

You can either:

- set `create_resource_group = true` to have Terraform create a new resource group
- keep `create_resource_group = false` to deploy into an existing resource group

## Optional DACPAC publish

To publish the DACPACs during `terraform apply`:

1. Install `SqlPackage` on the machine running Terraform.
2. Set `publish_dacpacs = true`.
3. Keep `dacpac_directory` pointed at the folder containing the `.dacpac` files.
4. Set `database_names` to the exact databases you want to keep in sync.

If you only want infrastructure, leave `publish_dacpacs = false`.

## Function App deployment notes

Set `deploy_function_app = true` to provision the parser Function App infrastructure in the same resource group.

Set `deploy_static_web_app = true` to provision a separate Azure Static Web App for the new Blazor website.

Set `deploy_website_acs_email = true` to provision ACS-backed website email resources for lost-registration, contact, and order emails.

Important website-hosting note:

- this is intentionally separate from the existing API host so current mobile and SOAP users do not share the same public front-end surface
- the Blazor site stays static on Static Web Apps while dynamic registration, contact, alert confirmation, and checkout routes continue to run on the existing `TropicalStorms.Api` host
- the website-originated email flows now run on Azure Communication Services instead of the legacy SMTP path
- you can keep the generated `azurestaticapps.net` hostname for testing and move the production CNAME later

ACS email note:

- Terraform creates the ACS Email Service, Azure-managed domain, sender username, and linked Communication Service
- use the `website_acs_communication_service_name` and `website_acs_sender_address` outputs when you configure `deploy-tropicalstorms-api.ps1` or the live App Service settings
- the current live sender address pattern is `noreply@<azure-managed-domain>.azurecomm.net`
- the repo no longer uses the abandoned `TropicalStorms__Email__*` SMTP settings for the website deployment path

Terraform sets these Function App settings:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`
- `APPINSIGHTS_INSTRUMENTATIONKEY`
- `NHC_TIMER_SCHEDULE`
- `NHCParser__SqlConnectionString`
- `NHCParser__CurrentYearOnly`
- `NHCParser__ProbeDatabaseOnStartup`

Terraform also provisions these Flex Consumption hosting settings by default:

- `FC1` Linux Flex Consumption plan
- `maximum_instance_count = 100`
- `instance_memory_in_mb = 512`
- `always_ready { name = "function:NHCParserTimer", instance_count = 1 }`

Monitoring defaults:

- workspace-based Application Insights
- 30-day Log Analytics retention
- low-noise function logging controlled by `src/NHCParser.Function/host.json`

API note:

- this Terraform folder does not provision a separate Application Insights resource for the TropicalStorms API
- API request telemetry is now opt-in through `APPLICATIONINSIGHTS_CONNECTION_STRING` on the App Service so you can avoid extra ingestion cost unless you actually want endpoint visibility

Important hosting note:

- the parser now targets Flex Consumption instead of a dedicated `B1` plan
- the warm-host behavior now comes from Flex `Always Ready`, not from App Service `Always On`
- keep the default `function:NHCParserTimer = 1` unless you intentionally accept more cold starts
- the TropicalStorms API App Service is managed separately from this Terraform folder and now runs on the Linux app `api-tropicalstorms-linux-cu66c7` behind `https://webservice.hurricanesoftware.com`

For the end-to-end path that also publishes the code package, run the repo-level script instead:

```powershell
.\scripts\deploy-azure-stack.ps1
```

## Recommended low-cost security posture

For the cheapest minimal setup that still lets you connect directly from your own machine:

- set `allow_azure_services = false`
- set `client_ip_address` to your current public IP
- update that IP whenever your public IP changes

For the current live deployment in this repo, there is also a helper script:

```powershell
.\scripts\update-firewall-ip.ps1
```

Run it from the repository root, not from the `terraform` folder.

The current live direction in this repo is the private path option: move Azure apps into VNet integration and use a SQL private endpoint design.