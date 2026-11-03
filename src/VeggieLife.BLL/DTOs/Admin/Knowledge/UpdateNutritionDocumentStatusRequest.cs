namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// Yêu cầu cập nhật trạng thái kích hoạt/ẩn tài liệu dinh dưỡng (FN-CHAT-07).
/// </summary>
public class UpdateNutritionDocumentStatusRequest
{
    public bool IsActive { get; set; }
}
