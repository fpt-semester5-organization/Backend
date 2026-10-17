using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// Payload dữ liệu trả về cho kết quả nhận diện nguyên liệu tủ lạnh (FN-VIS-01).
/// </summary>
public class CreateFridgeDetectionResponseData
{
    /// <summary>
    /// Đã lưu vào tủ lạnh ảo hay chưa (true với Thành viên, false với Khách).
    /// </summary>
    public bool Saved { get; set; }

    /// <summary>
    /// Danh sách các nguyên liệu nhận diện được từ ảnh.
    /// </summary>
    public List<DetectedFridgeIngredientDto> Detected { get; set; } = new();

    /// <summary>
    /// Các cảnh báo về trường phái ăn chay hoặc phát hiện thịt/động vật.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Hạn mức quét ảnh còn lại.
    /// </summary>
    public FridgeDetectionQuotaDto Quota { get; set; } = new();
}
