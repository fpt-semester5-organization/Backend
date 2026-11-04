namespace VeggieLife.BLL.DTOs.Admin.Ingredients;

/// <summary>
/// DTO yêu cầu khóa hoặc mở khóa nguyên liệu (BR-ADM-ING-01).
/// </summary>
public class ToggleIngredientLockRequest
{
    public bool IsLocked { get; set; }
}
