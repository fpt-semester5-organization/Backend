using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.DAL.Entities;

namespace VeggieLife.API.Controllers;

/// <summary>
/// Controller kiểm thử các thành phần khung nền tảng: Chuẩn phản hồi, Mã lỗi, Phân quyền, JWT, Redis, Health & Interface Stubs.
/// </summary>
public class TestController : BaseApiController
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRedisService _redisService;
    private readonly IAiService _aiService;
    private readonly IVectorDbService _vectorDbService;
    private readonly IEmailService _emailService;
    private readonly IMediaService _mediaService;
    private readonly IPaymentGatewayService _paymentGatewayService;

    public TestController(
        IJwtTokenService jwtTokenService,
        IRedisService redisService,
        IAiService aiService,
        IVectorDbService vectorDbService,
        IEmailService emailService,
        IMediaService mediaService,
        IPaymentGatewayService paymentGatewayService)
    {
        _jwtTokenService = jwtTokenService;
        _redisService = redisService;
        _aiService = aiService;
        _vectorDbService = vectorDbService;
        _emailService = emailService;
        _mediaService = mediaService;
        _paymentGatewayService = paymentGatewayService;
    }

    /// <summary>
    /// Thử nghiệm phản hồi thành công chuẩn: { statusCode: 200, message, data }.
    /// </summary>
    [HttpGet("success")]
    public IActionResult TestSuccess()
    {
        var sampleData = new
        {
            framework = "VeggieLife Backend (.NET 8)",
            status = "Ready",
            documentation = "/swagger",
            health = "/health"
        };

        return OkResponse(sampleData, "Phản hồi chuẩn thành công.");
    }

    /// <summary>
    /// Thử nghiệm ném lỗi nghiệp vụ cố ý để kiểm tra chuẩn phản hồi { statusCode, errorCode, message }.
    /// </summary>
    [HttpGet("error")]
    public IActionResult TestDeliberateBusinessError()
    {
        throw AppException.BadRequest(
            ErrorCodes.ErrValidation,
            "Lỗi kiểm thử cố ý: dữ liệu đầu vào không hợp lệ hoặc thiếu trường bắt buộc.",
            new { field = "Email", reason = "Email không được để trống" });
    }

    /// <summary>
    /// Thử nghiệm ngoại lệ chưa kiểm soát để kiểm chứng bọc lỗi 500 an toàn toàn cục.
    /// </summary>
    [HttpGet("error-unhandled")]
    public IActionResult TestUnhandledException()
    {
        throw new InvalidOperationException("Ngoại lệ cố ý mô phỏng sự cố hệ thống chưa kiểm soát.");
    }

    /// <summary>
    /// Sinh Token mẫu cho Khách vãng lai (GUEST).
    /// </summary>
    [HttpGet("token/guest")]
    public IActionResult GenerateGuestToken()
    {
        var guestUuid = Guid.NewGuid().ToString();
        var token = _jwtTokenService.GenerateGuestToken(guestUuid);
        return OkResponse(token, "Đã sinh JWT Token cho vai trò GUEST.");
    }

    /// <summary>
    /// Sinh Token mẫu cho Thành viên (MEMBER).
    /// </summary>
    [HttpGet("token/member")]
    public IActionResult GenerateMemberToken([FromQuery] int? userId = null)
    {
        var mockUser = new User
        {
            UserId = userId ?? 2,
            Email = "member@veggielife.vn",
            FullName = "Nguyễn Văn Member",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE"
        };

        var token = _jwtTokenService.GenerateUserToken(mockUser);
        return OkResponse(token, "Đã sinh JWT Token cho vai trò MEMBER.");
    }

    /// <summary>
    /// Sinh Token mẫu cho Hội viên VIP (VIP).
    /// </summary>
    [HttpGet("token/vip")]
    public IActionResult GenerateVipToken()
    {
        var mockUser = new User
        {
            UserId = 202,
            Email = "vip@veggielife.vn",
            FullName = "Trần Thị VIP",
            Role = UserRoles.Vip,
            AccountStatus = "ACTIVE"
        };

        var token = _jwtTokenService.GenerateUserToken(mockUser);
        return OkResponse(token, "Đã sinh JWT Token cho vai trò VIP.");
    }

    /// <summary>
    /// Sinh Token mẫu cho Quản trị viên (ADMIN).
    /// </summary>
    [HttpGet("token/admin")]
    public IActionResult GenerateAdminToken()
    {
        var mockUser = new User
        {
            UserId = 999,
            Email = "admin@veggielife.vn",
            FullName = "Hệ thống Admin",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE"
        };

        var token = _jwtTokenService.GenerateUserToken(mockUser);
        return OkResponse(token, "Đã sinh JWT Token cho vai trò ADMIN.");
    }

    /// <summary>
    /// Endpoint yêu cầu quyền từ GUEST trở lên (GUEST, MEMBER, VIP, ADMIN).
    /// </summary>
    [HttpGet("auth/guest-or-above")]
    [Authorize(Policy = PolicyNames.RequireGuestOrAbove)]
    public IActionResult TestAuthGuest()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            role = CurrentUserRole,
            guestUuid = CurrentGuestUuid
        }, "Xác thực thành công với vai trò GUEST trở lên.");
    }

    /// <summary>
    /// Endpoint yêu cầu quyền MEMBER trở lên (MEMBER, VIP, ADMIN).
    /// </summary>
    [HttpGet("auth/member-only")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public IActionResult TestAuthMember()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            email = CurrentUserEmail,
            role = CurrentUserRole
        }, "Xác thực thành công với vai trò MEMBER trở lên.");
    }

    /// <summary>
    /// Thao tác ghi cộng đồng (Thích, Bình luận, Đăng bài): Chặn Khách vãng lai và trả thông báo mời đăng ký.
    /// </summary>
    [HttpPost("auth/community-action")]
    [VeggieLife.API.Authorization.RequireMember(MemberFeatureType.Community)]
    public IActionResult TestCommunityAction()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            action = "Like/Comment/Post",
            status = "Executed"
        }, "Thực hiện thao tác cộng đồng thành công.");
    }

    /// <summary>
    /// Thao tác cá nhân hóa (BMI/TDEE, Hồ sơ, Thực đơn cá nhân): Chặn Khách vãng lai và trả thông báo mời đăng ký.
    /// </summary>
    [HttpGet("auth/personalization-action")]
    [VeggieLife.API.Authorization.RequireMember(MemberFeatureType.Personalization)]
    public IActionResult TestPersonalizationAction()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            feature = "Personalization/Profile/MealPlan",
            status = "Accessed"
        }, "Truy cập tính năng cá nhân hóa thành công.");
    }

    /// <summary>
    /// Endpoint yêu cầu quyền VIP trở lên (VIP, ADMIN).
    /// </summary>
    [HttpGet("auth/vip-only")]
    [Authorize(Policy = PolicyNames.RequireVip)]
    public IActionResult TestAuthVip()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            email = CurrentUserEmail,
            role = CurrentUserRole
        }, "Xác thực thành công với đặc quyền VIP trở lên.");
    }

    /// <summary>
    /// Endpoint yêu cầu quyền tối cao ADMIN.
    /// </summary>
    [HttpGet("auth/admin-only")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public IActionResult TestAuthAdmin()
    {
        return OkResponse(new
        {
            userId = CurrentUserId,
            email = CurrentUserEmail,
            role = CurrentUserRole
        }, "Xác thực thành công với quyền hạn ADMIN.");
    }

    /// <summary>
    /// Thử nghiệm các thao tác với dịch vụ bộ đệm phân tán Redis.
    /// </summary>
    [HttpGet("redis")]
    public async Task<IActionResult> TestRedis()
    {
        var testKey = "test_counter";
        var counter = await _redisService.IncrementAsync(testKey, 1, TimeSpan.FromMinutes(5));
        var exists = await _redisService.ExistsAsync(testKey);

        return OkResponse(new
        {
            cacheKey = testKey,
            currentCounter = counter,
            exists = exists
        }, "Kiểm tra thao tác Redis thành công.");
    }

    /// <summary>
    /// Thử nghiệm gọi các Interface dịch vụ dùng chung (Stubs).
    /// </summary>
    [HttpGet("services-stub")]
    public async Task<IActionResult> TestServicesStub()
    {
        var aiText = await _aiService.GenerateTextAsync("Giới thiệu chế độ ăn chay");
        var vectorSearch = await _vectorDbService.SearchSimilarAsync(new float[768], topK: 1);
        await _emailService.SendOtpEmailAsync("test@veggielife.vn", "123456", "VERIFY_ACCOUNT");
        var mediaUrl = await _mediaService.UploadImageAsync(System.IO.Stream.Null, "avatar.png");
        var momo = await _paymentGatewayService.CreateMoMoPaymentAsync(1001, 50000, "Nâng cấp VIP", "https://veggielife.vn/callback");

        return OkResponse(new
        {
            aiService = aiText,
            vectorResults = vectorSearch,
            mediaUploadedUrl = mediaUrl,
            payment = momo
        }, "Tất cả các bản rỗng của Interface dịch vụ dùng chung đã được tiêm phụ thuộc (DI) và phản hồi tốt.");
    }
}

