using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Services.MasterData;

namespace VeggieLife.API.Controllers;

/// <summary>
/// API Controller tra cứu danh mục dùng chung hệ thống (FN-PROF-08):
/// Trường phái ăn chay, nhóm dị ứng, danh mục món ăn, nguyên liệu thực phẩm.
/// </summary>
[Route("api/v1")]
[AllowAnonymous]
public class MasterDataController : BaseApiController
{
    private readonly IMasterDataService _masterDataService;

    public MasterDataController(IMasterDataService masterDataService)
    {
        _masterDataService = masterDataService;
    }

    /// <summary>
    /// FN-PROF-08: Tra cứu danh mục 07 trường phái ăn chay cốt lõi kèm quy tắc cấm (D109).
    /// </summary>
    [HttpGet("dietary-schools")]
    public async Task<IActionResult> ListDietarySchools(CancellationToken ct)
    {
        var result = await _masterDataService.GetDietarySchoolsAsync(ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-PROF-08: Tra cứu danh mục các nhóm chất gây dị ứng thực phẩm.
    /// </summary>
    [HttpGet("allergens")]
    public async Task<IActionResult> ListAllergens(CancellationToken ct)
    {
        var result = await _masterDataService.GetAllergensAsync(ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-PROF-08: Tra cứu danh mục món ăn thuần chay đang kích hoạt.
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories(CancellationToken ct)
    {
        var result = await _masterDataService.GetCategoriesAsync(ct);
        return OkResponse(result, "OK");
    }

    /// <summary>
    /// FN-PROF-08: Tra cứu danh mục nguyên liệu thực phẩm (tìm kiếm theo từ khóa, phân trang).
    /// </summary>
    [HttpGet("ingredients")]
    public async Task<IActionResult> ListIngredients(
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        CancellationToken ct = default)
    {
        var (items, meta) = await _masterDataService.GetIngredientsAsync(keyword, page, size, ct);
        return PagedResponse(items, meta, "OK");
    }
}
