using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Products;

namespace VeggieLife.API.Controllers;

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
    /// BR-GUEST-07: Guest scan quota & Tra cứu mã vạch sản phẩm.
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [AllowAnonymous]
    public async Task<IActionResult> ScanBarcode(string barcode)
    {
        int? userId = CurrentUserId.HasValue ? (int)CurrentUserId.Value : null;
        var result = await _productService.ScanBarcodeAsync(barcode, userId);
        return OkResponse(result, "Tra cứu mã vạch thành công.");
    }
}
