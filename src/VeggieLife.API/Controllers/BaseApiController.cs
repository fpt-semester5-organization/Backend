using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Responses;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Lớp Controller cơ sở dùng chung cho toàn bộ các API Controller của hệ thống VeggieLife.
/// Cung cấp các phương thức trả lời chuẩn và tiện ích giải mã Claims của người dùng đang đăng nhập.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Trả về phản hồi thành công chuẩn kèm payload dữ liệu: { statusCode: 200, message, data }.
    /// </summary>
    protected IActionResult OkResponse<T>(T data, string message = "Thao tác thành công.")
    {
        return Ok(ApiResponse<T>.Success(data, message));
    }

    /// <summary>
    /// Trả về phản hồi thành công chuẩn không kèm payload: { statusCode: 200, message }.
    /// </summary>
    protected IActionResult OkResponse(string message = "Thao tác thành công.")
    {
        return Ok(ApiResponse.Success(message));
    }

    /// <summary>
    /// Trả về phản hồi tạo mới thành công: { statusCode: 201, message, data }.
    /// </summary>
    protected IActionResult CreatedResponse<T>(T data, string message = "Khởi tạo thành công.")
    {
        return StatusCode(StatusCodes.Status201Created, new ApiResponse<T>(StatusCodes.Status201Created, message, data));
    }

    /// <summary>
    /// Trả về kết quả phân trang chuẩn PagedResult.
    /// </summary>
    protected IActionResult PagedResponse<T>(PagedResult<T> pagedResult, string message = "Lấy danh sách thành công.")
    {
        return Ok(ApiResponse<PagedResult<T>>.Success(pagedResult, message));
    }

    /// <summary>
    /// Lấy UserId của người dùng hiện tại từ Claims (nếu là tài khoản đã đăng nhập).
    /// </summary>
    protected long? CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(idClaim, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Lấy Email của người dùng hiện tại từ Claims.
    /// </summary>
    protected string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);

    /// <summary>
    /// Lấy Role của người dùng hiện tại từ Claims (GUEST, MEMBER, VIP, ADMIN).
    /// </summary>
    protected string? CurrentUserRole => User.FindFirstValue(ClaimTypes.Role);

    /// <summary>
    /// Lấy mã định danh vô danh (Guest UUID) từ Claims nếu người dùng là khách vãng lai.
    /// </summary>
    protected string? CurrentGuestUuid => User.FindFirstValue("Guest_UUID");
}
