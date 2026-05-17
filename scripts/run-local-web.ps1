[CmdletBinding()]
param(
    [string]$ProjectPath = ".\src\HurricaneSoftware.Web\HurricaneSoftware.Web.csproj"
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

Require-Command -Name "dotnet"

Write-Step "Starting HurricaneSoftware.Web locally"
Write-Host "How to test: open the localhost URL printed by Blazor" -ForegroundColor Yellow
Write-Host "Reminder: the API should also be running at http://127.0.0.1:5085 for website forms and checkout to work." -ForegroundColor Yellow

& dotnet run --project $project