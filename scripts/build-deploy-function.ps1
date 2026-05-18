[CmdletBinding()]
param(
    [string]$ResourceGroup = "rg-eus2-gencode-enterprise",
    [string]$FunctionAppName = "func-nhcparser-flex-cu66c7",
    [string]$ProjectPath = ".\src\NHCParser.Function\NHCParser.Function.csproj",
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

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$project = if ([System.IO.Path]::IsPathRooted($ProjectPath)) { $ProjectPath } else { Join-Path $workspaceRoot $ProjectPath }
$publishRoot = Join-Path $scriptRoot "tempzip"
$publishDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("NHCParser.Function.build-" + [guid]::NewGuid().ToString("N"))
$zipPath = Join-Path $publishRoot "NHCParser.Function.zip"

Require-Command -Name "tar.exe"

try {
    Write-Step "Building Function package"
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
        throw "Function publish output not found: $publishDirectory"
    }

    Write-Step "Creating Function deployment zip"
    & tar.exe -a -c -f $zipPath -C $publishDirectory .
    if ($LASTEXITCODE -ne 0) {
        throw "tar.exe failed while creating $zipPath"
    }

    if ($BuildOnly) {
        Write-Host "Function build completed: $zipPath" -ForegroundColor Green
        Write-Host "How to test after deploy: az functionapp function list --name $FunctionAppName --resource-group $ResourceGroup -o table" -ForegroundColor Yellow
        return
    }

    Write-Step "Deploying Function App package"
    Require-Command -Name "az"

    & az resource update --resource-group $ResourceGroup --resource-type Microsoft.Web/sites --name $FunctionAppName --api-version 2024-04-01 --set properties.functionAppConfig.runtime.version=10.0 --only-show-errors | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Function runtime update failed."
    }

    & az functionapp deployment source config-zip --resource-group $ResourceGroup --name $FunctionAppName --src $zipPath -o none
    if ($LASTEXITCODE -ne 0) {
        throw "Function deploy failed."
    }

    Write-Host "Function deploy completed." -ForegroundColor Green
    Write-Host "How to test: az functionapp function list --name $FunctionAppName --resource-group $ResourceGroup -o table" -ForegroundColor Yellow
    Write-Host "How to test: az functionapp scale config show --name $FunctionAppName --resource-group $ResourceGroup" -ForegroundColor Yellow
}
finally {
    if (Test-Path -LiteralPath $publishDirectory) {
        Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}