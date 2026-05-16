[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SubscriptionId = "<sub id>",
    [string]$ResourceGroupName = "<resource group>",
    [string]$SqlServerName = "<sql server name>",
    [string]$RuleName = "ClientIp",
    [string]$IpAddress
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command -Name $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Require-Command -Name "az"

if ($SubscriptionId -eq "<sub id>" -or $ResourceGroupName -eq "<resource group>" -or $SqlServerName -eq "<sql server name>") {
    throw "Set -SubscriptionId, -ResourceGroupName, and -SqlServerName before running this script. The checked-in defaults are placeholders."
}

if ([string]::IsNullOrWhiteSpace($IpAddress)) {
    $IpAddress = (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 15).Trim()
}

Write-Host "Using public IP: $IpAddress" -ForegroundColor Cyan

if (-not [string]::IsNullOrWhiteSpace($SubscriptionId)) {
    & az account set --subscription $SubscriptionId | Out-Null
}

if ($PSCmdlet.ShouldProcess("$SqlServerName/$RuleName", "Update firewall rule to $IpAddress")) {
    & az sql server firewall-rule create `
        --resource-group $ResourceGroupName `
        --server $SqlServerName `
        --name $RuleName `
        --start-ip-address $IpAddress `
        --end-ip-address $IpAddress `
        --output table

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to update firewall rule $RuleName on server $SqlServerName"
    }
}

Write-Host "Current firewall rules:" -ForegroundColor Cyan
& az sql server firewall-rule list --resource-group $ResourceGroupName --server $SqlServerName -o table