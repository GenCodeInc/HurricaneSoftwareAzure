[CmdletBinding()]
param(
    [switch]$SkipApi,
    [switch]$SkipWeb,
    [switch]$SkipFunction
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Start-LocalWindow {
    param(
        [string]$ScriptPath,
        [string]$Title
    )

    Start-Process -FilePath "powershell.exe" -ArgumentList @(
        "-NoExit",
        "-ExecutionPolicy", "Bypass",
        "-Command",
        "& { `$host.UI.RawUI.WindowTitle = '$Title'; & '$ScriptPath' }"
    ) | Out-Null
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not $SkipApi) {
    Start-LocalWindow -ScriptPath (Join-Path $scriptRoot "run-local-api.ps1") -Title "Local API"
}

if (-not $SkipWeb) {
    Start-LocalWindow -ScriptPath (Join-Path $scriptRoot "run-local-web.ps1") -Title "Local Website"
}

if (-not $SkipFunction) {
    Start-LocalWindow -ScriptPath (Join-Path $scriptRoot "run-local-function.ps1") -Title "Local Function"
}

Write-Host "Local run windows launched." -ForegroundColor Green
Write-Host "How to test API: http://127.0.0.1:5085/api/tropical-storms/HelloWorld" -ForegroundColor Yellow
Write-Host "How to test website: open the localhost URL shown by the website window" -ForegroundColor Yellow
Write-Host "How to test function: watch the function window for timer start/completion logs" -ForegroundColor Yellow