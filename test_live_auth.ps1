$rand = Get-Random

# 1. Test Weak Password
Write-Output "=== 1. TEST WEAK PASSWORD ==="
$weakBody = @{
    fullName = "Test User"
    email = "test.weak.$rand@veggielife.vn"
    password = "weakpassword"
    confirmPassword = "weakpassword"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/register" -Method Post -Body $weakBody -ContentType "application/json"
} catch {
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = [System.IO.StreamReader]::new($stream)
    Write-Output $reader.ReadToEnd()
}

# 2. Test Valid Registration
Write-Output "`n=== 2. TEST VALID REGISTRATION ==="
$email = "test.lockout.$rand@veggielife.vn"
$validBody = @{
    fullName = "Nguyen Van Lockout"
    email = $email
    password = "VeggieLife@2026"
    confirmPassword = "VeggieLife@2026"
} | ConvertTo-Json

$regRes = Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/register" -Method Post -Body $validBody -ContentType "application/json"
$regRes | ConvertTo-Json -Depth 5

# 3. Test Wrong OTP until 5 times (Lockout test)
Write-Output "`n=== 3. TEST 5 WRONG OTP ATTEMPTS & 15-MIN LOCKOUT ==="
for ($step = 1; $step -le 5; $step++) {
    $wrongBody = @{
        email = $email
        otp = "000000"
    } | ConvertTo-Json

    try {
        Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/verify-otp" -Method Post -Body $wrongBody -ContentType "application/json"
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = [System.IO.StreamReader]::new($stream)
        $text = $reader.ReadToEnd()
        Write-Output "Attempt $step - $text"
    }
}

# 4. Test Resend OTP while locked (Expect 429)
Write-Output "`n=== 4. TEST RESEND OTP WHILE LOCKED ==="
$resendBody = @{
    email = $email
    purpose = "EMAIL_VERIFY"
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/resend-otp" -Method Post -Body $resendBody -ContentType "application/json"
} catch {
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = [System.IO.StreamReader]::new($stream)
    $text = $reader.ReadToEnd()
    Write-Output "Resend while locked - $text"
}

# 5. Test Register and Successful Verification with correct OTP
Write-Output "`n=== 5. TEST SUCCESSFUL ACTIVATION WITH CORRECT OTP ==="
$activeEmail = "test.success.$rand@veggielife.vn"
$successRegBody = @{
    fullName = "Hoang Thi Thao"
    email = $activeEmail
    password = "VeggieLife@2026"
    confirmPassword = "VeggieLife@2026"
} | ConvertTo-Json

$reg2 = Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/register" -Method Post -Body $successRegBody -ContentType "application/json"
Write-Output "Registration successful for $activeEmail"

# Lay OTP qua dev endpoint
$otpInfo = Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/dev-latest-otp?email=$activeEmail" -Method Get
$correctOtp = $otpInfo.data.otp
Write-Output "Dev OTP retrieved: $correctOtp"

# Xac thuc voi dung OTP
$verifySuccessBody = @{
    email = $activeEmail
    otp = $correctOtp
} | ConvertTo-Json

$verifyRes = Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/verify-otp" -Method Post -Body $verifySuccessBody -ContentType "application/json"
$verifyRes | ConvertTo-Json -Depth 5

# 6. Test Privacy: Resend OTP for non-existent email
Write-Output "`n=== 6. TEST PRIVACY RESEND OTP (NON-EXISTENT EMAIL) ==="
$ghostBody = @{
    email = "ghost.notfound.$rand@veggielife.vn"
    purpose = "EMAIL_VERIFY"
} | ConvertTo-Json

$ghostRes = Invoke-RestMethod -Uri "http://localhost:5137/api/v1/auth/resend-otp" -Method Post -Body $ghostBody -ContentType "application/json"
$ghostRes | ConvertTo-Json -Depth 5
