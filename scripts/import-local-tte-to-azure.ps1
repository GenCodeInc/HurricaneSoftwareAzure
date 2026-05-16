[CmdletBinding()]
param(
    [string]$SourceServer = "localhost",
    [string]$SourceDatabase = "TTE",
    [switch]$UseSourceTrustedConnection = $true,
    [string]$SourceUser,
    [string]$SourcePassword,
    [string]$TargetServer = "tcp:sql-gencode-cu-66c7.database.windows.net,1433",
    [string]$TargetDatabase = "TTE",
    [string]$TargetUser = "<user>",
    [string]$TargetPassword,
    [string]$WorkingDirectory = "C:\DBBackups\TTE_bcp",
    [switch]$KeepDataFiles,
    [switch]$SkipDelete,
    [int]$BatchSize = 5000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Command {
    param([string]$Name)

    $command = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if (-not $command) {
        throw "Required command not found: $Name"
    }

    return $command.Source
}

function Invoke-SqlCmdText {
    param(
        [string]$Server,
        [string]$Database,
        [string]$Query,
        [switch]$TrustedConnection,
        [string]$User,
        [string]$Password,
        [switch]$TrimHeader
    )

    $arguments = @(
        "-S", $Server,
        "-d", $Database,
        "-Q", $Query,
        "-W",
        "-h", "-1",
        "-s", "|"
    )

    if ($TrustedConnection) {
        $arguments += "-E"
    }
    else {
        $arguments += @("-U", $User, "-P", $Password)
    }

    $output = & sqlcmd @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed against ${Server}/${Database}:`n$($output | Out-String)"
    }

    $text = ($output | Out-String).Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return @()
    }

    $lines = $text -split "`r?`n"
    return $lines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
}

function Quote-SqlIdentifier {
    param([string]$Name)

    return "[" + $Name.Replace("]", "]]") + "]"
}

function Get-TableMetadata {
    param(
        [string]$Server,
        [string]$Database,
        [switch]$TrustedConnection,
        [string]$User,
        [string]$Password
    )

    $query = @"
SELECT s.name, t.name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;
"@

    $lines = Invoke-SqlCmdText -Server $Server -Database $Database -Query $query -TrustedConnection:$TrustedConnection -User $User -Password $Password
    $tables = foreach ($line in $lines) {
        $parts = $line.Split("|", 2)
        if ($parts.Count -eq 2) {
            [pscustomobject]@{
                Schema = $parts[0].Trim()
                Table  = $parts[1].Trim()
            }
        }
    }

    return $tables
}

function Invoke-TargetSql {
    param([string]$Query)

    Invoke-SqlCmdText -Server $TargetServer -Database $TargetDatabase -Query $Query -User $TargetUser -Password $TargetPassword | Out-Null
}

function Invoke-BcpExport {
    param(
        [string]$Schema,
        [string]$Table,
        [string]$OutputFile
    )

    $objectName = "$Schema.$Table"
    $arguments = @($objectName, "out", $OutputFile, "-S", $SourceServer, "-d", $SourceDatabase)

    if ($UseSourceTrustedConnection) {
        $arguments += "-T"
    }
    else {
        $arguments += @("-U", $SourceUser, "-P", $SourcePassword)
    }

    $arguments += @("-n")

    Write-Host "Exporting $objectName" -ForegroundColor Cyan
    $output = & bcp @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "bcp export failed for ${objectName}:`n$($output | Out-String)"
    }
}

function Invoke-BcpImport {
    param(
        [string]$Schema,
        [string]$Table,
        [string]$InputFile
    )

    $objectName = "$Schema.$Table"
    $arguments = @(
        $objectName,
        "in",
        $InputFile,
        "-S", $TargetServer,
        "-d", $TargetDatabase,
        "-U", $TargetUser,
        "-P", $TargetPassword,
        "-n",
        "-E",
        "-b", $BatchSize
    )

    Write-Host "Importing $objectName" -ForegroundColor Cyan
    $output = & bcp @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "bcp import failed for ${objectName}:`n$($output | Out-String)"
    }
}

if (-not $UseSourceTrustedConnection) {
    if ([string]::IsNullOrWhiteSpace($SourceUser) -or [string]::IsNullOrWhiteSpace($SourcePassword)) {
        throw "SourceUser and SourcePassword are required when not using trusted connection."
    }
}

if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw "TargetPassword is required. Pass -TargetPassword '<pwd>'."
}

Require-Command -Name "bcp" | Out-Null
Require-Command -Name "sqlcmd" | Out-Null

New-Item -ItemType Directory -Path $WorkingDirectory -Force | Out-Null

$tables = Get-TableMetadata -Server $SourceServer -Database $SourceDatabase -TrustedConnection:$UseSourceTrustedConnection -User $SourceUser -Password $SourcePassword
if (-not $tables -or $tables.Count -eq 0) {
    throw "No user tables found in $SourceServer/$SourceDatabase."
}

$qualifiedTables = $tables | ForEach-Object { "$(Quote-SqlIdentifier $_.Schema).$(Quote-SqlIdentifier $_.Table)" }

Write-Host "Disabling target constraints" -ForegroundColor Yellow
$disableSql = ($qualifiedTables | ForEach-Object { "ALTER TABLE $_ NOCHECK CONSTRAINT ALL;" }) -join " "
Invoke-TargetSql -Query $disableSql

if (-not $SkipDelete) {
    Write-Host "Deleting existing target rows" -ForegroundColor Yellow
    $deleteSql = ($qualifiedTables | ForEach-Object { "DELETE FROM $_;" }) -join " "
    Invoke-TargetSql -Query $deleteSql
}

try {
    foreach ($table in $tables) {
        $fileName = "{0}.{1}.dat" -f $table.Schema, $table.Table
        $dataFile = Join-Path $WorkingDirectory $fileName

        Invoke-BcpExport -Schema $table.Schema -Table $table.Table -OutputFile $dataFile
        Invoke-BcpImport -Schema $table.Schema -Table $table.Table -InputFile $dataFile
    }

    Write-Host "Re-enabling target constraints" -ForegroundColor Yellow
    $enableSql = ($qualifiedTables | ForEach-Object { "ALTER TABLE $_ WITH CHECK CHECK CONSTRAINT ALL;" }) -join " "
    Invoke-TargetSql -Query $enableSql
}
finally {
    try {
        $enableSql = ($qualifiedTables | ForEach-Object { "ALTER TABLE $_ WITH CHECK CHECK CONSTRAINT ALL;" }) -join " "
        Invoke-TargetSql -Query $enableSql
    }
    catch {
        Write-Warning "Could not re-enable one or more target constraints automatically. Review the target database before continuing."
    }

    if (-not $KeepDataFiles) {
        Get-ChildItem -LiteralPath $WorkingDirectory -Filter *.dat -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "TTE migration completed." -ForegroundColor Green