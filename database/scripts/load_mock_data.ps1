# Script nạp dữ liệu mẫu - Chạy nhiều lần không bị trùng
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$sqlFile = Join-Path $scriptPath "..\seed\99_MockData.sql"

# Lấy connection string hoặc dùng mặc định
$server = "(localdb)\MSSQLLocalDB"
$database = "VeggieLifeDB"

Write-Host "Bắt đầu nạp dữ liệu mẫu vào cơ sở dữ liệu $database trên máy chủ $server..."
sqlcmd -S $server -d $database -i $sqlFile -b

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Nạp dữ liệu mẫu thành công! Bạn có thể chạy lại script này bao nhiêu lần tùy ý mà không sợ trùng lặp." -ForegroundColor Green
} else {
    Write-Host "❌ Nạp dữ liệu mẫu thất bại." -ForegroundColor Red
}
