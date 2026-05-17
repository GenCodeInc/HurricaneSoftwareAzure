[CmdletBinding()]
param()

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

Start-LocalWindow -ScriptPath (Join-Path $scriptRoot "run-local-api.ps1") -Title "Local API"
Start-LocalWindow -ScriptPath (Join-Path $scriptRoot "run-local-web.ps1") -Title "Local Website"

Write-Host "Local website stack windows launched." -ForegroundColor Green
Write-Host "How to test API: http://127.0.0.1:5085/api/tropical-storms/HelloWorld" -ForegroundColor Yellow
Write-Host "How to test website: open the localhost URL shown by the website window" -ForegroundColor Yellow
Write-Host "How to test website API flow: http://127.0.0.1:5085/api/website/registration/recover/acs" -ForegroundColor Yellow