using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VeggieLife.API.DTOs.Fridge;

/// <summary>
/// Form dữ liệu multipart/form-data nhận tệp ảnh chụp tủ lạnh (FN-VIS-01).
/// </summary>
public class CreateFridgeDetectionForm
{
    /// <summary>
    /// Tệp ảnh JPEG, PNG hoặc WEBP (độ phân giải >= 640x480, dung lượng <= 10 MB).
    /// </summary>
    [Required(ErrorMessage = "Vui lòng tải lên tệp hình ảnh tủ lạnh.")]
    public IFormFile Image { get; set; } = null!;
}
