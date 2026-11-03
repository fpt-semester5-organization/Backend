using System;
using System.Collections.Generic;

namespace VeggieLife.BLL.DTOs.Fridge.Responses;

/// <summary>
/// DTO chứa thông tin phiên nhận diện tủ lạnh gần nhất lấy từ CSDL (FN-VIS-01).
/// </summary>
public class LatestFridgeSessionDto
{
    public int SessionId { get; set; }
    public string ImageCdnUrl { get; set; } = string.Empty;
    public int DetectedItemsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DetectedFridgeIngredientDto> Detected { get; set; } = new();
}

