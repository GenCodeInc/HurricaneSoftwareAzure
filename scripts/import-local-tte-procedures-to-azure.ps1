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
    [string]$WorkingDirectory = "C:\DBBackups\TTE_procedures",
    [switch]$ContinueOnError = $true,
    [switch]$SkipUnsupportedPatterns = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function New-SqlConnection {
    param(
        [string]$Server,
        [string]$Database,
        [switch]$TrustedConnection,
        [string]$User,
        [string]$Password,
        [switch]$Encrypt,
        [switch]$TrustServerCertificate
    )

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
    $builder["Data Source"] = $Server
    $builder["Initial Catalog"] = $Database
    $builder["Integrated Security"] = [bool]$TrustedConnection
    if (-not $TrustedConnection) {
        $builder["User ID"] = $User
        $builder["Password"] = $Password
    }
    $builder["Encrypt"] = [bool]$Encrypt
    $builder["TrustServerCertificate"] = [bool]$TrustServerCertificate
    $builder["Connect Timeout"] = 30

    $connection = New-Object System.Data.SqlClient.SqlConnection $builder.ConnectionString
    $connection.Open()
    return $connection
}

function Invoke-ReaderQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Query
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Query
    $command.CommandTimeout = 0

    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    return ,$table
}

function Invoke-NonQuery {
    param(
        [System.Data.SqlClient.SqlConnection]$Connection,
        [string]$Query
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = $Query
    $command.CommandTimeout = 0
    [void]$command.ExecuteNonQuery()
}

function Normalize-ProcedureDefinition {
    param([string]$Definition)

    $normalized = $Definition.Trim()
    $pattern = '^(\s*)(CREATE|ALTER)(\s+PROC(?:EDURE)?\s+)'
    return [System.Text.RegularExpressions.Regex]::Replace(
        $normalized,
        $pattern,
        '$1CREATE OR ALTER$3',
        [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
    )
}

function Test-UnsupportedPattern {
    param([string]$Definition)

    $patterns = @(
        'BULK\s+INSERT',
        'OPENROWSET',
        'OPENDATASOURCE',
        'xp_',
        'sp_OA',
        '\bUSE\b\s+\[',
        '\.\.[A-Za-z_\[]'
    )

    foreach ($pattern in $patterns) {
        if ($Definition -match $pattern) {
            return $pattern
        }
    }

    return $null
}

if (-not $UseSourceTrustedConnection) {
    if ([string]::IsNullOrWhiteSpace($SourceUser) -or [string]::IsNullOrWhiteSpace($SourcePassword)) {
        throw "SourceUser and SourcePassword are required when not using trusted connection."
    }
}

if ([string]::IsNullOrWhiteSpace($TargetPassword)) {
    throw "TargetPassword is required. Pass -TargetPassword '<pwd>'."
}

New-Item -ItemType Directory -Path $WorkingDirectory -Force | Out-Null
$logPath = Join-Path $WorkingDirectory "procedure-import.log"
New-Item -ItemType File -Path $logPath -Force | Out-Null

$sourceConnection = $null
$targetConnection = $null

try {
    $sourceConnection = New-SqlConnection -Server $SourceServer -Database $SourceDatabase -TrustedConnection:$UseSourceTrustedConnection -User $SourceUser -Password $SourcePassword -TrustServerCertificate
    $targetConnection = New-SqlConnection -Server $TargetServer -Database $TargetDatabase -User $TargetUser -Password $TargetPassword -Encrypt -TrustServerCertificate:$false

    $procedureTable = Invoke-ReaderQuery -Connection $sourceConnection -Query @"
SELECT
    s.name AS SchemaName,
    p.name AS ProcedureName,
    m.definition AS Definition
FROM sys.procedures p
JOIN sys.schemas s ON s.schema_id = p.schema_id
JOIN sys.sql_modules m ON m.object_id = p.object_id
WHERE p.is_ms_shipped = 0
ORDER BY s.name, p.name;
"@

    if ($procedureTable.Rows.Count -eq 0) {
        throw "No user stored procedures found in $SourceServer/$SourceDatabase."
    }

    foreach ($row in $procedureTable.Rows) {
        $schemaName = [string]$row.SchemaName
        $procedureName = [string]$row.ProcedureName
        $definition = [string]$row.Definition
        $fullName = "$schemaName.$procedureName"

        $unsupportedPattern = $null
        if ($SkipUnsupportedPatterns) {
            $unsupportedPattern = Test-UnsupportedPattern -Definition $definition
        }

        if ($unsupportedPattern) {
            $message = "SKIPPED $fullName because it matched unsupported pattern '$unsupportedPattern'."
            Add-Content -LiteralPath $logPath -Value $message
            Write-Warning $message
            continue
        }

        $normalizedDefinition = Normalize-ProcedureDefinition -Definition $definition
        $sqlFilePath = Join-Path $WorkingDirectory ("{0}.{1}.sql" -f $schemaName, $procedureName)
        Set-Content -LiteralPath $sqlFilePath -Value $normalizedDefinition -Encoding UTF8

        try {
            Write-Host "Applying procedure $fullName" -ForegroundColor Cyan
            Invoke-NonQuery -Connection $targetConnection -Query $normalizedDefinition
            Add-Content -LiteralPath $logPath -Value "IMPORTED $fullName"
        }
        catch {
            $message = "FAILED $fullName - $($_.Exception.Message)"
            Add-Content -LiteralPath $logPath -Value $message
            Write-Warning $message

            if (-not $ContinueOnError) {
                throw
            }
        }
    }
}
finally {
    if ($sourceConnection) {
        $sourceConnection.Dispose()
    }
    if ($targetConnection) {
        $targetConnection.Dispose()
    }
}

Write-Host "Procedure import finished. Review log: $logPath" -ForegroundColor Green