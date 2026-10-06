# ============================================================================
# VeggieLife Backend - EF Core 8 Database-First Scaffold Script
# Description: Reverse engineers SQL Server schema into C# Entities & DbContext.
# ============================================================================

param (
    [string]$Server = "localhost",
    [string]$Database = "VeggieLifeDb"
)

$ErrorActionPreference = "Stop"

$connectionString = "Server=$Server;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Scaffolding EF Core 8 from [$Database] on [$Server]..." -ForegroundColor Cyan
Write-Host " Target DAL Project: src/VeggieLife.DAL" -ForegroundColor Cyan
Write-Host " Startup Project:    src/VeggieLife.API" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer `
    -p src/VeggieLife.DAL -s src/VeggieLife.API `
    --output-dir Entities --context-dir Context --context AppDbContext `
    --no-onconfiguring --force

if ($LASTEXITCODE -eq 0) {
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " SUCCESS: EF Core Scaffolding completed successfully!" -ForegroundColor Green
    Write-Host " Entities: src/VeggieLife.DAL/Entities/" -ForegroundColor Green
    Write-Host " Context:  src/VeggieLife.DAL/Context/AppDbContext.cs" -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host " Scaffolding FAILED!" -ForegroundColor Red
}
