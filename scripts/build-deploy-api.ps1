[CmdletBinding()]
param(
    [string]$ResourceGroup = "rg-eus2-gencode-enterprise",
    [string]$WebAppName = "api-tropicalstorms-linux-cu66c7",
    [string]$ProjectPath = ".\src\TropicalStorms.Api\TropicalStorms.Api.csproj",
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

$linuxFxVersion = '"DOTNETCORE|10.0"'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$project = if ([System.IO.Path]::IsPathRooted($ProjectPath)) { $ProjectPath } else { Join-Path $workspaceRoot $ProjectPath }
$publishRoot = Join-Path $scriptRoot "tempzip"
$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("TropicalStorms.Api.build-" + [guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $publishRoot "TropicalStorms.Api.zip"

Require-Command -Name "tar.exe"

try {
    Write-Step "Building API package"
    Require-Command -Name "dotnet"

    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

    & dotnet publish $project -c Release -o $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $project"
    }

    if (-not (Test-Path -LiteralPath $publishDirectory)) {
        throw "API publish output not found: $publishDirectory"
    }

    Write-Step "Creating API deployment zip"
    & tar.exe -a -c -f $zipPath -C $publishDirectory .
    if ($LASTEXITCODE -ne 0) {
        throw "tar.exe failed while creating $zipPath"
    }

    if ($BuildOnly) {
        Write-Host "API build completed: $zipPath" -ForegroundColor Green
        Write-Host "How to test after deploy: POST https://webservice.hurricanesoftware.com/api/website/registration/recover/acs" -ForegroundColor Yellow
        Write-Host "Postman: src/TropicalStorms.Api/Postman/TropicalStorms Website API.postman_collection.json" -ForegroundColor Yellow
        return
    }

    Write-Step "Deploying API to Azure App Service"
    Require-Command -Name "az"

    & az webapp config set --resource-group $ResourceGroup --name $WebAppName --linux-fx-version $linuxFxVersion --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "API runtime update failed."
    }

    & az webapp deploy --resource-group $ResourceGroup --name $WebAppName --src-path $zipPath --type zip --async false
    if ($LASTEXITCODE -ne 0) {
        throw "API deploy failed."
    }

    Write-Host "API deploy completed." -ForegroundColor Green
    Write-Host "How to test: POST https://webservice.hurricanesoftware.com/api/website/registration/recover/acs" -ForegroundColor Yellow
    Write-Host "Postman: src/TropicalStorms.Api/Postman/TropicalStorms Website API.postman_collection.json" -ForegroundColor Yellow
}
finally {
    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}