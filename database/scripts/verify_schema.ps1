<#
.SYNOPSIS
    Kiểm tra tự động cơ sở dữ liệu VeggieLife (38 bảng, 312 cột).
.DESCRIPTION
    Chạy query kiểm tra trực tiếp vào SQL Server và báo kết quả PASS/FAIL.
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
$sqlFile = Join-Path $scriptDir "verify_schema.sql"

$authArgs = @()
if ($User -and $Password) {
    $authArgs = @("-U", $User, "-P", $Password)
} else {
    $authArgs = @("-E")
}

Write-Host ">>> Đang chạy kiểm thử nghiệm thu schema trên database [$Database]...`n" -ForegroundColor Cyan

sqlcmd -S $Server -d $Database @authArgs -f 65001 -i $sqlFile -b
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n>>> KIỂM TRA HOÀN TẤT VỚI KẾT QUẢ [PASS]!" -ForegroundColor Green
} else {
    Write-Host "`n>>> KIỂM TRA THẤT BẠI [FAIL]!" -ForegroundColor Red
    exit 1
}
