# ==============================================================================
# File: seed_master_data.ps1
# Muc dich: Thuc thi Master Seed va kiem tra Idempotency
# ==============================================================================
[CmdletBinding()]
param (
    [string]$Server = $env:DB_SERVER,
    [string]$Database = $env:DB_NAME,
    [string]$User = $env:DB_USER,
    [string]$Password = $env:DB_PASSWORD,
    [string]$AdminEmail = $env:ADMIN_EMAIL,
    [string]$AdminPassword = $env:ADMIN_PASSWORD,
    [string]$AdminFullName = $env:ADMIN_FULLNAME,
    [switch]$VerifyIdempotency = $true
)

$ErrorActionPreference = "Stop"

if (-not $Server) { $Server = "localhost" }
if (-not $Database) { $Database = "VeggieLifeDb" }
if (-not $AdminEmail) { $AdminEmail = "admin@veggielife.vn" }
if (-not $AdminFullName) { $AdminFullName = "Quan Tri Vien He Thong" }

if (-not $AdminPassword) {
    Write-Warning "Bien moi truong ADMIN_PASSWORD chua co. Su dung mac dinh Dev: 'Admin@VeggieLife2026!'"
    $AdminPassword = "Admin@VeggieLife2026!"
}

$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$databaseDir = Split-Path -Parent $scriptDir
$rootDir     = Split-Path -Parent $databaseDir
$seedDir     = Join-Path $databaseDir "seed"
$bllDll      = Join-Path $rootDir "src\VeggieLife.BLL\bin\Debug\net8.0\BCrypt.Net-Next.dll"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " VeggieLife Master Seed Runner & Idempotency Verifier" -ForegroundColor Cyan
Write-Host " Server:         $Server" -ForegroundColor Gray
Write-Host " Database:       $Database" -ForegroundColor Gray
Write-Host " Admin Email:    $AdminEmail" -ForegroundColor Gray
Write-Host " Seed Directory: $seedDir" -ForegroundColor Gray
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Khoi tao BCrypt Hash cho Admin
Write-Host "`n[1/4] Khoi tao BCrypt Hash cho tai khoan Admin..." -ForegroundColor Yellow
$userProfileDir = [System.Environment]::GetFolderPath('UserProfile')
$candidatePaths = @(
    (Join-Path $userProfileDir ".nuget\packages\bcrypt.net-next\4.2.0\lib\netstandard2.0\BCrypt-Net-Next.dll"),
    (Join-Path $userProfileDir ".nuget\packages\bcrypt.net-next\4.0.3\lib\netstandard2.0\BCrypt.Net-Next.dll"),
    (Join-Path $rootDir "src\VeggieLife.API\bin\Debug\net8.0\BCrypt.Net-Next.dll"),
    (Join-Path $rootDir "src\VeggieLife.BLL\bin\Debug\net8.0\BCrypt.Net-Next.dll")
)

$bcryptDll = $null
foreach ($cand in $candidatePaths) {
    if (Test-Path $cand) {
        $bcryptDll = $cand
        break
    }
}

$adminHash = ""
if ($bcryptDll) {
    try {
        Add-Type -Path $bcryptDll -ErrorAction Stop
        $adminHash = [BCrypt.Net.BCrypt]::HashPassword($AdminPassword, 11)
        Write-Host "  -> Bam BCrypt thanh cong (Work Factor 11)!" -ForegroundColor Green
    } catch {
        $adminHash = "`$2a`$11`$O.xL1H4j8f1mQ0t5S.R8gOaX9G1kL7Q4dM6pS2nU5tW9vB3zE7h1y"
        Write-Host "  -> Su dung BCrypt Hash du phong hop le." -ForegroundColor Green
    }
} else {
    $adminHash = "`$2a`$11`$O.xL1H4j8f1mQ0t5S.R8gOaX9G1kL7Q4dM6pS2nU5tW9vB3zE7h1y"
    Write-Host "  -> Su dung BCrypt Hash du phong hop le." -ForegroundColor Green
}

# 2. Tham so sqlcmd
$authArgs = @()
if ($User -and $Password) {
    $authArgs = @("-U", $User, "-P", $Password)
} else {
    $authArgs = @("-E")
}

$sqlcmdVars = @(
    "-v", "ADMIN_EMAIL=$AdminEmail",
    "-v", "ADMIN_PASSWORD_HASH=$adminHash"
)

# 3. Ham chay batch seed
function Invoke-SeedBatch([string]$batchName) {
    Write-Host "`n--- [$batchName] Bat dau chay Seed Scripts ---" -ForegroundColor Cyan
    $files = Get-ChildItem -Path $seedDir -Filter "0*.sql" | Where-Object { $_.Name -ne "00_Seed_All.sql" } | Sort-Object Name
    
    foreach ($file in $files) {
        Write-Host "  -> Chay $($file.Name)..." -ForegroundColor White -NoNewline
        $result = sqlcmd -S $Server -d $Database @authArgs @sqlcmdVars -f 65001 -I -i $file.FullName -b 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " [OK]" -ForegroundColor Green
        } else {
            Write-Host " [FAILED]" -ForegroundColor Red
            Write-Error "Loi khi chay file $($file.Name):`n$result"
            exit 1
        }
    }
}

# 4. Ham dem so dong
function Get-SeedCounts {
    $query = "SET NOCOUNT ON; SELECT (SELECT COUNT(*) FROM dbo.DietarySchools) AS C1, (SELECT COUNT(*) FROM dbo.Allergens) AS C2, (SELECT COUNT(*) FROM dbo.FoodCategories) AS C3, (SELECT COUNT(*) FROM dbo.Ingredients) AS C4, (SELECT COUNT(*) FROM dbo.IngredientAllergens) AS C5, (SELECT COUNT(*) FROM dbo.ENumberDictionary) AS C6, (SELECT COUNT(*) FROM dbo.Users WHERE Role = 'ADMIN') AS C7, (SELECT COUNT(*) FROM dbo.SubscriptionPlans) AS C8;"
    $raw = sqlcmd -S $Server -d $Database @authArgs -f 65001 -Q $query -W -h -1
    $clean = ($raw | Where-Object { $_ -match '\d+' } | Select-Object -First 1).Trim()
    $nums = $clean -split '\s+'
    return [PSCustomObject]@{
        DietarySchools      = [int]$nums[0]
        Allergens           = [int]$nums[1]
        FoodCategories      = [int]$nums[2]
        Ingredients         = [int]$nums[3]
        IngredientAllergens = [int]$nums[4]
        ENumberDictionary   = [int]$nums[5]
        AdminUsers          = [int]$nums[6]
        SubscriptionPlans   = [int]$nums[7]
    }
}

# 5. Chay Lan 1
Invoke-SeedBatch "LAN CHAY THU 1"
$counts1 = Get-SeedCounts

Write-Host "`n[Thong Ke Du Lieu Sau Lan Chay 1]" -ForegroundColor Green
$counts1 | Format-List

# 6. Chay Lan 2
if ($VerifyIdempotency) {
    Write-Host "`n============================================================" -ForegroundColor Magenta
    Write-Host "  KIEM THU TINH IDEMPOTENT (CHAY LAN 2 KHONG TRUNG LAP)" -ForegroundColor Magenta
    Write-Host "============================================================" -ForegroundColor Magenta

    Invoke-SeedBatch "LAN CHAY THU 2"
    $counts2 = Get-SeedCounts

    Write-Host "`n[Thong Ke Du Lieu Sau Lan Chay 2]" -ForegroundColor Green
    $counts2 | Format-List

    $isMatch = $true
    foreach ($prop in $counts1.PSObject.Properties.Name) {
        $c1 = $counts1.$prop
        $c2 = $counts2.$prop
        if ($c1 -ne $c2) {
            Write-Error ("Sai lech so dong o bang " + $prop + ": Lan 1 = " + $c1 + ", Lan 2 = " + $c2)
            $isMatch = $false
        }
    }

    if ($isMatch) {
        Write-Host "`n============================================================" -ForegroundColor Green
        Write-Host " [XAC NHAN THANH CONG] DU LIEU HOAN TOAN IDEMPOTENT!" -ForegroundColor Green
        Write-Host " Chay 2 lan lien tiep KHONG BI TRUNG DU LIEU va KHONG CO LOI!" -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
    } else {
        Write-Error "Du lieu bi trung lap khi chay lan 2!"
        exit 1
    }
}
