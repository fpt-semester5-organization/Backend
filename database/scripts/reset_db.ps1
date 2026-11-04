<#
.SYNOPSIS
    Reset database VeggieLifeDb về trạng thái sạch (Drop và Create lại).
.DESCRIPTION
    Script dùng cho phát triển local, đóng tất cả kết nối đang mở và tạo mới database.
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

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VeggieLife Database Reset Tool" -ForegroundColor Cyan
Write-Host " Server:   $Server" -ForegroundColor Gray
Write-Host " Database: $Database" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

$authArgs = @()
if ($User -and $Password) {
    $authArgs = @("-U", $User, "-P", $Password)
} else {
    $authArgs = @("-E")
}

$dropCreateQuery = @"
IF DB_ID('$Database') IS NOT NULL
BEGIN
    ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$Database];
    PRINT N'Đã xóa database [$Database] thành công.';
END
CREATE DATABASE [$Database];
PRINT N'Đã khởi tạo database mới [$Database] thành công.';
"@

try {
    sqlcmd -S $Server @authArgs -C -Q $dropCreateQuery -b
    if ($LASTEXITCODE -eq 0) {
        Write-Host "==> Reset database $Database thành công!" -ForegroundColor Green
    } else {
        Write-Error "Reset database thất bại với exit code $LASTEXITCODE."
    }
} catch {
    Write-Error "Lỗi khi thực thi lệnh sqlcmd: $_"
}