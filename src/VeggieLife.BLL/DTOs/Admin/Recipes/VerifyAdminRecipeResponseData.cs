namespace VeggieLife.BLL.DTOs.Admin.Recipes;

/// <summary>
/// Dữ liệu phản hồi sau khi cấp huy hiệu công thức chuẩn chay (FN-ADM-09).
/// </summary>
public class VerifyAdminRecipeResponseData
{
    public int RecipeId { get; set; }
    public bool IsVerified { get; set; }
}
