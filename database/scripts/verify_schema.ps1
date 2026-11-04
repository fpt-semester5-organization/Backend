<#
.SYNOPSIS
    Kiểm tra tự động cơ sở dữ liệu VeggieLife (41 bảng, 343 cột).
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

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
$OutputEncoding           = [System.Text.Encoding]::UTF8
chcp 65001 | Out-Null

if (-not $Server) { $Server = "localhost,1433" }
if (-not $Database) { $Database = "VeggieLifeDb" }
if (-not $User -and -not $Password) {
    if ($env:DB_USER) {
        $User = $env:DB_USER
        $Password = $env:DB_PASSWORD
    } else {
        $User = "sa"
        $Password = "12345"
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlFile = Join-Path $scriptDir "verify_schema.sql"

$authArgs = @()
if ($User -and $Password) {
    $authArgs = @("-U", $User, "-P", $Password)
} else {
    $authArgs = @("-E")
}

Write-Host ">>> Đang chạy kiểm thử nghiệm thu schema trên database [$Database]...`n" -ForegroundColor Cyan

sqlcmd -S $Server -d $Database @authArgs -C -f 65001 -i $sqlFile -b
if ($LASTEXITCODE -eq 0) {
    Write-Host "`n>>> KIỂM TRA HOÀN TẤT VỚI KẾT QUẢ [PASS]!" -ForegroundColor Green
} else {
    Write-Host "`n>>> KIỂM TRA THẤT BẠI [FAIL]!" -ForegroundColor Red
    exit 1
}