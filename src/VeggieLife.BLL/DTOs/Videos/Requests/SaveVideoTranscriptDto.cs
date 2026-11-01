using System.Collections.Generic;
using VeggieLife.BLL.DTOs.Videos.Responses;

namespace VeggieLife.BLL.DTOs.Videos.Requests;

/// <summary>
/// DTO lưu bản phiên âm và công thức bóc tách từ video xuống CSDL (FN-POST-09, FN-POST-10).
/// </summary>
public class SaveVideoTranscriptDto
{
    /// <summary>
    /// Bản phiên âm thô trích xuất từ âm thanh video.
    /// </summary>
    public string RawTranscript { get; set; } = string.Empty;

    /// <summary>
    /// Công thức trích xuất có cấu trúc (tiêu đề, khẩu phần, nguyên liệu, các bước, mẹo).
    /// </summary>
    public ExtractedRecipeDto? Recipe { get; set; }

    /// <summary>
    /// Danh sách phân đoạn lời thoại theo mốc thời gian.
    /// </summary>
    public List<TranscriptSegmentDto>? Segments { get; set; }

    /// <summary>
    /// Phiên bản mô hình AI STT.
    /// </summary>
    public string? SttModelVersion { get; set; }
}
