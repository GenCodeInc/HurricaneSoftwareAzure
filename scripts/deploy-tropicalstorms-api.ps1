[CmdletBinding()]
param(
    [string]$EnvFile = ".env",
    [string]$ResourceGroup = "<resource group>",
    [string]$Location = "centralus",
    [string]$PlanName = "<api plan name>",
    [string]$PlanSku = "B1",
    [string]$WebAppName = "<api app name>",
    [ValidateSet("Windows", "Linux")]
    [string]$PlanOs = "Linux",
    [string]$RuntimeStack = "DOTNETCORE:8.0",
    [string]$VnetName = "<api vnet name>",
    [string]$IntegrationSubnetName = "<api integration subnet name>",
    [string]$IntegrationSubnetAddressPrefix = "10.20.0.96/27",
    [string]$ProjectPath = ".\src\TropicalStorms.Api\TropicalStorms.Api.csproj",
    [switch]$ValidateOnly,
    [switch]$SkipBuild,
    [switch]$SkipLogs,
    [switch]$SkipVnetIntegration,
    [switch]$DisableLegacyShim
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Import-EnvFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Environment file not found: $Path"
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Count -ne 2) {
            throw "Invalid line in ${Path}: $line"
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"')
        Set-Item -Path "Env:$key" -Value $value
    }
}

function Get-OptionalEnv {
    param([string]$Name)

    return [Environment]::GetEnvironmentVariable($Name)
}

function Resolve-ConfigValue {
    param(
        [string]$Value,
        [string]$Placeholder,
        [string]$EnvName,
        [switch]$Required
    )

    if (-not [string]::IsNullOrWhiteSpace($Value) -and $Value -ne $Placeholder) {
        return $Value
    }

    $envValue = Get-OptionalEnv -Name $EnvName
    if (-not [string]::IsNullOrWhiteSpace($envValue)) {
        return $envValue
    }

    if ($Required) {
        throw "Required setting is missing. Provide a parameter value or set $EnvName in $envPath."
    }

    return $Value
}

function Invoke-AzQuiet {
    param([string[]]$Arguments)

    $failed = $false
    try {
        $output = & az @Arguments --only-show-errors 2>&1
    }
    catch {
        $failed = $true
        $output = $_ | Out-String
    }

    if ($failed -or $LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }

    return $output
}

function Invoke-AzJson {
    param([string[]]$Arguments)

    $text = Invoke-AzQuiet -Arguments $Arguments | Out-String
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return ($text.Trim() | ConvertFrom-Json)
}

function Try-Invoke-AzJson {
    param([string[]]$Arguments)

    $failed = $false
    try {
        $output = & az @Arguments --only-show-errors 2>&1
    }
    catch {
        $failed = $true
        $output = $_ | Out-String
    }

    if ($failed -or $LASTEXITCODE -ne 0) {
        $message = ($output | Out-String)
        $normalizedMessage = $message.ToLowerInvariant()
        if ($normalizedMessage.Contains("resourcenotfound") -or
            $normalizedMessage.Contains("was not found") -or
            $normalizedMessage.Contains("not found") -or
            $normalizedMessage.Contains("could not be found")) {
            return $null
        }

        throw "az $($Arguments -join ' ') failed:`n$message"
    }

    $text = ($output | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function New-WebPackage {
    param(
        [string]$WorkspaceRoot,
        [string]$Project,
        [switch]$SkipBuild
    )

    $publishRoot = Join-Path $WorkspaceRoot ".publish"
    $publishDirectory = Join-Path $publishRoot "TropicalStorms.Api"
    $zipPath = Join-Path $publishRoot "TropicalStorms.Api.zip"

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    if (-not $SkipBuild) {
        if (Test-Path -LiteralPath $publishDirectory) {
            Remove-Item -LiteralPath $publishDirectory -Recurse -Force
        }

        New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

        & dotnet publish $Project -c Release -o $publishDirectory | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $Project"
        }
    }
    elseif (-not (Test-Path -LiteralPath $publishDirectory)) {
        throw "SkipBuild was specified but no existing publish output was found at $publishDirectory"
    }

    & tar.exe -a -c -f $zipPath -C $publishDirectory . | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "tar.exe failed while creating $zipPath"
    }

    return $zipPath
}

function Get-SqlServerNameFromConnectionString {
    param([string]$ConnectionString)

    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        return $null
    }

    $match = [regex]::Match($ConnectionString, "(?:Server|Data Source)=tcp:(?<server>[^,;]+)", [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($match.Success) {
        return $match.Groups["server"].Value
    }

    return $null
}

function New-SqlConnectionStringFromEnvironment {
    $serverName = [Environment]::GetEnvironmentVariable("AZURE_SQL_SERVER_NAME")
    $adminUser = [Environment]::GetEnvironmentVariable("AZURE_SQL_ADMIN_USER")
    $adminPassword = [Environment]::GetEnvironmentVariable("AZURE_SQL_ADMIN_PASSWORD")
    $databaseName = [Environment]::GetEnvironmentVariable("AZURE_TTE_SQL_DATABASE_NAME")

    if ([string]::IsNullOrWhiteSpace($databaseName)) {
        $databaseName = "TTE"
    }

    if ([string]::IsNullOrWhiteSpace($serverName) -or [string]::IsNullOrWhiteSpace($adminUser) -or [string]::IsNullOrWhiteSpace($adminPassword)) {
        return $null
    }

    if (-not $serverName.EndsWith(".database.windows.net", [System.StringComparison]::OrdinalIgnoreCase)) {
        $serverName = "$serverName.database.windows.net"
    }

    return "Server=tcp:$serverName,1433;Initial Catalog=$databaseName;Persist Security Info=False;User ID=$adminUser;Password=$adminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

function Ensure-IntegrationSubnet {
    param(
        [string]$ResourceGroup,
        [string]$VnetName,
        [string]$SubnetName,
        [string]$AddressPrefix
    )

    $subnet = Try-Invoke-AzJson -Arguments @(
        "network", "vnet", "subnet", "show",
        "--resource-group", $ResourceGroup,
        "--vnet-name", $VnetName,
        "--name", $SubnetName,
        "-o", "json"
    )

    if (-not $subnet) {
        Write-Step "Creating integration subnet $SubnetName"
        Invoke-AzQuiet -Arguments @(
            "network", "vnet", "subnet", "create",
            "--resource-group", $ResourceGroup,
            "--vnet-name", $VnetName,
            "--name", $SubnetName,
            "--address-prefixes", $AddressPrefix,
            "--delegations", "Microsoft.Web/serverFarms",
            "-o", "json"
        ) | Out-Null

        return
    }

    $delegations = @($subnet.delegations | ForEach-Object { $_.serviceName })
    if ($delegations.Count -gt 0 -and "Microsoft.Web/serverFarms" -notin $delegations) {
        throw "Subnet $SubnetName is delegated to $($delegations -join ', ') instead of Microsoft.Web/serverFarms."
    }
}

function Ensure-WebAppPrivateSqlPath {
    param(
        [string]$ResourceGroup,
        [string]$WebAppName,
        [string]$VnetName,
        [string]$SubnetName,
        [string]$SubnetAddressPrefix
    )

    Ensure-IntegrationSubnet -ResourceGroup $ResourceGroup -VnetName $VnetName -SubnetName $SubnetName -AddressPrefix $SubnetAddressPrefix

    $existingIntegrations = @(Invoke-AzJson -Arguments @(
        "webapp", "vnet-integration", "list",
        "--resource-group", $ResourceGroup,
        "--name", $WebAppName,
        "-o", "json"
    ))

    $existingIntegrationNames = @(
        $existingIntegrations |
            ForEach-Object {
                if ($null -ne $_ -and $_.PSObject.Properties["name"]) {
                    $_.PSObject.Properties["name"].Value
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )

    if ($existingIntegrationNames -notcontains $SubnetName) {
        Write-Step "Adding web app VNet integration on $SubnetName"
        Invoke-AzQuiet -Arguments @(
            "webapp", "vnet-integration", "add",
            "--resource-group", $ResourceGroup,
            "--name", $WebAppName,
            "--vnet", $VnetName,
            "--subnet", $SubnetName,
            "-o", "json"
        ) | Out-Null
    }

    Invoke-AzQuiet -Arguments @(
        "resource", "update",
        "--resource-group", $ResourceGroup,
        "--resource-type", "Microsoft.Web/sites",
        "--name", $WebAppName,
        "--set", "properties.vnetRouteAllEnabled=true",
        "-o", "json"
    ) | Out-Null
}

function Get-PlanOsKind {
    param($Plan)

    if ($null -eq $Plan) {
        return $null
    }

    $reservedProperty = $Plan.PSObject.Properties["reserved"]
    if ($null -ne $reservedProperty -and $reservedProperty.Value -eq $true) {
        return "Linux"
    }

    $kindProperty = $Plan.PSObject.Properties["kind"]
    if ($null -ne $kindProperty -and (($kindProperty.Value | Out-String) -match "linux")) {
        return "Linux"
    }

    return "Windows"
}

function Get-WebAppOsKind {
    param($WebApp)

    if ($null -eq $WebApp) {
        return $null
    }

    $kindProperty = $WebApp.PSObject.Properties["kind"]
    if ($null -ne $kindProperty -and (($kindProperty.Value | Out-String) -match "linux")) {
        return "Linux"
    }

    return "Windows"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $workspaceRoot $EnvFile }

Write-Step "Loading environment from $envPath"
Import-EnvFile -Path $envPath

$ResourceGroup = Resolve-ConfigValue -Value $ResourceGroup -Placeholder "<resource group>" -EnvName "AZURE_RESOURCE_GROUP" -Required
$PlanName = Resolve-ConfigValue -Value $PlanName -Placeholder "<api plan name>" -EnvName "AZURE_API_PLAN_NAME" -Required
$WebAppName = Resolve-ConfigValue -Value $WebAppName -Placeholder "<api app name>" -EnvName "AZURE_API_APP_NAME" -Required
$VnetName = Resolve-ConfigValue -Value $VnetName -Placeholder "<api vnet name>" -EnvName "AZURE_API_VNET_NAME" -Required
$IntegrationSubnetName = Resolve-ConfigValue -Value $IntegrationSubnetName -Placeholder "<api integration subnet name>" -EnvName "AZURE_API_INTEGRATION_SUBNET_NAME" -Required

$sqlConnectionString = [Environment]::GetEnvironmentVariable("NHCParser__SqlConnectionString")
if ([string]::IsNullOrWhiteSpace($sqlConnectionString)) {
    $sqlConnectionString = [Environment]::GetEnvironmentVariable("AZURE_TTE_SQL_CONNECTION_STRING")
}
if ([string]::IsNullOrWhiteSpace($sqlConnectionString)) {
    $sqlConnectionString = [Environment]::GetEnvironmentVariable("ConnectionStrings__TTE")
}
if ([string]::IsNullOrWhiteSpace($sqlConnectionString)) {
    $sqlConnectionString = New-SqlConnectionStringFromEnvironment
}
if ([string]::IsNullOrWhiteSpace($sqlConnectionString)) {
    throw "No SQL connection string found. Set NHCParser__SqlConnectionString, AZURE_TTE_SQL_CONNECTION_STRING, or ConnectionStrings__TTE in $envPath, or provide AZURE_SQL_SERVER_NAME, AZURE_SQL_ADMIN_USER, and AZURE_SQL_ADMIN_PASSWORD."
}

$sqlServerName = Get-SqlServerNameFromConnectionString -ConnectionString $sqlConnectionString
if (-not [string]::IsNullOrWhiteSpace($sqlServerName) -and $sqlServerName.EndsWith('.database.windows.net', [System.StringComparison]::OrdinalIgnoreCase)) {
    $sqlServerName = $sqlServerName.Substring(0, $sqlServerName.IndexOf('.'))
}

$applicationInsightsConnectionString = [Environment]::GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")
if ([string]::IsNullOrWhiteSpace($applicationInsightsConnectionString)) {
    $applicationInsightsConnectionString = [Environment]::GetEnvironmentVariable("ApplicationInsights__ConnectionString")
}

$websiteAcsConnectionString = [Environment]::GetEnvironmentVariable("AZURE_WEBSITE_ACS_CONNECTION_STRING")
if ([string]::IsNullOrWhiteSpace($websiteAcsConnectionString)) {
    $websiteAcsConnectionString = [Environment]::GetEnvironmentVariable("TROPICALSTORMS_WEBSITE_ACS_CONNECTION_STRING")
}

$websiteAcsCommunicationServiceName = [Environment]::GetEnvironmentVariable("AZURE_WEBSITE_ACS_COMMUNICATION_SERVICE_NAME")
$websiteAcsSenderAddress = [Environment]::GetEnvironmentVariable("AZURE_WEBSITE_ACS_SENDER_ADDRESS")
if ([string]::IsNullOrWhiteSpace($websiteAcsSenderAddress)) {
    $websiteAcsSenderAddress = [Environment]::GetEnvironmentVariable("TROPICALSTORMS_WEBSITE_ACS_SENDER_ADDRESS")
}

$websiteAcsSenderDisplayName = [Environment]::GetEnvironmentVariable("AZURE_WEBSITE_ACS_SENDER_DISPLAY_NAME")
if ([string]::IsNullOrWhiteSpace($websiteAcsSenderDisplayName)) {
    $websiteAcsSenderDisplayName = [Environment]::GetEnvironmentVariable("TROPICALSTORMS_WEBSITE_ACS_SENDER_DISPLAY_NAME")
}
if ([string]::IsNullOrWhiteSpace($websiteAcsSenderDisplayName)) {
    $websiteAcsSenderDisplayName = "Tracking The Eye"
}

$websiteAcsAdminAddress = [Environment]::GetEnvironmentVariable("AZURE_WEBSITE_ACS_ADMIN_ADDRESS")
if ([string]::IsNullOrWhiteSpace($websiteAcsAdminAddress)) {
    $websiteAcsAdminAddress = [Environment]::GetEnvironmentVariable("TROPICALSTORMS_WEBSITE_ACS_ADMIN_ADDRESS")
}
if ([string]::IsNullOrWhiteSpace($websiteAcsAdminAddress)) {
    $websiteAcsAdminAddress = "www@gencode.com"
}

if ([string]::IsNullOrWhiteSpace($websiteAcsConnectionString) -and -not [string]::IsNullOrWhiteSpace($websiteAcsCommunicationServiceName)) {
    $communicationExtension = Try-Invoke-AzJson -Arguments @("extension", "show", "--name", "communication", "-o", "json")
    if (-not $communicationExtension) {
        Invoke-AzQuiet -Arguments @("config", "set", "extension.dynamic_install_allow_preview=true") | Out-Null
        Invoke-AzQuiet -Arguments @("extension", "add", "--name", "communication", "--allow-preview", "true", "--yes") | Out-Null
    }

    $communicationKeys = Invoke-AzJson -Arguments @(
        "communication", "list-key",
        "--resource-group", $ResourceGroup,
        "--name", $websiteAcsCommunicationServiceName,
        "-o", "json"
    )

    $websiteAcsConnectionString = $communicationKeys.primaryConnectionString
}

Write-Step "Checking Azure access"
Invoke-AzQuiet -Arguments @("account", "show", "-o", "json") | Out-Null

$plan = Try-Invoke-AzJson -Arguments @("appservice", "plan", "show", "--resource-group", $ResourceGroup, "--name", $PlanName, "-o", "json")
if (-not $plan) {
    if ($ValidateOnly) {
        Write-Host "Plan would be created: $PlanName ($PlanSku, $PlanOs)"
    }
    else {
        Write-Step "Creating App Service plan $PlanName"
        $planCreateArguments = @("appservice", "plan", "create", "--resource-group", $ResourceGroup, "--name", $PlanName, "--location", $Location, "--sku", $PlanSku)
        if ($PlanOs -eq "Linux") {
            $planCreateArguments += "--is-linux"
        }

        Invoke-AzQuiet -Arguments $planCreateArguments
    }
}
elseif ((Get-PlanOsKind -Plan $plan) -ne $PlanOs) {
    throw "Existing plan $PlanName is $((Get-PlanOsKind -Plan $plan)), but PlanOs was set to $PlanOs. Use a different plan name for the parallel deployment."
}

$webApp = Try-Invoke-AzJson -Arguments @("webapp", "show", "--resource-group", $ResourceGroup, "--name", $WebAppName, "-o", "json")
if (-not $webApp) {
    if ($ValidateOnly) {
        Write-Host "Web App would be created: $WebAppName ($PlanOs, $RuntimeStack)"
    }
    else {
        Write-Step "Creating Web App $WebAppName"
        Invoke-AzQuiet -Arguments @("webapp", "create", "--resource-group", $ResourceGroup, "--plan", $PlanName, "--name", $WebAppName, "--runtime", $RuntimeStack)

        $webApp = Invoke-AzJson -Arguments @("webapp", "show", "--resource-group", $ResourceGroup, "--name", $WebAppName, "-o", "json")
    }
}
elseif ((Get-WebAppOsKind -WebApp $webApp) -ne $PlanOs) {
    throw "Existing web app $WebAppName is $((Get-WebAppOsKind -WebApp $webApp)), but PlanOs was set to $PlanOs. Use a different web app name for the parallel deployment."
}

if ($ValidateOnly) {
    Write-Host "Validation completed."
    return
}

Write-Step "Stopping any local TropicalStorms.Api process to avoid publish locks"
Get-Process TropicalStorms.Api -ErrorAction SilentlyContinue | Stop-Process -Force

if (-not $SkipVnetIntegration) {
    Write-Step "Ensuring private SQL network path for the Web App"
    Ensure-WebAppPrivateSqlPath -ResourceGroup $ResourceGroup -WebAppName $WebAppName -VnetName $VnetName -SubnetName $IntegrationSubnetName -SubnetAddressPrefix $IntegrationSubnetAddressPrefix
}

Write-Step "Publishing application package"
$project = if ([System.IO.Path]::IsPathRooted($ProjectPath)) { $ProjectPath } else { Join-Path $workspaceRoot $ProjectPath }
$zipPath = New-WebPackage -WorkspaceRoot $workspaceRoot -Project $project -SkipBuild:$SkipBuild

Write-Step "Configuring application settings"
$appSettings = @(
    "ConnectionStrings__TTE=$sqlConnectionString",
    "NHCParser__SqlConnectionString=$sqlConnectionString",
    "TropicalStorms__LegacySoapShim__Enabled=$(((-not $DisableLegacyShim)).ToString().ToLowerInvariant())",
    "TropicalStorms__LegacySoapShim__Path=/Services/TropicalStorms.asmx"
)

if (-not [string]::IsNullOrWhiteSpace($websiteAcsConnectionString) -and -not [string]::IsNullOrWhiteSpace($websiteAcsSenderAddress)) {
    $appSettings += @(
        "TropicalStorms__Website__AcsEmail__Enabled=true",
        "TropicalStorms__Website__AcsEmail__ConnectionString=$websiteAcsConnectionString",
        "TropicalStorms__Website__AcsEmail__SenderAddress=$websiteAcsSenderAddress",
        "TropicalStorms__Website__AcsEmail__SenderDisplayName=$websiteAcsSenderDisplayName",
        "TropicalStorms__Website__AcsEmail__AdminAddress=$websiteAcsAdminAddress"
    )
}

if (-not [string]::IsNullOrWhiteSpace($applicationInsightsConnectionString)) {
    $appSettings += "APPLICATIONINSIGHTS_CONNECTION_STRING=$applicationInsightsConnectionString"
}

Invoke-AzQuiet -Arguments @(
    "webapp", "config", "appsettings", "set",
    "--resource-group", $ResourceGroup,
    "--name", $WebAppName,
    "--settings"
) + $appSettings | Out-Null

Write-Step "Removing legacy SMTP application settings"
Invoke-AzQuiet -Arguments @(
    "webapp", "config", "appsettings", "delete",
    "--resource-group", $ResourceGroup,
    "--name", $WebAppName,
    "--setting-names",
    "TropicalStorms__Email__Enabled",
    "TropicalStorms__Email__Host",
    "TropicalStorms__Email__Port",
    "TropicalStorms__Email__UseSsl",
    "TropicalStorms__Email__UserName",
    "TropicalStorms__Email__Password",
    "TropicalStorms__Email__FromAddress",
    "TropicalStorms__Email__FromName",
    "TropicalStorms__Email__AdminAddress"
) | Out-Null

Write-Step "Allowing app-level HTTPS redirects"
Invoke-AzQuiet -Arguments @("webapp", "update", "--resource-group", $ResourceGroup, "--name", $WebAppName, "--set", "httpsOnly=false")

Write-Step "Deploying package"
Invoke-AzQuiet -Arguments @("webapp", "deploy", "--resource-group", $ResourceGroup, "--name", $WebAppName, "--src-path", $zipPath, "--type", "zip", "--async", "false")

if (-not $SkipLogs) {
    Write-Step "Enabling filesystem logging"
    Invoke-AzQuiet -Arguments @("webapp", "log", "config", "--resource-group", $ResourceGroup, "--name", $WebAppName, "--application-logging", "filesystem", "--level", "information", "--web-server-logging", "filesystem")
}

Write-Host "`nWeb App: $WebAppName"
Write-Host "URL: https://$WebAppName.azurewebsites.net"
Write-Host "Legacy SOAP WSDL: https://$WebAppName.azurewebsites.net/Services/TropicalStorms.asmx?wsdl"
Write-Host "Live logs: az webapp log tail --name $WebAppName --resource-group $ResourceGroup"
