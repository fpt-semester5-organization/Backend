using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Videos.Responses;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Giao tiếp với Microservice Speech-To-Text (Whisper + Gemini) để bóc tách công thức nấu ăn từ video (FN-POST-10).
/// </summary>
public interface IAiSttClient
{
    /// <summary>
    /// Gửi video URL sang AI STT Microservice để trích xuất âm thanh, phiên âm lời thoại và cấu trúc hóa công thức.
    /// </summary>
    /// <param name="videoUrl">URL video Cloudinary công khai.</param>
    /// <param name="ct">CancellationToken.</param>
    /// <returns>Đối tượng kết quả bóc tách gồm rawTranscript và recipe, hoặc null nếu có lỗi/thất bại.</returns>
    Task<AiSttResultDto?> ExtractRecipeFromVideoAsync(string videoUrl, CancellationToken ct = default);
}
