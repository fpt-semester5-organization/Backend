<#
.SYNOPSIS
    Thực thi toàn bộ DDL trong schema/ và master data trong seed/ bằng sqlcmd.
.DESCRIPTION
    Tự động quét các file .sql theo thứ tự tên (01 -> 38) và thực thi lên SQL Server.
#>
[CmdletBinding()]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingWriteHost', '')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', 'Password')]
param (
    [string]$Server = $env:DB_SERVER,
    [string]$Database = $env:DB_NAME,
    [string]$User = $env:DB_USER,
    [string]$Password = $env:DB_PASSWORD
)

$ErrorActionPreference = "Stop"

if (-not $Server) { $Server = "localhost" }
if (-not $Database) { $Database = "VeggieLifeDb" }

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$schemaDir = Join-Path (Split-Path -Parent $scriptDir) "schema"
$seedDir   = Join-Path (Split-Path -Parent $scriptDir) "seed"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VeggieLife Schema & Seed Runner" -ForegroundColor Cyan
Write-Host " Server:   $Server" -ForegroundColor Gray
Write-Host " Database: $Database" -ForegroundColor Gray
Write-Host " Schema:   $schemaDir" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

$authArgs = @()
if ($User -and $Password) {
    $authArgs = @("-U", $User, "-P", $Password)
} else {
    $authArgs = @("-E")
}

# 1. Đảm bảo database đã tồn tại
$ensureDbQuery = "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];"
sqlcmd -S $Server @authArgs -Q $ensureDbQuery -b
if ($LASTEXITCODE -ne 0) {
    Write-Error "Không thể tạo hoặc kết nối database $Database."
    exit 1
}

# 2. Chạy toàn bộ file DDL trong schema/
$schemaFiles = Get-ChildItem -Path $schemaDir -Filter "*.sql" | Sort-Object Name
Write-Host "`n[1/2] Đang thực thi $($schemaFiles.Count) file DDL trong schema/..." -ForegroundColor Yellow

$successCount = 0
foreach ($file in $schemaFiles) {
    Write-Host "  -> Running: $($file.Name)..." -ForegroundColor White -NoNewline
    $result = sqlcmd -S $Server -d $Database @authArgs -I -i $file.FullName -b 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK]" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host " [FAILED]" -ForegroundColor Red
        Write-Error "`nLỗi khi chạy file $($file.Name):`n$result"
        exit 1
    }
}

# 3. Chạy seed data nếu có file
$seedFiles = Get-ChildItem -Path $seedDir -Filter "*.sql" -ErrorAction SilentlyContinue | Sort-Object Name
if ($seedFiles -and $seedFiles.Count -gt 0) {
    Write-Host "`n[2/2] Đang thực thi $($seedFiles.Count) file Seed Data..." -ForegroundColor Yellow
    foreach ($file in $seedFiles) {
        Write-Host "  -> Running: $($file.Name)..." -ForegroundColor White -NoNewline
        $result = sqlcmd -S $Server -d $Database @authArgs -I -i $file.FullName -b 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " [OK]" -ForegroundColor Green
        } else {
            Write-Host " [FAILED]" -ForegroundColor Red
            Write-Error "`nLỗi khi chạy seed data $($file.Name):`n$result"
            exit 1
        }
    }
} else {
    Write-Host "`n[2/2] Không có file seed data nào trong thư mục seed/ (bỏ qua)." -ForegroundColor DarkGray
}

Write-Host "`n============================================================" -ForegroundColor Cyan
Write-Host " Hoàn tất thực thi thành công $successCount file schema!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
