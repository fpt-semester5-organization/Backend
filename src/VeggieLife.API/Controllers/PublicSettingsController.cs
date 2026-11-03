using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Settings;
using VeggieLife.BLL.Services.Settings;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller cung cấp các tham số cấu hình công khai của hệ thống cho Frontend (hạn mức thực đơn, chat, quét tủ lạnh, link pháp lý).
/// Cho phép cả Khách vãng lai, Thành viên và VIP truy cập mà không yêu cầu xác thực.
/// </summary>
[ApiController]
[Route("api/v1/system/settings")]
[AllowAnonymous]
public class PublicSettingsController : BaseApiController
{
    private readonly ISystemSettingService _settingService;

    public PublicSettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Lấy toàn bộ các chỉ số cấu hình công khai của hệ thống để Frontend hiển thị và đồng bộ theo thời gian thực.
    /// </summary>
    [HttpGet("public")]
    [ProducesResponseType(typeof(ApiResponse<PublicSystemSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicSettings(CancellationToken ct)
    {
        var result = await _settingService.GetPublicSettingsAsync(ct);
        return OkResponse(result, "Lấy cấu hình hệ thống thành công.");
    }
}
