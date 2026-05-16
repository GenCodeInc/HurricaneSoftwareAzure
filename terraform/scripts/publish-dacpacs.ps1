[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RequiredEnv {
    param([string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable is missing: $Name"
    }

    return $value
}

function Resolve-SqlPackagePath {
    param([string]$Value)

    if (-not [string]::IsNullOrWhiteSpace($Value) -and $Value -ne "sqlpackage") {
        if (-not (Test-Path -LiteralPath $Value)) {
            throw "SqlPackage not found at $Value"
        }

        return $Value
    }

    $command = Get-Command sqlpackage -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "SqlPackage was not found on PATH. Set sqlpackage_path or install SqlPackage."
    }

    return $command.Source
}

function Publish-Dacpac {
    param(
        [string]$ToolPath,
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$AdminUser,
        [string]$AdminPassword,
        [string]$DacpacPath,
        [string]$ExcludeObjectTypes
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
            "/p:DropObjectsNotInSource=False",
            "/p:ExcludeObjectTypes=$ExcludeObjectTypes"
        ) -NoNewWindow -Wait -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath

        $stdout = Get-Content -LiteralPath $stdoutPath -Raw
        $stderr = Get-Content -LiteralPath $stderrPath -Raw
        $output = @($stdout, $stderr) -join ""

        if ($process.ExitCode -ne 0) {
            throw "SqlPackage publish failed for ${DatabaseName}:`n$output"
        }
    }
    finally {
        Remove-Item -LiteralPath $stdoutPath, $stderrPath -ErrorAction SilentlyContinue
    }
}

$dacpacDirectory = Get-RequiredEnv -Name "DACPAC_DIRECTORY"
$sqlPackagePath = Resolve-SqlPackagePath -Value (Get-RequiredEnv -Name "SQLPACKAGE_PATH")
$serverName = Get-RequiredEnv -Name "SQL_SERVER_NAME"
$adminUser = Get-RequiredEnv -Name "SQL_ADMIN_USER"
$adminPassword = Get-RequiredEnv -Name "SQL_ADMIN_PASSWORD"
$excludeObjectTypes = Get-RequiredEnv -Name "EXCLUDE_OBJECT_TYPES"

$databaseNames = (Get-RequiredEnv -Name "DATABASE_NAMES").Split(",", [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() }

if (-not (Test-Path -LiteralPath $dacpacDirectory)) {
    throw "DACPAC directory not found: $dacpacDirectory"
}

foreach ($databaseName in $databaseNames) {
    $dacpacPath = Join-Path $dacpacDirectory "$databaseName.dacpac"
    if (-not (Test-Path -LiteralPath $dacpacPath)) {
        throw "DACPAC not found for database $databaseName at $dacpacPath"
    }

    Write-Host "Publishing $databaseName from $dacpacPath" -ForegroundColor Cyan
    Publish-Dacpac -ToolPath $sqlPackagePath -ServerName $serverName -DatabaseName $databaseName -AdminUser $adminUser -AdminPassword $adminPassword -DacpacPath $dacpacPath -ExcludeObjectTypes $excludeObjectTypes
}

Write-Host "DACPAC publish completed." -ForegroundColor Green