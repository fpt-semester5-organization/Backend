<#
.SYNOPSIS
    Script kiểm thử tự động toàn diện các kịch bản của FN-PROF-03 và FN-PROF-06.
.DESCRIPTION
    Kiểm thử:
    1. Tệp giả mạo đuôi .jpg (nội dung text thuần) bị từ chối với HTTP 400 ERR_MEDIA_INVALID_FILE.
    2. Tệp vượt quá 5 MB bị từ chối với HTTP 413 ERR_MEDIA_FILE_TOO_LARGE.
    3. Gọi API không kèm token xác thực bị từ chối với HTTP 401 ERR_AUTH_INVALID_TOKEN.
    4. Khách vãng lai (Guest) gọi tính năng cá nhân hóa bị từ chối với HTTP 403 ERR_GUEST_PERSONALIZATION_RESTRICTED.
    5. Ghi nhận mốc cân nặng hợp lệ thành công với HTTP 201 và tự động tính toán lại BMI/BMR/TDEE/Calo mục tiêu.
    6. Ghi cân nặng ngoài khoảng 20-300 kg bị từ chối với HTTP 400 ERR_VALIDATION.
    7. Ghi cân nặng ngày ở tương lai bị từ chối với HTTP 400 ERR_VALIDATION.
    8. Tra cứu lịch sử cân nặng có phân trang thành công với HTTP 200 (Envelope D52).
    9. Tra cứu lịch sử cân nặng với khoảng ngày sai (from > to) bị từ chối với HTTP 400 ERR_VALIDATION.
#>

param (
    [string]$BaseUrl = "http://localhost:5137"
)

$ErrorActionPreference = "Continue"

function Write-TestHeader($msg) {
    Write-Host "`n========================================================" -ForegroundColor Cyan
    Write-Host ">> $msg" -ForegroundColor Cyan
    Write-Host "========================================================" -ForegroundColor Cyan
}

function Write-TestResult($testName, $isSuccess, $detail) {
    if ($isSuccess) {
        Write-Host "  [PASS] $testName" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $testName - $detail" -ForegroundColor Red
    }
}

# 1. Hàm sinh JWT Token chuẩn HS256 cho môi trường Development
function New-DevJwtToken($userId, $role) {
    $secret = "VeggieLifeSecretKeyForDevelopmentMustBe32CharsLong!"
    $issuer = "VeggieLife.API"
    $audience = "VeggieLife.Client"

    $header = @{
        alg = "HS256"
        typ = "JWT"
    }
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $exp = $now + 7200

    $payload = @{
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" = "$userId"
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = "$role"
        "sub" = "$userId"
        "role" = "$role"
        "iss" = $issuer
        "aud" = $audience
        "nbf" = $now
        "exp" = $exp
        "iat" = $now
    }

    $headerJson = $header | ConvertTo-Json -Compress
    $payloadJson = $payload | ConvertTo-Json -Compress

    $headerB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($headerJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')
    $payloadB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payloadJson)).Split('=')[0].Replace('+', '-').Replace('/', '_')

    $toSign = "$headerB64.$payloadB64"
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [System.Text.Encoding]::UTF8.GetBytes($secret)
    $signatureBytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($toSign))
    $signatureB64 = [Convert]::ToBase64String($signatureBytes).Split('=')[0].Replace('+', '-').Replace('/', '_')

    return "$headerB64.$payloadB64.$signatureB64"
}

$memberToken = New-DevJwtToken -userId 2 -role "MEMBER"
$guestToken = New-DevJwtToken -userId 999 -role "GUEST"

Write-TestHeader "BẮT ĐẦU KIỂM THỬ TÍCH HỢP TỰ ĐỘNG FN-PROF-03 & FN-PROF-06"
Write-Host "Target API: $BaseUrl" -ForegroundColor Yellow

$passedCount = 0
$failedCount = 0

# Helper gửi HTTP request không văng exception khi gặp mã lỗi 4xx/5xx
function Invoke-ApiRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [string]$ContentType = "application/json"
    )

    try {
        $params = @{
            Uri = $Uri
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
        }
        if ($Body) {
            if ($ContentType -like "*multipart*") {
                $params["Body"] = [System.Text.Encoding]::GetEncoding("iso-8859-1").GetBytes($Body)
            } elseif ($Body -is [string]) {
                $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body)
            } else {
                $params["Body"] = $Body
            }
            $params["ContentType"] = $ContentType
        }
        $resp = Invoke-WebRequest @params
        return @{
            StatusCode = [int]$resp.StatusCode
            Content = $resp.Content
        }
    }
    catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        if ($resp) {
            $stream = $resp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            return @{
                StatusCode = [int]$resp.StatusCode
                Content = $body
            }
        }
        return @{ StatusCode = 0; Content = $_.Exception.Message }
    }
    catch {
        return @{ StatusCode = 0; Content = $_.Exception.Message }
    }
}

# =========================================================================
# TEST CASE 1: Upload tệp giả mạo đuôi .jpg (magic bytes không phải ảnh) -> 400 ERR_MEDIA_INVALID_FILE
# =========================================================================
$fakeFile = [System.IO.Path]::GetTempFileName() + ".jpg"
[System.IO.File]::WriteAllText($fakeFile, "THIS IS PURE TEXT NOT A REAL JPEG")
try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $fileBytes = [System.IO.File]::ReadAllBytes($fakeFile)
    $fileName = [System.IO.Path]::GetFileName($fakeFile)
    
    $enc = [System.Text.Encoding]::GetEncoding("iso-8859-1")
    $body = "--$boundary`r`nContent-Disposition: form-data; name=`"file`"; filename=`"$fileName`"`r`nContent-Type: image/jpeg`r`n`r`n"
    $body += $enc.GetString($fileBytes)
    $body += "`r`n--$boundary--`r`n"

    $resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/avatar" -Method "POST" `
        -Headers @{ "Authorization" = "Bearer $memberToken" } `
        -Body $body -ContentType "multipart/form-data; boundary=$boundary"

    $isPass = ($resp.StatusCode -eq 400) -and ($resp.Content -match "ERR_MEDIA_INVALID_FILE")
    if ($isPass) { $passedCount++ } else { $failedCount++ }
    Write-TestResult "TC1: Upload tệp giả đuôi .jpg -> 400 ERR_MEDIA_INVALID_FILE" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"
}
finally {
    if (Test-Path $fakeFile) { Remove-Item $fakeFile -Force }
}

# =========================================================================
# TEST CASE 2: Upload tệp quá 5 MB -> 413 ERR_MEDIA_FILE_TOO_LARGE
# =========================================================================
$largeFile = [System.IO.Path]::GetTempFileName() + ".png"
$largeBytes = New-Object byte[] (5 * 1024 * 1024 + 1024 * 512) # 5.5 MB
[System.IO.File]::WriteAllBytes($largeFile, $largeBytes)
try {
    $boundary = [System.Guid]::NewGuid().ToString()
    $enc = [System.Text.Encoding]::GetEncoding("iso-8859-1")
    $body = "--$boundary`r`nContent-Disposition: form-data; name=`"file`"; filename=`"large.png`"`r`nContent-Type: image/png`r`n`r`n"
    $body += $enc.GetString($largeBytes)
    $body += "`r`n--$boundary--`r`n"

    $resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/avatar" -Method "POST" `
        -Headers @{ "Authorization" = "Bearer $memberToken" } `
        -Body $body -ContentType "multipart/form-data; boundary=$boundary"

    $isPass = ($resp.StatusCode -eq 413) -and ($resp.Content -match "ERR_MEDIA_FILE_TOO_LARGE")
    if ($isPass) { $passedCount++ } else { $failedCount++ }
    Write-TestResult "TC2: Upload tệp > 5 MB -> 413 ERR_MEDIA_FILE_TOO_LARGE" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"
}
finally {
    if (Test-Path $largeFile) { Remove-Item $largeFile -Force }
}

# =========================================================================
# TEST CASE 3: Không có Bearer Token -> 401 ERR_AUTH_INVALID_TOKEN
# =========================================================================
$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "GET"
$isPass = ($resp.StatusCode -eq 401) -and ($resp.Content -match "ERR_AUTH_INVALID_TOKEN")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC3: Gọi API không mang Bearer Token -> 401 ERR_AUTH_INVALID_TOKEN" $isPass "Status=$($resp.StatusCode)"

# =========================================================================
# TEST CASE 4: Khách vãng lai (Guest) -> 403 ERR_GUEST_PERSONALIZATION_RESTRICTED
# =========================================================================
$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "GET" `
    -Headers @{ "Authorization" = "Bearer $guestToken" }
$isPass = ($resp.StatusCode -eq 403) -and ($resp.Content -match "ERR_GUEST_PERSONALIZATION_RESTRICTED")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC4: Khách vãng lai truy cập cá nhân hóa -> 403 ERR_GUEST_PERSONALIZATION_RESTRICTED" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"

# =========================================================================
# TEST CASE 5: Ghi cân nặng mới hợp lệ -> 201 Created và tính lại các chỉ số
# =========================================================================
$todayStr = [DateTime]::UtcNow.ToString("yyyy-MM-dd")
$weightBody = "{`"weightKg`": 54.0, `"loggedDate`": `"$todayStr`", `"note`": `"Kiem thu can nang`"}"

$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "POST" `
    -Headers @{ "Authorization" = "Bearer $memberToken" } `
    -Body $weightBody

if ($resp.StatusCode -eq 409 -and $resp.Content -match "ERR_PROFILE_NOT_COMPLETED") {
    $passedCount++
    Write-TestResult "TC5a: Ghi cân nặng khi chưa có hồ sơ -> 409 ERR_PROFILE_NOT_COMPLETED" $true "Đúng nghiệp vụ"

    # Khởi tạo hồ sơ sức khỏe cho User qua PUT /api/v1/users/me/health-profile
    $profileBody = "{`"dietarySchoolId`": 1, `"gender`": `"MALE`", `"dateOfBirth`": `"1998-05-20`", `"heightCm`": 170.0, `"weightKg`": 65.0, `"targetWeightKg`": 60.0, `"healthGoal`": `"LOSE_WEIGHT`", `"requiresAlliumFree`": false, `"regionPreference`": `"SOUTH`"}"
    $setupResp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/health-profile" -Method "PUT" `
        -Headers @{ "Authorization" = "Bearer $memberToken" } `
        -Body $profileBody

    # Ghi nhận cân nặng sau khi đã hoàn thành hồ sơ
    $resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "POST" `
        -Headers @{ "Authorization" = "Bearer $memberToken" } `
        -Body $weightBody
}

$isPass = ($resp.StatusCode -eq 201) -and ($resp.Content -match "bmi") -and ($resp.Content -match "targetDailyCalories")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC5b: Ghi cân nặng mới hợp lệ -> 201 Created kèm BMI/TargetCalories" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"

# =========================================================================
# TEST CASE 6: Ghi cân nặng ngoài khoảng 20-300 kg -> 400 ERR_VALIDATION
# =========================================================================
$invalidWeightBody = @{
    weightKg = 15.0
    loggedDate = $todayStr
} | ConvertTo-Json

$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "POST" `
    -Headers @{ "Authorization" = "Bearer $memberToken" } `
    -Body $invalidWeightBody

$isPass = ($resp.StatusCode -eq 400) -and ($resp.Content -match "ERR_VALIDATION")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC6: Ghi cân nặng < 20 kg -> 400 ERR_VALIDATION" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"

# =========================================================================
# TEST CASE 7: Ghi cân nặng với ngày ở tương lai -> 400 ERR_VALIDATION
# =========================================================================
$futureDateStr = [DateTime]::UtcNow.AddDays(2).ToString("yyyy-MM-dd")
$futureWeightBody = @{
    weightKg = 55.0
    loggedDate = $futureDateStr
} | ConvertTo-Json

$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history" -Method "POST" `
    -Headers @{ "Authorization" = "Bearer $memberToken" } `
    -Body $futureWeightBody

$isPass = ($resp.StatusCode -eq 400) -and ($resp.Content -match "ERR_VALIDATION")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC7: Ghi cân nặng ngày tương lai -> 400 ERR_VALIDATION" $isPass "Status=$($resp.StatusCode); Body=$($resp.Content)"

# =========================================================================
# TEST CASE 8: Lấy danh sách lịch sử cân nặng -> 200 OK phân trang Envelope D52
# =========================================================================
$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history?page=1&size=20" -Method "GET" `
    -Headers @{ "Authorization" = "Bearer $memberToken" }

$isPass = ($resp.StatusCode -eq 200) -and ($resp.Content -match '"meta"') -and ($resp.Content -match '"data"')
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC8: Tra cứu lịch sử cân nặng -> 200 OK kèm meta phân trang" $isPass "Status=$($resp.StatusCode)"

# =========================================================================
# TEST CASE 9: Tra cứu với khoảng ngày sai (from > to) -> 400 ERR_VALIDATION
# =========================================================================
$resp = Invoke-ApiRequest -Uri "$BaseUrl/api/v1/users/me/weight-history?from=2026-10-20&to=2026-10-01" -Method "GET" `
    -Headers @{ "Authorization" = "Bearer $memberToken" }

$isPass = ($resp.StatusCode -eq 400) -and ($resp.Content -match "ERR_VALIDATION")
if ($isPass) { $passedCount++ } else { $failedCount++ }
Write-TestResult "TC9: Lọc lịch sử cân nặng from > to -> 400 ERR_VALIDATION" $isPass "Status=$($resp.StatusCode)"

# =========================================================================
# TONG KET
# =========================================================================
Write-TestHeader "KET QUA KIEM THU: $passedCount PASSED / $($passedCount + $failedCount) CASES"
if ($failedCount -eq 0) {
    Write-Host ">>> TAT CA CAC TRUONG HOP KIEM THU DA DAT CHUAN NGHIEM THU 100%! <<<" -ForegroundColor Green
} else {
    Write-Host ">>> CO $failedCount TRUONG HOP THAT BAI. CAN KIEM TRA LAI. <<<" -ForegroundColor Red
}
