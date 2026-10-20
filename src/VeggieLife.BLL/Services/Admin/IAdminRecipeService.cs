using System.Threading;
using System.Threading.Tasks;
using VeggieLife.BLL.DTOs.Admin.Recipes;

namespace VeggieLife.BLL.Services.Admin;

/// <summary>
/// Giao diện dịch vụ Quản trị xác minh công thức chuẩn chay (FN-ADM-09).
/// </summary>
public interface IAdminRecipeService
{
    /// <summary>
    /// FN-ADM-09: Xác minh và gắn huy hiệu 'Công Thức Chay Chuẩn' khi thỏa mãn đồng thời 4 tiêu chí của BR-ADM-MAS-003.
    /// </summary>
    Task<VerifyAdminRecipeResponseData> VerifyRecipeAsync(
        int id,
        VerifyAdminRecipeRequest? request,
        int adminId,
        CancellationToken ct = default);

    /// <summary>
    /// FN-ADM-09: Gỡ huy hiệu 'Công Thức Chay Chuẩn' khỏi công thức.
    /// </summary>
    Task<UnverifyAdminRecipeResponseData> UnverifyRecipeAsync(
        int id,
        int adminId,
        CancellationToken ct = default);
}
