using System;

namespace VeggieLife.BLL.DTOs.Guest.Responses;

/// <summary>
/// Ngữ cảnh hành động dang dở và đường dẫn quay lại của Khách vãng lai lưu trên Redis 15 phút (FN-AUTH-03, BR-GUEST-AUTH-01).
/// </summary>
public class GuestReturnContextDto
{
    public string? ReturnUrl { get; set; }
    public string? PendingAction { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
