# How To Deploy

Use only the section you need.

Generated deployment zip files now go under `scripts\tempzip` so they stay out of git and can be deleted cleanly.

## Most common cases

If you changed a button color, text, spacing, layout, image, or page in the website only, do this and nothing else:

```powershell
.\scripts\build-deploy-web.ps1
```

If you changed API code only, do this:

```powershell
.\scripts\build-deploy-api.ps1
```

If you changed parser code only, do this:

```powershell
.\scripts\build-deploy-function.ps1
```

If you changed all 3 code apps together, do this:

```powershell
.\scripts\build-deploy-all.ps1
```

If you changed Terraform only, do this:

```powershell
cd .\terraform
terraform apply
```

The live Azure resources in this repo are split up like this:

- Website: Azure Static Web App `stapp-hurricanesoftware-cu66c7`
- Web Service: Azure App Service `api-tropicalstorms-linux-cu66c7`
- Parser: Azure Function App `func-nhcparser-flex-cu66c7`
- SQL: Azure SQL Server `sql-gencode-cu-66c7`
- Resource group: `rg-eus2-gencode-enterprise`

## Before you deploy anything

From the repo root:

```powershell
az login
```

If the deploy script uses `.env`, make sure `.env` exists and has the values you need.

---

## Deploy Website

Use this when you changed `src/HurricaneSoftware.Web`.

If you changed something small like a button color, page text, or spacing, this is the section you want.

### Easiest way

From the repo root:

```powershell
.\scripts\build-deploy-web.ps1
```

This script builds the website and deploys it to the live Static Web App.

If you only want to build:

```powershell
.\scripts\build-deploy-web.ps1 -BuildOnly
```

By default the script fetches the deployment token from Azure CLI. If you want to pass one yourself:

```powershell
.\scripts\build-deploy-web.ps1 -DeploymentToken "<paste token here>"
```

### Azure portal way

The portal is used to get the deploy token and verify the site. It is not the easiest place to upload the full site.

1. Go to Azure Portal.
2. Open Static Web Apps.
3. Open `stapp-hurricanesoftware-cu66c7`.
4. On Overview, choose `Manage deployment token`.
5. Copy the token.
6. Run the `npx @azure/static-web-apps-cli deploy ...` command above on your machine.
7. In the portal, open the site and verify it loaded.

### Quick check after deploy

Test:

- `https://www.hurricanesoftware.com`
- `https://red-ocean-0a1dd550f.7.azurestaticapps.net`

You do not need to redeploy the API or parser for a website-only visual change.

---

## Deploy Web Service

Use this when you changed `src/TropicalStorms.Api`.

### Easiest way

From the repo root:

```powershell
.\scripts\build-deploy-api.ps1
```

This is the simple code deploy path for API changes.

If you only want to build:

```powershell
.\scripts\build-deploy-api.ps1 -BuildOnly
```

If you need the full infrastructure-aware deploy script instead:

```powershell
.\scripts\deploy-tropicalstorms-api.ps1
```

The simple script does this:

- builds and publishes the API
- zip deploys it to `api-tropicalstorms-linux-cu66c7`

### Azure portal way

The script is still the simplest deploy path.

The portal is useful for checking or changing settings after deploy:

1. Go to Azure Portal.
2. Open App Services.
3. Open `api-tropicalstorms-linux-cu66c7`.
4. Open `Environment variables`.
5. Check the `TropicalStorms__Website__AcsEmail__*` settings if needed.
6. Use `Restart` if you changed settings manually.

### Quick check after deploy

Test:

- `https://webservice.hurricanesoftware.com/api/website/registration/recover/acs`
- `https://api-tropicalstorms-linux-cu66c7.azurewebsites.net/api/website/registration/recover/acs`

If you want a quick live POST test:

```powershell
Invoke-RestMethod -Uri 'https://api-tropicalstorms-linux-cu66c7.azurewebsites.net/api/website/registration/recover/acs' -Method POST -ContentType 'application/json' -Body '{"email":"escott@gencode.com"}'
```

---

## Deploy Parser

Use this when you changed `src/NHCParser.Function` or `src/NHCParser.Core`.

### Easiest way

From the repo root:

```powershell
.\scripts\build-deploy-function.ps1
```

This is the simple code deploy path for parser changes.

If you only want to build:

```powershell
.\scripts\build-deploy-function.ps1 -BuildOnly
```

If you also changed parser infrastructure:

```powershell
.\scripts\deploy-azure-stack.ps1 -SkipSql
```

If this is a first-time parser deployment:

```powershell
.\scripts\deploy-azure-stack.ps1
```

### Azure portal way

The script is the easiest deploy path.

The portal is useful for checking the app and changing timer settings:

1. Go to Azure Portal.
2. Open Function App.
3. Open `func-nhcparser-flex-cu66c7`.
4. Open `Environment variables` or `Configuration`.
5. Change `NHC_TIMER_SCHEDULE` if needed.
6. Restart the Function App if you changed settings manually.
7. Open `Functions` and confirm `NHCParserTimer` is present.

### Quick check after deploy

```powershell
az functionapp function list --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise -o table
az functionapp scale config show --name func-nhcparser-flex-cu66c7 --resource-group rg-eus2-gencode-enterprise
```

---

## Deploy SQL

Use this when you changed DACPACs or need to recreate the Azure SQL database.

### Easiest way

From the repo root:

```powershell
.\scripts\deploy-azure-sql.ps1
```

If you only want to validate first:

```powershell
.\scripts\deploy-azure-sql.ps1 -ValidateOnly
```

This creates or updates the Azure SQL server and publishes the DACPACs.

### Azure portal way

The portal is good for firewall changes, not for the full DACPAC deploy flow.

If your public IP changed:

1. Go to Azure Portal.
2. Open SQL servers.
3. Open `sql-gencode-cu-66c7`.
4. Open `Networking`.
5. Update the `ClientIp` firewall rule.
6. Save.

### Quick check after deploy

```powershell
.\scripts\update-firewall-ip.ps1
```

---

## Deploy Terraform Infrastructure

Use this when you changed files in `terraform`.

### Easiest way

From the `terraform` folder:

```powershell
cd .\terraform
terraform init
terraform validate
terraform plan
terraform apply
```

### Azure portal way

Terraform changes should be applied from Terraform, not by clicking around in the portal.

Use the portal after `terraform apply` to verify the resources were created or updated.

### What Terraform controls here

- Azure SQL
- parser Function App infrastructure
- website Static Web App infrastructure
- ACS email infrastructure for website email

---

## Common simple choices

If you only changed the website:

```powershell
.\scripts\build-deploy-web.ps1
```

Example: button color change, text change, image change, CSS change, page layout change.

If you only changed the API:

```powershell
.\scripts\build-deploy-api.ps1
```

Example: endpoint change, PayPal change, registration/email logic change.

If you only changed the parser code:

```powershell
.\scripts\build-deploy-function.ps1
```

Example: parser logic change, NHC format fix, timer function code change.

If you changed all 3 code apps:

```powershell
.\scripts\build-deploy-all.ps1
```

If you changed Terraform only:

```powershell
cd .\terraform
terraform apply
```

Example: new Azure resource, subnet change, ACS resource change, SQL infra change.