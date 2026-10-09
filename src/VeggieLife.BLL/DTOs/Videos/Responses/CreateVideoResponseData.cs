using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Videos.Responses;

/// <summary>
/// DTO chứa dữ liệu trả về sau khi nhận video hoặc ảnh (FN-POST-08).
/// </summary>
public class CreateVideoResponseData
{
    /// <summary>
    /// Mã định danh video vừa khởi tạo trong CSDL (đối với video).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VideoId { get; set; }

    /// <summary>
    /// Trạng thái xử lý của tệp: "PROCESSING" đối với video, "READY" đối với ảnh.
    /// </summary>
    public string Status { get; set; } = "PROCESSING";

    /// <summary>
    /// Đường dẫn CDN của ảnh (đối với ảnh).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImageUrl { get; set; }
}
