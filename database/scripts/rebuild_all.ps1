<#
.SYNOPSIS
    Chạy toàn bộ quy trình tự động hóa Database: Reset DB -> Chạy DDL Schema -> Scaffold EF Core Entities.
.DESCRIPTION
    Tiện ích 1-click cho lập trình viên để cập nhật toàn bộ database và code DAL.
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

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "============================================================" -ForegroundColor Magenta
Write-Host " VeggieLife Full Database & Model Rebuild Pipeline" -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta

# Bước 1: Reset database
Write-Host "`n>>> BƯỚC 1: RESET DATABASE <<<" -ForegroundColor Cyan
& (Join-Path $scriptDir "reset_db.ps1") -Server $Server -Database $Database -User $User -Password $Password
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Bước 2: Chạy schema và seed
Write-Host "`n>>> BƯỚC 2: CHẠY SCHEMA DDL & SEED DATA <<<" -ForegroundColor Cyan
& (Join-Path $scriptDir "run_all.ps1") -Server $Server -Database $Database -User $User -Password $Password
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Bước 3: Scaffold EF Core Model
Write-Host "`n>>> BƯỚC 3: SCAFFOLD EF CORE MODEL & BUILD DAL <<<" -ForegroundColor Cyan
& (Join-Path $scriptDir "scaffold.ps1") -Server $Server -Database $Database -User $User -Password $Password
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host " TẤT CẢ CÁC BƯỚC ĐÃ HOÀN TẤT THÀNH CÔNG!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
