# ==============================================================================
# VEGGIELIFE - KỊCH BẢN KIỂM THỬ ĐẶC QUYỀN HỘI VIÊN VIP & THỰC ĐƠN
# ==============================================================================

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "  BẮT ĐẦU CHẠY KIỂM THỬ TOÀN DIỆN CÁC TÍNH NĂNG VIP MEMBER (SWAGGER/API) " -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

# ------------------------------------------------------------------------------
# BƯỚC 1: ĐĂNG NHẬP VIP MEMBER
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 1: ĐĂNG NHẬP LẤY TOKEN JWT" -ForegroundColor Yellow
Write-Host "Endpoint: POST $BaseUrl/api/v1/auth/login" -ForegroundColor Gray
$loginPayload = @{
    email    = "vip@veggielife.vn"
    password = "Veggie@2026"
}
$loginJson = $loginPayload | ConvertTo-Json
Write-Host "Request Body:`n$loginJson" -ForegroundColor DarkGray

$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" -Method Post -Body $loginJson -ContentType "application/json"
$Token = $loginResponse.data.accessToken
$UserRole = $loginResponse.data.user.role
$FullName = $loginResponse.data.user.fullName

Write-Host "=> ĐĂNG NHẬP THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Người dùng: $FullName | Role: $UserRole" -ForegroundColor Green
Write-Host "   Token: $($Token.Substring(0, 30))..." -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type"  = "application/json"
}

# ------------------------------------------------------------------------------
# BƯỚC 2: TẠO THỰC ĐƠN MỚI 7 NGÀY
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 2: SINH THỰC ĐƠN 7 NGÀY BẰNG AI (VIP HẠN MỨC: 3 THỰC ĐƠN/TUẦN)" -ForegroundColor Yellow
Write-Host "Endpoint: POST $BaseUrl/api/v1/meal-plans" -ForegroundColor Gray
$today = (Get-Date).ToString("yyyy-MM-dd")
$createPlanPayload = @{
    startDate          = $today
    budgetTier         = "STANDARD"
    durationDays       = 7
    customInstructions = "Ưu tiên các món nấm thanh đạm và canh rau mát"
}
$createPlanJson = $createPlanPayload | ConvertTo-Json
Write-Host "Request Body:`n$createPlanJson" -ForegroundColor DarkGray

$planResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans" -Method Post -Headers $headers -Body $createPlanJson
$PlanId = $planResponse.data.planId
$PlanStatus = $planResponse.data.planStatus
$TotalSlots = $planResponse.data.slots.Count
$TargetKcal = $planResponse.data.targetDailyKcal

Write-Host "=> TẠO THỰC ĐƠN THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Plan ID: $PlanId | Trạng thái: $PlanStatus | Số bữa ăn: $TotalSlots bữa | Calo mục tiêu: $TargetKcal kcal/ngày" -ForegroundColor Green

# Lấy Slot đầu tiên làm mẫu để test đổi món
$SelectedSlot = $planResponse.data.slots[0]
$SlotId = $SelectedSlot.slotId
$OldRecipeTitle = $SelectedSlot.recipe.title
$OldKcal = $SelectedSlot.recipe.caloriesKcal
Write-Host "   Chọn bữa ăn mẫu: Slot ID $SlotId ($($SelectedSlot.mealType) - Ngày $($SelectedSlot.dayOfWeek): $OldRecipeTitle - $OldKcal kcal)" -ForegroundColor Cyan

# ------------------------------------------------------------------------------
# BƯỚC 3: ĐỔI MÓN THEO GHI CHÚ BẰNG AI (SWAP-BY-NOTE)
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 3: ĐỔI MÓN BẰNG AI THEO KHẨU VỊ GHI CHÚ (ĐẶC QUYỀN VIP/MEMBER)" -ForegroundColor Yellow
Write-Host "Endpoint: POST $BaseUrl/api/v1/meal-plans/$PlanId/slots/$SlotId/swap-by-note" -ForegroundColor Gray
$swapByNotePayload = @{
    note              = "Tôi muốn đổi sang món canh nấm thanh mát hoặc đậu hũ non, ít dầu mỡ"
    excludedRecipeIds = @()
}
$swapByNoteJson = $swapByNotePayload | ConvertTo-Json
Write-Host "Request Body:`n$swapByNoteJson" -ForegroundColor DarkGray

$swapResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans/$PlanId/slots/$SlotId/swap-by-note" -Method Post -Headers $headers -Body $swapByNoteJson
$NewRecipeTitle = $swapResponse.data.slot.recipe.title
$NewKcal = $swapResponse.data.slot.recipe.caloriesKcal
$SwapsRemaining = $swapResponse.data.swapsRemaining
$MaxSwaps = $swapResponse.data.maxSwapsAllowed

Write-Host "=> AI ĐỔI MÓN THÀNH CÔNG THEO GHI CHÚ!" -ForegroundColor Green
Write-Host "   Món cũ: $OldRecipeTitle ($OldKcal kcal)" -ForegroundColor DarkYellow
Write-Host "   Món mới: $NewRecipeTitle ($NewKcal kcal)" -ForegroundColor Green
Write-Host "   Số lượt đổi món còn lại: $SwapsRemaining / $MaxSwaps" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 4: XEM LỊCH SỬ ĐỔI MÓN (SWAP HISTORY)
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 4: TRA CỨU LỊCH SỬ ĐỔI MÓN CỦA BỮA ĂN (SLOT SWAP HISTORY)" -ForegroundColor Yellow
Write-Host "Endpoint: GET $BaseUrl/api/v1/meal-plans/slots/$SlotId/swap-history" -ForegroundColor Gray

$historyResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans/slots/$SlotId/swap-history" -Method Get -Headers $headers
$lastHistory = $historyResponse.data[0]

Write-Host "=> LỊCH SỬ ĐỔI MÓN ĐÃ ĐƯỢC GHI NHẬN ĐẦY ĐỦ!" -ForegroundColor Green
Write-Host "   Lý do đổi: $($lastHistory.swapReason)" -ForegroundColor Green
Write-Host "   Chênh lệch calo: $($lastHistory.caloriesDelta) kcal" -ForegroundColor Green
Write-Host "   Thời gian đổi: $($lastHistory.swappedAt)" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 5: XEM PHÂN TÍCH DINH DƯỠNG CHUYÊN SÂU (ĐẶC QUYỀN VIP: LEVEL=DETAILED)
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 5: XEM PHÂN TÍCH CALO & DINH DƯỠNG CHI TIẾT (VIP MACRONUTRIENTS)" -ForegroundColor Yellow
Write-Host "Endpoint: GET $BaseUrl/api/v1/meal-plans/$PlanId/nutrition?level=DETAILED" -ForegroundColor Gray

$nutritionResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans/$PlanId/nutrition?level=DETAILED" -Method Get -Headers $headers
$TotalPlanCalories = $nutritionResponse.data.totalPlanCalories
$VariancePercent = $nutritionResponse.data.variancePercent

Write-Host "=> PHÂN TÍCH DINH DƯỠNG VIP:" -ForegroundColor Green
Write-Host "   Tổng năng lượng tuần: $TotalPlanCalories kcal" -ForegroundColor Green
Write-Host "   Độ lệch so với mục tiêu thể trạng: $VariancePercent %" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 6: TẠO DANH SÁCH ĐI CHỢ 4 QUẦY
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 6: TỔNG HỢP DANH SÁCH ĐI CHỢ TỪ 21 BỮA ĂN (4 QUẦY CHỢ)" -ForegroundColor Yellow
Write-Host "Endpoint: POST $BaseUrl/api/v1/meal-plans/$PlanId/shopping-list" -ForegroundColor Gray
$shoppingPayload = @{
    servings          = 1
    customPantryNotes = "Đã có sẵn gia vị và dầu mè ở nhà"
}
$shoppingJson = $shoppingPayload | ConvertTo-Json
Write-Host "Request Body:`n$shoppingJson" -ForegroundColor DarkGray

$shoppingResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans/$PlanId/shopping-list" -Method Post -Headers $headers -Body $shoppingJson
$ShoppingListId = $shoppingResponse.data.listId
$TotalIngredients = $shoppingResponse.data.totalIngredients

Write-Host "=> ĐÃ TẠO DANH SÁCH ĐI CHỢ!" -ForegroundColor Green
Write-Host "   Shopping List ID: $ShoppingListId | Tổng số nguyên liệu gộp: $TotalIngredients món" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 7: XUẤT FILE PDF THỰC ĐƠN & ĐI CHỢ (ĐẶC QUYỀN VIP - BR-PAY-SUB-03)
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 7: XUẤT THỰC ĐƠN TUẦN RA FILE PDF CHUẨN A4 DÀNH CHO VIP" -ForegroundColor Yellow
Write-Host "Endpoint: GET $BaseUrl/api/v1/meal-plans/$PlanId/export?include=PLAN,SHOPPING" -ForegroundColor Gray

$pdfFilePath = "$PSScriptRoot\veggielife-plan-$PlanId.pdf"
Invoke-WebRequest -Uri "$BaseUrl/api/v1/meal-plans/$PlanId/export?include=PLAN,SHOPPING" -Headers $headers -OutFile $pdfFilePath

Write-Host "=> XUẤT FILE PDF THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Đã lưu tại: $pdfFilePath" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 8: HỦY THỰC ĐƠN THEO PHƯƠNG ÁN A (TASK-K1 - HỒI QUOTA TUẦN)
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 8: HỦY THỰC ĐƠN (PHƯƠNG ÁN A: PLANSTATUS = CANCELLED & HỒI QUOTA TUẦN)" -ForegroundColor Yellow
Write-Host "Endpoint: PUT $BaseUrl/api/v1/meal-plans/$PlanId/cancel" -ForegroundColor Gray

$cancelResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans/$PlanId/cancel" -Method Put -Headers $headers
$NewStatus = $cancelResponse.data.planStatus

Write-Host "=> HỦY THỰC ĐƠN THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Plan ID: $PlanId | Trạng thái mới: $NewStatus" -ForegroundColor Green
Write-Host "   Hạn mức tuần đã được giải phóng ngay lập tức!" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 9: XEM LẠI DANH SÁCH THỰC ĐƠN ĐÃ HỦY
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 9: XEM LẠI TẤT CẢ THỰC ĐƠN ĐÃ HỦY (LỌC THEO STATUS=CANCELLED)" -ForegroundColor Yellow
Write-Host "Endpoint: GET $BaseUrl/api/v1/meal-plans?page=1&size=10&status=CANCELLED" -ForegroundColor Gray

$listCancelledResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans?page=1&size=10&status=CANCELLED" -Method Get -Headers $headers
$cancelledCount = $listCancelledResponse.data.Count
$totalCancelled = $listCancelledResponse.meta.total

Write-Host "=> TRA CỨU THỰC ĐƠN ĐÃ HỦY THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Số lượng thực đơn đã hủy tìm thấy: $cancelledCount (Tổng cộng trong hệ thống: $totalCancelled)" -ForegroundColor Green

# ------------------------------------------------------------------------------
# BƯỚC 10: TẠO THỰC ĐƠN MỚI ĐỂ CHỨNG MINH HỒI QUOTA TUẦN KHÔNG BỊ LỖI 403
# ------------------------------------------------------------------------------
Write-Host "`n>>> BƯỚC 10: TẠO LẠI THỰC ĐƠN MỚI CÙNG TUẦN ĐỂ KIỂM CHỨNG HỒI HẠN MỨC" -ForegroundColor Yellow
Write-Host "Endpoint: POST $BaseUrl/api/v1/meal-plans" -ForegroundColor Gray

$newPlanResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/meal-plans" -Method Post -Headers $headers -Body $createPlanJson
$NewPlanId = $newPlanResponse.data.planId

Write-Host "=> TẠO THỰC ĐƠN MỚI THÀNH CÔNG!" -ForegroundColor Green
Write-Host "   Plan ID mới: $NewPlanId (Không bị lỗi 403 ErrQuotaMealPlanExceeded - Quota đã phục hồi thành công!)" -ForegroundColor Green

Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host "  HOÀN TẤT KIỂM THỬ TOÀN BỘ 10 BƯỚC - HỆ THỐNG HOẠT ĐỘNG HOÀN HẢO!     " -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

