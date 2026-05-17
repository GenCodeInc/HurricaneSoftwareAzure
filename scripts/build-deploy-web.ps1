[CmdletBinding()]
param(
    [string]$ResourceGroup = "rg-eus2-gencode-enterprise",
    [string]$StaticWebAppName = "stapp-hurricanesoftware-cu66c7",
    [string]$ProjectPath = ".\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj",
    [string]$DeploymentToken,
    [switch]$BuildOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Require-Command {
    param([string]$Name)

    $command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Required command not found: $Name"
    }
}

function Invoke-AzJson {
    param([string[]]$Arguments)

    $output = & az @Arguments --only-show-errors 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "az $($Arguments -join ' ') failed:`n$($output | Out-String)"
    }

    $text = ($output | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return $null
    }

    return $text | ConvertFrom-Json
}

function Invoke-Process {
    param(
        [string]$FilePath,
        [string[]]$ArgumentList
    )

    $process = Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -NoNewWindow -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "$FilePath exited with code $($process.ExitCode)."
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$project = if ([System.IO.Path]::IsPathRooted($ProjectPath)) { $ProjectPath } else { Join-Path $workspaceRoot $ProjectPath }
$publishDirectory = Join-Path $workspaceRoot "src\HurricaneSoftware.Web\bin\Release\net8.0\publish\wwwroot"

Write-Step "Building website publish output"
Require-Command -Name "dotnet"

& dotnet publish $project -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for $project"
}

if ($BuildOnly) {
    Write-Host "Website build completed: $publishDirectory" -ForegroundColor Green
    Write-Host "How to test after deploy: https://www.hurricanesoftware.com" -ForegroundColor Yellow
    return
}

if (-not (Test-Path -LiteralPath $publishDirectory)) {
    throw "Website publish output not found: $publishDirectory"
}

Require-Command -Name "az"
Require-Command -Name "npm"

if ([string]::IsNullOrWhiteSpace($DeploymentToken)) {
    $DeploymentToken = [Environment]::GetEnvironmentVariable("AZURE_STATIC_WEB_APP_DEPLOYMENT_TOKEN")
}

if ([string]::IsNullOrWhiteSpace($DeploymentToken)) {
    Write-Step "Fetching Static Web App deployment token"
    $secretResult = Invoke-AzJson -Arguments @(
        "staticwebapp", "secrets", "list",
        "--name", $StaticWebAppName,
        "--resource-group", $ResourceGroup,
        "-o", "json"
    )

    $DeploymentToken = $secretResult.properties.apiKey
}

if ([string]::IsNullOrWhiteSpace($DeploymentToken)) {
    throw "Static Web App deployment token could not be resolved."
}

Write-Step "Deploying website to Azure Static Web Apps"
$npmArguments = @(
    "exec",
    "--yes",
    "--package=@azure/static-web-apps-cli",
    "--",
    "swa",
    "deploy",
    $publishDirectory,
    "--deployment-token",
    $DeploymentToken,
    "--app-name",
    $StaticWebAppName,
    "--resource-group",
    $ResourceGroup,
    "--env",
    "production"
)

Invoke-Process -FilePath "npm.cmd" -ArgumentList $npmArguments

Write-Host "Website deploy completed." -ForegroundColor Green
Write-Host "How to test: https://www.hurricanesoftware.com" -ForegroundColor Yellow
Write-Host "How to test: https://red-ocean-0a1dd550f.7.azurestaticapps.net" -ForegroundColor Yellow