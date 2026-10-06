#!/usr/bin/env bash
# ============================================================================
# VeggieLife Backend - EF Core 8 Database-First Scaffold Script (Bash)
# Description: Reverse engineers SQL Server schema into C# Entities & DbContext.
# ============================================================================

set -e

SERVER="${1:-localhost}"
DATABASE="${2:-VeggieLifeDb}"
CONNECTION_STRING="Server=${SERVER};Database=${DATABASE};Trusted_Connection=True;TrustServerCertificate=True;"

echo "=========================================================="
echo " Scaffolding EF Core 8 from [${DATABASE}] on [${SERVER}]..."
echo "=========================================================="

dotnet ef dbcontext scaffold "${CONNECTION_STRING}" Microsoft.EntityFrameworkCore.SqlServer \
  -p src/VeggieLife.DAL -s src/VeggieLife.API \
  --output-dir Entities --context-dir Context --context AppDbContext \
  --no-onconfiguring --force

echo "=========================================================="
echo " SUCCESS: EF Core Scaffolding completed successfully!"
echo "=========================================================="
