[CmdletBinding()]
param(
    [string]$EnvFile = ".env",
    [string[]]$DatabaseNames = @("TTE"),
    [switch]$ValidateOnly,
    [switch]$SkipPublish,
    [switch]$SkipFirewall,
    [string]$SqlPackagePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)

    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Import-EnvFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Environment file not found: $Path"
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith("#")) {
            continue
        }

        $parts = $trimmed -split "=", 2
        if ($parts.Count -ne 2) {
            throw "Invalid line in ${Path}: $line"
        }

        $key = $parts[0].Trim()
        $value = $parts[1].Trim().Trim('"')
        Set-Item -Path "Env:$key" -Value $value
    }
}

function Require-Command {
    param([string]$Name)

    $command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Required command not found: $Name"
    }

    return $command.Source
}

function Resolve-SqlPackagePath {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "SqlPackage not found at the supplied path: $ExplicitPath"
        }

        return $ExplicitPath
    }

    $command = Get-Command -Name "sqlpackage" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        "C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\140\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\170\DAC\bin\sqlpackage.exe",
        "C:\Program Files\Microsoft SQL Server\160\DAC\bin\sqlpackage.exe",
        "C:\Program Files\Microsoft SQL Server\150\DAC\bin\sqlpackage.exe",
        "C:\Program Files\Microsoft SQL Server\140\DAC\bin\sqlpackage.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "SqlPackage was not found. Install it or pass -SqlPackagePath explicitly."
}

function Get-RequiredEnv {
    param([string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable is missing: $Name"
    }

    return $value
}

function Test-TrueValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $false
    }

    return $Value.Trim().ToLowerInvariant() -in @("1", "true", "yes", "y")
}

function Resolve-AzureRegion {
    param([string]$Region)

    $normalized = $Region.Trim().ToLowerInvariant()
    $aliases = @{
        "eus"  = "eastus"
        "eus2" = "eastus2"
        "cus"  = "centralus"
        "scus" = "southcentralus"
        "wus"  = "westus"
        "wus2" = "westus2"
        "wus3" = "westus3"
    }

    if ($aliases.ContainsKey($normalized)) {
        return $aliases[$normalized]
    }

    return $normalized
}

function Invoke-Az {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    $escapedArguments = $Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_.Replace('"', '""')) + '"'
        }
        else {
            $_
        }
    }

    $commandText = "az $($escapedArguments -join ' ')"

    $stdoutPath = [System.IO.Path]::GetTempFileName()
    $stderrPath = [System.IO.Path]::GetTempFileName()

    try {
        $process = Start-Process -FilePath "cmd.exe" -ArgumentList "/d", "/c", $commandText -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath
        $stdout = Get-Content -LiteralPath $stdoutPath -Raw
        $stderr = Get-Content -LiteralPath $stderrPath -Raw
        $output = @($stdout, $stderr) -join ""
        $global:LASTEXITCODE = $process.ExitCode

        if (-not $AllowFailure -and $process.ExitCode -ne 0) {
            throw "az $($Arguments -join ' ') failed:`n$output"
        }
    }
    finally {
        Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue
    }

    return ($output | Out-String).Trim()
}

function Get-CurrentPublicIp {
    try {
        return (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 10).Trim()
    }
    catch {
        Write-Warning "Could not detect current public IP address: $($_.Exception.Message)"
        return $null
    }
}

function Ensure-ResourceGroup {
    param(
        [string]$Name,
        [string]$Region
    )

    $exists = Invoke-Az -Arguments @("group", "exists", "--name", $Name)
    if ($exists -eq "true") {
        return
    }

    Invoke-Az -Arguments @("group", "create", "--name", $Name, "--location", $Region, "-o", "none") | Out-Null
}

function Ensure-SqlServer {
    param(
        [string]$ResourceGroup,
        [string]$Region,
        [string]$ServerName,
        [string]$AdminUser,
        [string]$AdminPassword
    )

    Invoke-Az -Arguments @("sql", "server", "show", "--resource-group", $ResourceGroup, "--name", $ServerName, "-o", "none") -AllowFailure | Out-Null
    if ($LASTEXITCODE -eq 0) {
        return
    }

    Invoke-Az -Arguments @(
        "sql", "server", "create",
        "--resource-group", $ResourceGroup,
        "--name", $ServerName,
        "--location", $Region,
        "--admin-user", $AdminUser,
        "--admin-password", $AdminPassword,
        "-o", "none"
    ) | Out-Null
}

function Ensure-FirewallRules {
    param(
        [string]$ResourceGroup,
        [string]$ServerName,
        [bool]$AllowAzureServices
    )

    if ($AllowAzureServices) {
        Invoke-Az -Arguments @(
            "sql", "server", "firewall-rule", "create",
            "--resource-group", $ResourceGroup,
            "--server", $ServerName,
            "--name", "AllowAzureServices",
            "--start-ip-address", "0.0.0.0",
            "--end-ip-address", "0.0.0.0",
            "-o", "none"
        ) | Out-Null
    }

    $publicIp = Get-CurrentPublicIp
    if ($publicIp) {
        Invoke-Az -Arguments @(
            "sql", "server", "firewall-rule", "create",
            "--resource-group", $ResourceGroup,
            "--server", $ServerName,
            "--name", "ClientIp",
            "--start-ip-address", $publicIp,
            "--end-ip-address", $publicIp,
            "-o", "none"
        ) | Out-Null
    }
}

function Ensure-Database {
    param(
        [string]$ResourceGroup,
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$ServiceObjective,
        [string]$BackupStorageRedundancy
    )

    Invoke-Az -Arguments @("sql", "db", "show", "--resource-group", $ResourceGroup, "--server", $ServerName, "--name", $DatabaseName, "-o", "none") -AllowFailure | Out-Null
    if ($LASTEXITCODE -eq 0) {
        return
    }

    Invoke-Az -Arguments @(
        "sql", "db", "create",
        "--resource-group", $ResourceGroup,
        "--server", $ServerName,
        "--name", $DatabaseName,
        "--service-objective", $ServiceObjective,
        "--backup-storage-redundancy", $BackupStorageRedundancy,
        "-o", "none"
    ) | Out-Null
}

function Publish-Dacpac {
    param(
        [string]$ToolPath,
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$AdminUser,
        [string]$AdminPassword,
        [string]$DacpacPath
    )

    $stdoutPath = [System.IO.Path]::GetTempFileName()
    $stderrPath = [System.IO.Path]::GetTempFileName()

    try {
        $process = Start-Process -FilePath $ToolPath -ArgumentList @(
            "/Action:Publish",
            "/TargetServerName:tcp:$ServerName.database.windows.net,1433",
            "/TargetDatabaseName:$DatabaseName",
            "/TargetUser:$AdminUser",
            "/TargetPassword:$AdminPassword",
            "/SourceFile:`"$DacpacPath`"",
            "/TargetEncryptConnection:True",
            "/TargetTrustServerCertificate:False",
            "/p:AllowIncompatiblePlatform=True",
            "/p:BlockOnPossibleDataLoss=False",
            "/p:ExcludeObjectTypes=Users;Logins;StoredProcedures",
            "/p:DropObjectsNotInSource=False"
        ) -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath

        $stdout = Get-Content -LiteralPath $stdoutPath -Raw
        $stderr = Get-Content -LiteralPath $stderrPath -Raw
        $output = @($stdout, $stderr) -join ""
        $global:LASTEXITCODE = $process.ExitCode

        if ($process.ExitCode -ne 0) {
            throw "SqlPackage publish failed for ${DatabaseName}:`n$output"
        }
    }
    finally {
        Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $scriptRoot
$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $workspaceRoot $EnvFile }
$dacpacDirectory = Join-Path $workspaceRoot "DAC Packages"

Write-Step "Loading environment from $envPath"
Import-EnvFile -Path $envPath

$subscriptionId = Get-RequiredEnv -Name "AZURE_SUBSCRIPTION_ID"
$resourceGroup = Get-RequiredEnv -Name "AZURE_RESOURCE_GROUP"
$region = Resolve-AzureRegion -Region (Get-RequiredEnv -Name "AZURE_REGION")
$serverName = Get-RequiredEnv -Name "AZURE_SQL_SERVER_NAME"
$adminUser = Get-RequiredEnv -Name "AZURE_SQL_ADMIN_USER"
$adminPassword = Get-RequiredEnv -Name "AZURE_SQL_ADMIN_PASSWORD"

$serviceObjective = [Environment]::GetEnvironmentVariable("AZURE_SQL_SERVICE_OBJECTIVE")
if ([string]::IsNullOrWhiteSpace($serviceObjective)) {
    $serviceObjective = "Basic"
}

$backupStorageRedundancy = [Environment]::GetEnvironmentVariable("AZURE_SQL_BACKUP_STORAGE_REDUNDANCY")
if ([string]::IsNullOrWhiteSpace($backupStorageRedundancy)) {
    $backupStorageRedundancy = "Local"
}

$allowAzureServices = Test-TrueValue -Value ([Environment]::GetEnvironmentVariable("AZURE_SQL_ALLOW_AZURE_SERVICES"))

if (-not (Test-Path -LiteralPath $dacpacDirectory)) {
    throw "DACPAC directory not found: $dacpacDirectory"
}

$availableDacpacs = Get-ChildItem -LiteralPath $dacpacDirectory -Filter *.dacpac | Sort-Object Name
if (-not $availableDacpacs) {
    throw "No DACPAC files found in $dacpacDirectory"
}

$selectedDatabaseNames = @($DatabaseNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })
if (-not $selectedDatabaseNames) {
    throw "At least one database name must be supplied."
}

$dacpacFiles = foreach ($databaseName in $selectedDatabaseNames) {
    $dacpacPath = Join-Path $dacpacDirectory "$databaseName.dacpac"
    if (-not (Test-Path -LiteralPath $dacpacPath)) {
        throw "DACPAC not found for database $databaseName at $dacpacPath"
    }

    Get-Item -LiteralPath $dacpacPath
}

Write-Step "Checking local prerequisites"
Require-Command -Name "az" | Out-Null

if (-not $SkipPublish) {
    $SqlPackagePath = Resolve-SqlPackagePath -ExplicitPath $SqlPackagePath
}

Write-Step "Using Azure subscription $subscriptionId"
Invoke-Az -Arguments @("account", "set", "--subscription", $subscriptionId) | Out-Null

if ($ValidateOnly) {
    Write-Host "Validation succeeded. Deployment prerequisites and configuration look usable." -ForegroundColor Green
    Write-Host "Selected DACPACs: $($dacpacFiles.Name -join ', ')"
    exit 0
}

Write-Step "Ensuring resource group $resourceGroup"
Ensure-ResourceGroup -Name $resourceGroup -Region $region

Write-Step "Ensuring Azure SQL logical server $serverName"
Ensure-SqlServer -ResourceGroup $resourceGroup -Region $region -ServerName $serverName -AdminUser $adminUser -AdminPassword $adminPassword

if (-not $SkipFirewall) {
    Write-Step "Configuring firewall rules"
    Ensure-FirewallRules -ResourceGroup $resourceGroup -ServerName $serverName -AllowAzureServices:$allowAzureServices
}

foreach ($dacpac in $dacpacFiles) {
    $databaseName = $dacpac.BaseName

    Write-Step "Ensuring database $databaseName"
    Ensure-Database -ResourceGroup $resourceGroup -ServerName $serverName -DatabaseName $databaseName -ServiceObjective $serviceObjective -BackupStorageRedundancy $backupStorageRedundancy

    if (-not $SkipPublish) {
        Write-Step "Publishing $($dacpac.Name) to $databaseName"
        Publish-Dacpac -ToolPath $SqlPackagePath -ServerName $serverName -DatabaseName $databaseName -AdminUser $adminUser -AdminPassword $adminPassword -DacpacPath $dacpac.FullName
    }
}

Write-Host "`nDeployment completed." -ForegroundColor Green