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
    public class UsersController : ControllerBase
    {
        private readonly IProductService _productService;

        public UsersController(IProductService productService)
        {
            _productService = productService;
        }

        // BR-USR-09: Lịch sử quét mã vạch
        [HttpGet("me/scans")]
        [Authorize(Policy = "Member")] // Auth: Bearer (MEMBER | VIP)
        public async Task<ActionResult<ApiResponse<PagedResult<UserScanHistoryResponse>>>> GetMyScans(
            [FromQuery] string? verdict, 
            [FromQuery] bool? allergenOnly, 
            [FromQuery] int page = 1, 
            [FromQuery] int size = 20)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "ERR_AUTH_INVALID_TOKEN" });
            }

            var result = await _productService.GetUserScanHistoryAsync(userId, verdict, allergenOnly, page, size);
            
            return Ok(new ApiResponse<PagedResult<UserScanHistoryResponse>>
            {
                Success = true,
                StatusCode = 200,
                Message = "OK",
                Data = result
            });
        }
    }
}
