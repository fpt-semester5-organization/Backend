using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Ai;

/// <summary>
/// DTO phản hồi kết quả kiểm duyệt từ AI Service (POST /moderation/check).
/// Tuân thủ tài liệu đặc tả VL-SPEC-AI-MOD-01.
/// </summary>
public class ModerationCheckResponse
{
    /// <summary>
    /// Điểm số vi phạm do Gemini tính toán (0.0 đến 1.0). Ngưỡng Backend xử lý là >= 0.85.
    /// </summary>
    public double ViolationScore { get; set; }

    /// <summary>
    /// Loại vi phạm có trọng số nặng nhất: NONE, VIO_MEAT, VIO_LABEL, VIO_HATE, VIO_SPAM.
    /// </summary>
    public string ViolationType { get; set; } = "NONE";

    /// <summary>
    /// Danh sách giải trình ngắn gọn bằng Tiếng Việt chỉ rõ bằng chứng vi phạm (Tối đa 5 phần tử).
    /// </summary>
    public List<string> Reasons { get; set; } = new();

    /// <summary>
    /// Thông số hiệu năng: Tên model, Prompt Tokens, Completion Tokens, Latency ms.
    /// </summary>
    public ModerationUsage Usage { get; set; } = new();

    /// <summary>
    /// Dữ liệu kiểm toán: Toàn văn prompt thô gửi AI và kết quả thô nhận về.
    /// </summary>
    public ModerationAudit Audit { get; set; } = new();
}
