using System.ComponentModel.DataAnnotations;
using System.IO;

namespace VeggieLife.BLL.DTOs.Admin.Knowledge;

/// <summary>
/// Yêu cầu tải lên tài liệu dinh dưỡng PDF nạp vào Vector DB ở tầng BLL (FN-CHAT-07).
/// </summary>
public class UploadNutritionDocumentRequest
{
    [Required(ErrorMessage = "Tiêu đề tài liệu là bắt buộc.")]
    [StringLength(300, ErrorMessage = "Tiêu đề không được vượt quá 300 ký tự.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Admin thực hiện tải lên (ghi AIQuotaLogs khi phải OCR bằng Gemini).</summary>
    public int? ActorUserId { get; set; }

    [StringLength(200, ErrorMessage = "Tên tác giả không được vượt quá 200 ký tự.")]
    public string? Author { get; set; }

    public string FileName { get; set; } = string.Empty;

    public Stream FileStream { get; set; } = Stream.Null;

    public long FileLength { get; set; }
}
