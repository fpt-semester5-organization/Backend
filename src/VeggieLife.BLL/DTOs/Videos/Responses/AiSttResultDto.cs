using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Videos.Responses;

/// <summary>
/// DTO tiếp nhận kết quả bóc tách công thức và phiên âm giọng nói từ AI STT Microservice (FN-POST-10).
/// </summary>
public class AiSttResultDto
{
    /// <summary>
    /// Thời lượng video tính theo giây.
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Phiên bản mô hình STT (Faster-Whisper small-int8).
    /// </summary>
    [JsonPropertyName("sttModelVersion")]
    public string? SttModelVersion { get; set; }

    /// <summary>
    /// Bản phiên âm thô trích xuất từ âm thanh video.
    /// </summary>
    [JsonPropertyName("rawTranscript")]
    public string RawTranscript { get; set; } = string.Empty;

    /// <summary>
    /// Công thức nấu ăn có cấu trúc trích xuất bởi LLM (Gemini).
    /// </summary>
    [JsonPropertyName("recipe")]
    public ExtractedRecipeDto? Recipe { get; set; }
}
