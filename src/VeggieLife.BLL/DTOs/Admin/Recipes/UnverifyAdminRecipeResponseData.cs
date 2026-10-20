namespace VeggieLife.BLL.DTOs.Admin.Recipes;

/// <summary>
/// Dữ liệu phản hồi sau khi gỡ huy hiệu công thức chuẩn chay (FN-ADM-09).
/// </summary>
public class UnverifyAdminRecipeResponseData
{
    public int RecipeId { get; set; }
    public bool IsVerified { get; set; }
}
