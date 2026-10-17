using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VeggieLife.BLL.DTOs.Videos.Responses;

/// <summary>
/// DTO chứa bản phiên âm Speech-to-Text và công thức bóc tách của video (FN-POST-09).
/// </summary>
public class VideoTranscriptResponseData
{
    /// <summary>
    /// Mã định danh video.
    /// </summary>
    public int VideoId { get; set; }

    /// <summary>
    /// Bản phiên âm thô trích xuất từ âm thanh video.
    /// </summary>
    public string RawTranscript { get; set; } = string.Empty;

    /// <summary>
    /// Công thức nấu ăn đã trích xuất cấu trúc hóa từ bản phiên âm.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExtractedRecipeDto? ExtractedRecipe { get; set; }

    /// <summary>
    /// Hạn mức AI dùng thử (chỉ áp dụng đối với Khách vãng lai, null đối với Thành viên).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VideoTranscriptQuotaDto? Quota { get; set; }
}

/// <summary>
/// DTO cấu trúc công thức được trích xuất từ video.
/// </summary>
public class ExtractedRecipeDto
{
    /// <summary>
    /// Tên món ăn trích xuất từ video.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách nguyên liệu và định lượng.
    /// </summary>
    public List<ExtractedIngredientDto> Ingredients { get; set; } = new();

    /// <summary>
    /// Các bước chế biến nấu ăn.
    /// </summary>
    public List<string> Steps { get; set; } = new();
}

/// <summary>
/// DTO nguyên liệu trích xuất từ video.
/// </summary>
public class ExtractedIngredientDto
{
    /// <summary>
    /// Tên nguyên liệu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Định lượng nguyên liệu.
    /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Đơn vị đo lường (g, ml, muỗng cà phê, ...).
    /// </summary>
    public string? Unit { get; set; }
}

/// <summary>
/// DTO thông tin hạn ngạch xem phiên âm AI cho Khách vãng lai.
/// </summary>
public class VideoTranscriptQuotaDto
{
    /// <summary>
    /// Số lượt đã sử dụng.
    /// </summary>
    public int Used { get; set; }

    /// <summary>
    /// Tổng hạn mức dùng thử (mặc định 3 lượt).
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Số lượt còn lại.
    /// </summary>
    public int Remaining { get; set; }
}
