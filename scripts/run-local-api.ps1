[CmdletBinding()]
param(
    [string]$Urls = "http://127.0.0.1:5085",
    [string]$ProjectPath = ".\src\TropicalStorms.Api\TropicalStorms.Api.csproj"
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

function Import-EnvFileIfPresent {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Count -ne 2) {
            continue
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"')
        Set-Item -Path "Env:$key" -Value $value
    }
}

function Ensure-ApiConnectionString {
    if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable("ConnectionStrings__TTE"))) {
        return
    }

    if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable("NHCParser__SqlConnectionString"))) {
        $env:ConnectionStrings__TTE = [Environment]::GetEnvironmentVariable("NHCParser__SqlConnectionString")
        return
    }

    $serverName = [Environment]::GetEnvironmentVariable("AZURE_SQL_SERVER_NAME")
    $adminUser = [Environment]::GetEnvironmentVariable("AZURE_SQL_ADMIN_USER")
    $adminPassword = [Environment]::GetEnvironmentVariable("AZURE_SQL_ADMIN_PASSWORD")
    if ([string]::IsNullOrWhiteSpace($serverName) -or [string]::IsNullOrWhiteSpace($adminUser) -or [string]::IsNullOrWhiteSpace($adminPassword)) {
        return
    }

    if (-not $serverName.EndsWith(".database.windows.net", [System.StringComparison]::OrdinalIgnoreCase)) {
        $serverName = "$serverName.database.windows.net"
    }

    $env:ConnectionStrings__TTE = "Server=tcp:$serverName,1433;Initial Catalog=TTE;Persist Security Info=False;User ID=$adminUser;Password=$adminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$project = if ([System.IO.Path]::IsPathRooted($ProjectPath)) { $ProjectPath } else { Join-Path $workspaceRoot $ProjectPath }

Require-Command -Name "dotnet"
Import-EnvFileIfPresent -Path (Join-Path $workspaceRoot ".env")
Ensure-ApiConnectionString

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Step "Starting TropicalStorms.Api locally"
Write-Host "How to test: $Urls/api/tropical-storms/HelloWorld" -ForegroundColor Yellow
Write-Host "How to test: $Urls/api/website/registration/recover/acs" -ForegroundColor Yellow
Write-Host "How to test: $Urls/Services/TropicalStorms.asmx?wsdl" -ForegroundColor Yellow

& dotnet run --project $project --urls $Urls