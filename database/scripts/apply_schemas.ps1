# ============================================================================
# VeggieLifeDb - Automated Schema Runner
# Description: Executes all DDL .sql files in database\schema in alphanumeric order.
# ============================================================================

param (
    [string]$Server = "localhost",
    [string]$Database = "VeggieLifeDb"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$schemaDir = Join-Path (Split-Path -Parent $scriptDir) "schema"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Applying schemas to [$Database] on [$Server]..." -ForegroundColor Cyan
Write-Host " Schema Directory: $schemaDir" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$sqlFiles = Get-ChildItem -Path "$schemaDir\*.sql" | Sort-Object Name

if ($sqlFiles.Count -eq 0) {
    Write-Warning "No .sql schema files found in $schemaDir."
    exit 0
}

foreach ($file in $sqlFiles) {
    Write-Host " -> Executing: $($file.Name)..." -NoNewline
    $result = sqlcmd -S $Server -E -d $Database -I -b -i $file.FullName 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host " [FAILED]" -ForegroundColor Red
        Write-Error $result
        exit 1
    } else {
        Write-Host " [OK]" -ForegroundColor Green
    }
}

Write-Host "==========================================================" -ForegroundColor Green
Write-Host " SUCCESS: All $($sqlFiles.Count) schemas applied successfully!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green

# Display current tables
Write-Host "`nCurrent tables in [$Database]:" -ForegroundColor Yellow
sqlcmd -S $Server -E -d $Database -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME;"
