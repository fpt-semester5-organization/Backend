namespace VeggieLife.BLL.DTOs.Admin.Users;

/// <summary>
/// DTO yêu cầu cập nhật trạng thái & xử lý kỷ luật tài khoản (FN-ADM-02, BR-ADM-USR-002).
/// </summary>
public class UpdateAdminUserStatusRequest
{
    /// <summary>
    /// Hành động kỷ luật: WARN | RESTRICT_COMMENT | SUSPEND | BAN | REACTIVATE
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Số ngày áp dụng thời hạn tạm khóa hoặc khóa bình luận (bắt buộc khi SUSPEND hoặc RESTRICT_COMMENT).
    /// </summary>
    public int? DurationDays { get; set; }

    /// <summary>
    /// Lý do xử lý kỷ luật (10–500 ký tự).
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
