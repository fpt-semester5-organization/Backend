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

if (-not $Server) { $Server = "localhost" }
if (-not $Database) { $Database = "VeggieLifeDb" }

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
    PRINT 'Đã xóa database [$Database] thành công.';
END
CREATE DATABASE [$Database];
PRINT 'Đã khởi tạo database mới [$Database] thành công.';
"@

try {
    sqlcmd -S $Server @authArgs -Q $dropCreateQuery -b
    if ($LASTEXITCODE -eq 0) {
        Write-Host "==> Reset database $Database thành công!" -ForegroundColor Green
    } else {
        Write-Error "Reset database thất bại với exit code $LASTEXITCODE."
    }
} catch {
    Write-Error "Lỗi khi thực thi lệnh sqlcmd: $_"
}
