using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Products;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller xử lý tra cứu mã vạch sản phẩm đóng gói (FN-BAR-01).
/// </summary>
[Route("api/v1/products")]
[ApiController]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// FN-BAR-01: Tra cứu sản phẩm đóng gói theo mã vạch (EAN-8, EAN-13, UPC-A).
    /// Yêu cầu xác thực Bearer token (GUEST, MEMBER, VIP, ADMIN) - không cho phép truy cập công khai vô danh.
    /// Khách vãng lai tối đa 03 lần/ngày (BR-GUEST-07); Thành viên được kèm cảnh báo dị ứng cá nhân.
    /// </summary>
    /// <param name="barcode">Mã vạch chuẩn 8-14 chữ số.</param>
    /// <param name="method">Phương thức quét: CAMERA_BARCODE hoặc MANUAL_INPUT (mặc định CAMERA_BARCODE).</param>
    /// <param name="ct">CancellationToken.</param>
    [HttpGet("barcode/{barcode}")]
    [Authorize(Policy = PolicyNames.RequireGuestOrAbove)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeScanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ScanBarcode(
        [FromRoute] string barcode,
        [FromQuery] string method = "CAMERA_BARCODE",
        CancellationToken ct = default)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Thiếu, sai chữ ký hoặc hết hạn JWT.");
        }

        int? userId = CurrentUserId.HasValue ? (int)CurrentUserId.Value : null;

        // Trích xuất Guest ID từ Claims của Anonymous JWT hoặc Header X-Guest-UUID
        string? guestId = CurrentGuestUuid;
        if (string.IsNullOrWhiteSpace(guestId))
        {
            guestId = Request.Headers["X-Guest-UUID"].ToString();
        }

        var result = await _productService.ScanBarcodeAsync(barcode, userId, guestId, method, ct);
        return OkResponse(result, "Tra cứu mã vạch thành công.");
    }
}
