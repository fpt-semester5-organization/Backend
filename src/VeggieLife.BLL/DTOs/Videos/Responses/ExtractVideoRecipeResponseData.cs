namespace VeggieLife.BLL.DTOs.Videos.Responses;

/// <summary>
/// DTO phản hồi yêu cầu bóc tách công thức từ video (FN-POST-10).
/// </summary>
public class ExtractVideoRecipeResponseData
{
    /// <summary>
    /// Mã định danh video.
    /// </summary>
    public int VideoId { get; set; }

    /// <summary>
    /// Trạng thái bóc tách phiên âm: PENDING, PROCESSING, READY, FAILED.
    /// </summary>
    public string TranscriptStatus { get; set; } = "PENDING";
}
