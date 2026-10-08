<#
.SYNOPSIS
    Tự động reverse-engineer (scaffold) database thành Entity POCO và AppDbContext cho VeggieLife.DAL.
.DESCRIPTION
    Gọi lệnh 'dotnet ef dbcontext scaffold' theo quy chuẩn Backend/README.md.
    Sinh code vào thư mục src/VeggieLife.DAL/Entities và src/VeggieLife.DAL/Context.
    Sử dụng cờ --no-onconfiguring và --force để không ghi đè cấu hình trong AppDbContext.Partial.cs.
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
$databaseDir = Split-Path -Parent $scriptDir
$backendDir = Split-Path -Parent $databaseDir
$dalProject = Join-Path $backendDir "src\VeggieLife.DAL\VeggieLife.DAL.csproj"
$apiProject = Join-Path $backendDir "src\VeggieLife.API\VeggieLife.API.csproj"

$startupProject = $dalProject
if (Test-Path $apiProject) {
    $startupProject = $apiProject
}

# Xây dựng Connection String
if ($User -and $Password) {
    $connStr = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;"
} else {
    $connStr = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VeggieLife EF Core Database Scaffold Tool" -ForegroundColor Cyan
Write-Host " Server:          $Server" -ForegroundColor Gray
Write-Host " Database:        $Database" -ForegroundColor Gray
Write-Host " Target Project:  $dalProject" -ForegroundColor Gray
Write-Host " Startup Project: $startupProject" -ForegroundColor Gray
Write-Host " Output Dir:      Entities" -ForegroundColor Gray
Write-Host " Context Dir:     Context (AppDbContext)" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

# Kiểm tra dotnet ef tool đã cài đặt chưa
try {
    dotnet ef --version | Out-Null
} catch {
    Write-Host "Đang cài đặt dotnet-ef tool (global)..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}

Write-Host "`nĐang khôi phục NuGet packages (dotnet restore)..." -ForegroundColor Yellow
dotnet restore $dalProject

Write-Host "`nĐang thực thi Scaffold từ database..." -ForegroundColor Yellow

$scaffoldArgs = @(
    "ef", "dbcontext", "scaffold",
    $connStr,
    "Microsoft.EntityFrameworkCore.SqlServer",
    "-p", $dalProject,
    "-s", $startupProject,
    "--output-dir", "Entities",
    "--context-dir", "Context",
    "--context", "AppDbContext",
    "--no-onconfiguring",
    "--force"
)

& dotnet @scaffoldArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n==> Scaffold hoàn tất thành công!" -ForegroundColor Green

    Write-Host "Đang biên dịch kiểm tra dự án VeggieLife.DAL..." -ForegroundColor Yellow
    & dotnet build $dalProject --no-incremental
    if ($LASTEXITCODE -eq 0) {
        Write-Host "==> Dự án VeggieLife.DAL biên dịch thành công 100%!" -ForegroundColor Green
    } else {
        Write-Warning "Cảnh báo: Có lỗi biên dịch sau khi scaffold. Vui lòng kiểm tra lại Partial Context."
    }
} else {
    Write-Error "Lệnh scaffold thất bại với exit code $LASTEXITCODE."
    exit 1
}