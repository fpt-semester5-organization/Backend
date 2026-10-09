using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Products.Responses;
using VeggieLife.BLL.Services.Products;

namespace VeggieLife.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // BR-GUEST-07: Guest scan quota
        [HttpGet("barcode/{barcode}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BarcodeScanResponse>>> ScanBarcode(string barcode)
        {
            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }
            }

            var result = await _productService.ScanBarcodeAsync(barcode, userId);
            
            return Ok(new ApiResponse<BarcodeScanResponse>
            {
                Success = true,
                StatusCode = 200,
                Message = "Tra cứu mã vạch thành công.",
                Data = result
            });
        }
    }
}
