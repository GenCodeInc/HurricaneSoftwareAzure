[CmdletBinding()]
param(
    [string]$EnvFile = ".env",
    [switch]$ValidateOnly,
    [switch]$SkipSql,
    [switch]$SkipFunctionInfra,
    [switch]$SkipFunctionPublish,
    [switch]$SkipBuild,
    [switch]$SkipLogConfiguration,
    [string]$SqlPackagePath
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

function Get-RequiredEnv {
    param([string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable is missing: $Name"
    }

    return $value
}

function Get-OptionalEnv {
    param([string]$Name)

    return [Environment]::GetEnvironmentVariable($Name)
}

function Get-EnvValue {
    param(
        [string[]]$Names,
        [switch]$Required
    )

    foreach ($name in $Names) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
    }

    if ($Required) {
        throw "Required environment variable is missing. Checked: $($Names -join ', ')"
    }

    return $null
}

function Test-TrueValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.Trim().ToLowerInvariant() -in @("1", "true", "yes", "y")
}

function Resolve-AzureRegion {
    param([string]$Region)

    $normalized = $Region.Trim().ToLowerInvariant()
    $aliases = @{
        "eus"  = "eastus"
        "eus2" = "eastus2"
        "cus"  = "centralus"
        "scus" = "southcentralus"
        "wus"  = "westus"
        "wus2" = "westus2"
        "wus3" = "westus3"
    }

    if ($aliases.ContainsKey($normalized)) {
        return $aliases[$normalized]
    }

    return $normalized
}

function Require-Command {
    param([string]$Name)

    $command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Required command not found: $Name"
    }

    return $command.Source
}

function Invoke-AzJson {
    param([string[]]$Arguments)

    $output = & az @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }

    $text = ($output | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Invoke-AzQuiet {
    param([string[]]$Arguments)

    $output = & az @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }
}

function Build-SqlConnectionString {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$UserName,
        [string]$Password
    )

    return "Server=tcp:${ServerName}.database.windows.net,1433;Initial Catalog=${DatabaseName};Persist Security Info=False;User ID=${UserName};Password=${Password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

function New-FunctionPackage {
    param(
        [string]$WorkspaceRoot,
        [switch]$SkipBuild
    )

    $projectPath = Join-Path $WorkspaceRoot "src\NHCParser.Function\NHCParser.Function.csproj"
    $publishRoot = Join-Path $WorkspaceRoot ".publish"
    $publishDirectory = Join-Path $publishRoot "NHCParser.Function"
    $zipPath = Join-Path $publishRoot "NHCParser.Function.zip"

    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force
    }

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

    if (-not $SkipBuild) {
        & dotnet publish $projectPath -c Release -o $publishDirectory
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $projectPath"
        }
    }

    & tar.exe -a -c -f $zipPath -C $publishDirectory .
    if ($LASTEXITCODE -ne 0) {
        throw "tar.exe failed while creating $zipPath"
    }

    return $zipPath
}

function Get-FunctionDeploymentParameters {
    param(
        [string]$Location,
        [string]$SqlConnectionString,
        [string]$SqlServerName,
        [string]$TimerSchedule,
        [bool]$CurrentYearOnly,
        [bool]$ProbeDatabaseOnStartup,
        [string]$FunctionAppName,
        [string]$PlanName,
        [string]$StorageAccountName,
        [int]$MaximumInstanceCount,
        [int]$InstanceMemoryMb,
        [string]$AlwaysReadyFunctionName,
        [int]$AlwaysReadyInstanceCount
    )

    $parameters = @(
        "location=$Location",
        "sqlConnectionString=$SqlConnectionString",
        "sqlServerName=$SqlServerName",
        "timerSchedule=$TimerSchedule",
        "currentYearOnly=$($CurrentYearOnly.ToString().ToLowerInvariant())",
        "probeDatabaseOnStartup=$($ProbeDatabaseOnStartup.ToString().ToLowerInvariant())",
        "maximumInstanceCount=$MaximumInstanceCount",
        "instanceMemoryMB=$InstanceMemoryMb",
        "alwaysReadyFunctionName=$AlwaysReadyFunctionName",
        "alwaysReadyInstanceCount=$AlwaysReadyInstanceCount"
    )

    if (-not [string]::IsNullOrWhiteSpace($FunctionAppName)) {
        $parameters += "functionAppName=$FunctionAppName"
    }

    if (-not [string]::IsNullOrWhiteSpace($PlanName)) {
        $parameters += "appServicePlanName=$PlanName"
    }

    if (-not [string]::IsNullOrWhiteSpace($StorageAccountName)) {
        $parameters += "storageAccountName=$StorageAccountName"
    }

    return $parameters
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $workspaceRoot $EnvFile }
$sqlDeployScript = Join-Path $scriptRoot "deploy-azure-sql.ps1"
$bicepPath = Join-Path $scriptRoot "deploy-nhcparser-function.bicep"

Write-Step "Loading environment from $envPath"
Import-EnvFile -Path $envPath

$subscriptionId = Get-EnvValue -Names @("AZURE_SUBSCRIPTION_ID", "subscription") -Required
$resourceGroup = Get-EnvValue -Names @("AZURE_RESOURCE_GROUP", "resource_group") -Required
$location = Resolve-AzureRegion -Region (Get-EnvValue -Names @("AZURE_REGION", "region") -Required)
$serverName = Get-EnvValue -Names @("AZURE_SQL_SERVER_NAME", "sql_server") -Required
$adminUser = Get-EnvValue -Names @("AZURE_SQL_ADMIN_USER", "sql_admin_user") -Required
$adminPassword = Get-EnvValue -Names @("AZURE_SQL_ADMIN_PASSWORD", "sql_admin_password") -Required

$functionAppName = Get-OptionalEnv -Name "AZURE_FUNCTION_APP_NAME"
$planName = Get-OptionalEnv -Name "AZURE_FUNCTION_PLAN_NAME"
$storageAccountName = Get-OptionalEnv -Name "AZURE_FUNCTION_STORAGE_ACCOUNT_NAME"
$functionMaximumInstanceCount = 100
$functionMaximumInstanceCountValue = Get-OptionalEnv -Name "AZURE_FUNCTION_MAXIMUM_INSTANCE_COUNT"
if (-not [string]::IsNullOrWhiteSpace($functionMaximumInstanceCountValue)) {
    $functionMaximumInstanceCount = [int]$functionMaximumInstanceCountValue
}

$functionInstanceMemoryMb = 512
$functionInstanceMemoryMbValue = Get-OptionalEnv -Name "AZURE_FUNCTION_INSTANCE_MEMORY_MB"
if (-not [string]::IsNullOrWhiteSpace($functionInstanceMemoryMbValue)) {
    $functionInstanceMemoryMb = [int]$functionInstanceMemoryMbValue
}

$functionAlwaysReadyName = Get-OptionalEnv -Name "AZURE_FUNCTION_ALWAYS_READY_FUNCTION_NAME"
if ([string]::IsNullOrWhiteSpace($functionAlwaysReadyName)) {
    $functionAlwaysReadyName = "NHCParserTimer"
}

$functionAlwaysReadyInstanceCount = 1
$functionAlwaysReadyInstanceCountValue = Get-OptionalEnv -Name "AZURE_FUNCTION_ALWAYS_READY_INSTANCE_COUNT"
if (-not [string]::IsNullOrWhiteSpace($functionAlwaysReadyInstanceCountValue)) {
    $functionAlwaysReadyInstanceCount = [int]$functionAlwaysReadyInstanceCountValue
}

$timerSchedule = Get-OptionalEnv -Name "AZURE_FUNCTION_TIMER_SCHEDULE"
if ([string]::IsNullOrWhiteSpace($timerSchedule)) {
    $timerSchedule = "0 */5 * * * *"
}

$currentYearOnly = Test-TrueValue -Value (Get-OptionalEnv -Name "AZURE_FUNCTION_CURRENT_YEAR_ONLY")
if (-not [Environment]::GetEnvironmentVariable("AZURE_FUNCTION_CURRENT_YEAR_ONLY")) {
    $currentYearOnly = $true
}

$probeDatabaseOnStartup = Test-TrueValue -Value (Get-OptionalEnv -Name "AZURE_FUNCTION_PROBE_DATABASE_ON_STARTUP")

$sqlConnectionString = Build-SqlConnectionString -ServerName $serverName -DatabaseName "TTE" -UserName $adminUser -Password $adminPassword

Write-Step "Checking local prerequisites"
Require-Command -Name "az" | Out-Null
Require-Command -Name "dotnet" | Out-Null
Require-Command -Name "tar.exe" | Out-Null

if (-not $SkipSql -and -not $ValidateOnly -and -not (Test-Path -LiteralPath $sqlDeployScript)) {
    throw "SQL deployment script not found: $sqlDeployScript"
}

if (-not (Test-Path -LiteralPath $bicepPath)) {
    throw "Function App deployment template not found: $bicepPath"
}

Write-Step "Using Azure subscription $subscriptionId"
Invoke-AzQuiet -Arguments @("account", "set", "--subscription", $subscriptionId)

if ($ValidateOnly) {
    Write-Host "Validation succeeded. The stack deployment inputs and local tooling are available." -ForegroundColor Green
    Write-Host "Resource group: $resourceGroup"
    Write-Host "SQL server: $serverName"
    Write-Host "Function timer schedule: $timerSchedule"
    Write-Host "Function hosting: Flex Consumption (FC1 Linux), memory: $functionInstanceMemoryMb MB, max instances: $functionMaximumInstanceCount, always ready: function:$functionAlwaysReadyName=$functionAlwaysReadyInstanceCount"
    exit 0
}

if (-not $SkipSql) {
    Write-Step "Deploying Azure SQL resources and DACPACs"
    $sqlArguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $sqlDeployScript,
        "-EnvFile", $envPath
    )

    if ($SqlPackagePath) {
        $sqlArguments += @("-SqlPackagePath", $SqlPackagePath)
    }

    & PowerShell @sqlArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure SQL deployment failed."
    }
}

$deployedFunctionAppName = $functionAppName

if (-not $SkipFunctionInfra) {
    Write-Step "Deploying Function App infrastructure"
    $deploymentName = "nhcparser-$(Get-Date -Format 'yyyyMMddHHmmss')"
    $deploymentParameters = Get-FunctionDeploymentParameters -Location $location -SqlConnectionString $sqlConnectionString -SqlServerName $serverName -TimerSchedule $timerSchedule -CurrentYearOnly:$currentYearOnly -ProbeDatabaseOnStartup:$probeDatabaseOnStartup -FunctionAppName $functionAppName -PlanName $planName -StorageAccountName $storageAccountName -MaximumInstanceCount $functionMaximumInstanceCount -InstanceMemoryMb $functionInstanceMemoryMb -AlwaysReadyFunctionName $functionAlwaysReadyName -AlwaysReadyInstanceCount $functionAlwaysReadyInstanceCount

    $deployment = Invoke-AzJson -Arguments @(
        "deployment", "group", "create",
        "--resource-group", $resourceGroup,
        "--name", $deploymentName,
        "--template-file", $bicepPath,
        "--parameters"
    ) + $deploymentParameters

    $deployedFunctionAppName = $deployment.properties.outputs.functionAppName.value
    $deployedHostname = $deployment.properties.outputs.functionAppHostname.value

    Write-Host "Function App: $deployedFunctionAppName"
    Write-Host "Hostname: $deployedHostname"
}

if ([string]::IsNullOrWhiteSpace($deployedFunctionAppName)) {
    throw "Function App name is not known. Set AZURE_FUNCTION_APP_NAME or allow infrastructure deployment to create it."
}

if (-not $SkipFunctionPublish) {
    Write-Step "Publishing the Function App package"
    $zipPath = New-FunctionPackage -WorkspaceRoot $workspaceRoot -SkipBuild:$SkipBuild

    Invoke-AzQuiet -Arguments @(
        "functionapp", "deployment", "source", "config-zip",
        "--resource-group", $resourceGroup,
        "--name", $deployedFunctionAppName,
        "--src", $zipPath,
        "-o", "none"
    )
}

if (-not $SkipLogConfiguration) {
    Write-Step "Skipping legacy filesystem log configuration for the Flex Consumption Function App"
}

Write-Host "`nStack deployment completed." -ForegroundColor Green
Write-Host "Function App: $deployedFunctionAppName"
Write-Host "Scale config: az functionapp scale config show --name $deployedFunctionAppName --resource-group $resourceGroup"