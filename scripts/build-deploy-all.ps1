[CmdletBinding()]
param(
    [switch]$BuildOnly,
    [switch]$SkipWeb,
    [switch]$SkipApi,
    [switch]$SkipFunction,
    [string]$WebsiteDeploymentToken
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-ChildScript {
    param(
        [string]$ScriptPath,
        [string]$Label,
        [string[]]$Arguments
    )

    Write-Host "`n==> $Label" -ForegroundColor Cyan
    & PowerShell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed."
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $SkipWeb) {
    $webArguments = @()
    if ($BuildOnly) {
        $webArguments += "-BuildOnly"
    }

    if (-not [string]::IsNullOrWhiteSpace($WebsiteDeploymentToken)) {
        $webArguments += @("-DeploymentToken", $WebsiteDeploymentToken)
    }

    Invoke-ChildScript -ScriptPath (Join-Path $scriptRoot "build-deploy-web.ps1") -Label "Website" -Arguments $webArguments
}

if (-not $SkipApi) {
    $apiArguments = @()
    if ($BuildOnly) {
        $apiArguments += "-BuildOnly"
    }

    Invoke-ChildScript -ScriptPath (Join-Path $scriptRoot "build-deploy-api.ps1") -Label "API" -Arguments $apiArguments
}

if (-not $SkipFunction) {
    $functionArguments = @()
    if ($BuildOnly) {
        $functionArguments += "-BuildOnly"
    }

    Invoke-ChildScript -ScriptPath (Join-Path $scriptRoot "build-deploy-function.ps1") -Label "Function" -Arguments $functionArguments
}

Write-Host "`nAll requested build/deploy steps completed." -ForegroundColor Green