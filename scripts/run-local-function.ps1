[CmdletBinding()]
param(
    [string]$ProjectDirectory = ".\src\NHCParser.Function"
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
$projectDirectory = if ([System.IO.Path]::IsPathRooted($ProjectDirectory)) { $ProjectDirectory } else { Join-Path $workspaceRoot $ProjectDirectory }
$localSettingsPath = Join-Path $projectDirectory "local.settings.json"

Require-Command -Name "func"

if (-not (Test-Path -LiteralPath $localSettingsPath)) {
    Write-Host "Warning: local.settings.json was not found at $localSettingsPath" -ForegroundColor Yellow
    Write-Host "Copy local.settings.json.example first if this is your first local Function run." -ForegroundColor Yellow
}

Write-Step "Starting NHCParser.Function locally"
Write-Host "How to test: watch the console for 'NHC parser timer started' and 'NHC parser timer completed'" -ForegroundColor Yellow

Push-Location $projectDirectory
try {
    & func start
}
finally {
    Pop-Location
}