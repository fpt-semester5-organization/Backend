using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Settings;
using VeggieLife.BLL.DTOs.Guest.Responses;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.BLL.Services.Guest;

/// <summary>
/// Triển khai nghiệp vụ kiểm soát phiên Khách vãng lai, đếm hạn mức nguyên tử và thực thi chính sách bảo mật vùng địa lý (FN-AUTH-01, FN-AUTH-02).
/// </summary>
public class GuestService : IGuestService
{
    private readonly IRedisService _redisService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly GuestSecuritySettings _securitySettings;
    private readonly ILogger<GuestService> _logger;

    public const int DefaultAiTrialLimit = 3;
    public const int DefaultBarcodeDailyLimit = 3;
    public const int DefaultMapSearchLimit = 3;

    public GuestService(
        IRedisService redisService,
        IJwtTokenService jwtTokenService,
        IOptions<GuestSecuritySettings> securitySettings,
        ILogger<GuestService> logger)
    {
        _redisService = redisService;
        _jwtTokenService = jwtTokenService;
        _securitySettings = securitySettings.Value;
        _logger = logger;
    }

    public async Task<CreateAnonymousSessionResponse> CreateAnonymousSessionAsync(
        string? cookieGuestUuid,
        string? clientIp,
        string? clientCountry,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra giới hạn vùng địa lý nếu công tắc EnforceVietnamOnly đang bật
        var country = string.IsNullOrWhiteSpace(clientCountry) ? "VN" : clientCountry.Trim().ToUpperInvariant();
        if (_securitySettings.EnforceVietnamOnly)
        {
            if (!_securitySettings.AllowedCountryCodes.Contains(country))
            {
                _logger.LogWarning("Chặn truy cập khách từ quốc gia không hỗ trợ: {Country} (IP: {Ip})", country, clientIp ?? "Unknown");
                throw AppException.Forbidden(
                    ErrorCodes.ErrGuestRegionUnsupported,
                    "IP ngoài lãnh thổ Việt Nam — dịch vụ chưa hỗ trợ khu vực này (BR-GUEST-06). Vui lòng đăng ký tài khoản để được hỗ trợ.");
            }
        }

        // 2. Nhận diện hoặc sinh mới Guest_UUID (UUID v4)
        string guestUuid = cookieGuestUuid?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(guestUuid) || !Guid.TryParse(guestUuid, out _))
        {
            guestUuid = Guid.NewGuid().ToString();
        }

        // 3. Cấp Anonymous JWT Token hiệu lực 24h mang vai trò GUEST
        var tokenResult = _jwtTokenService.GenerateGuestToken(guestUuid);

        return await Task.FromResult(new CreateAnonymousSessionResponse
        {
            TokenType = "Bearer",
            AccessToken = tokenResult.AccessToken,
            ExpiresIn = tokenResult.ExpiresInSeconds,
            Role = UserRoles.Guest,
            GuestId = guestUuid,
            CountryCode = country
        });
    }

    public async Task<GetGuestQuotaResponse> GetGuestQuotaAsync(string guestUuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guestUuid))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã định danh khách vãng lai (guestUuid) không được để trống.");
        }

        var aiKey = $"guest_quota:ai:{guestUuid}";
        var barcodeKey = $"guest_quota:barcode:{guestUuid}:{DateTime.UtcNow:yyyyMMdd}";
        var mapKey = $"guest_quota:map:{guestUuid}";

        var aiUsed = await _redisService.GetAsync<long?>(aiKey, ct) ?? 0;
        var barcodeUsed = await _redisService.GetAsync<long?>(barcodeKey, ct) ?? 0;
        var mapUsed = await _redisService.GetAsync<long?>(mapKey, ct) ?? 0;

        var resetAt = DateTime.UtcNow.Date.AddDays(1).ToString("o");

        return new GetGuestQuotaResponse
        {
            Ai = new GuestQuotaItemDto
            {
                Used = (int)aiUsed,
                Limit = DefaultAiTrialLimit,
                Remaining = Math.Max(0, DefaultAiTrialLimit - (int)aiUsed)
            },
            BarcodeDaily = new GuestQuotaItemDto
            {
                Used = (int)barcodeUsed,
                Limit = DefaultBarcodeDailyLimit,
                Remaining = Math.Max(0, DefaultBarcodeDailyLimit - (int)barcodeUsed)
            },
            MapSearch = new GuestQuotaItemDto
            {
                Used = (int)mapUsed,
                Limit = DefaultMapSearchLimit,
                Remaining = Math.Max(0, DefaultMapSearchLimit - (int)mapUsed)
            },
            ResetAt = resetAt
        };
    }

    public async Task<GuestTrialResultDto> ConsumeTrialQuotaAsync(
        string guestUuid,
        GuestTrialType trialType,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guestUuid))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã định danh khách vãng lai (guestUuid) không được để trống.");
        }

        string key;
        int limit;
        TimeSpan expiry;
        string errorCode;
        string exceededMessage;
        string successMessage;

        switch (trialType)
        {
            case GuestTrialType.Ai:
                key = $"guest_quota:ai:{guestUuid}";
                limit = DefaultAiTrialLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaAiExceeded;
                exceededMessage = "Khách đã dùng hết 03 lượt AI dùng thử (FN-AUTH-02, BR-GUEST-AI-01). Vui lòng đăng ký tài khoản để tiếp tục nhận trợ giúp từ AI.";
                successMessage = "Sử dụng thành công 01 lượt AI dùng thử.";
                break;

            case GuestTrialType.BarcodeDaily:
                key = $"guest_quota:barcode:{guestUuid}:{DateTime.UtcNow:yyyyMMdd}";
                limit = DefaultBarcodeDailyLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaBarcodeExceeded;
                exceededMessage = "Khách đã quét đủ 03 lần/ngày (FN-AUTH-02). Vui lòng đăng ký tài khoản để quét không giới hạn mã vạch thực phẩm.";
                successMessage = "Sử dụng thành công 01 lượt quét mã vạch dùng thử.";
                break;

            case GuestTrialType.MapSearch:
                key = $"guest_quota:map:{guestUuid}";
                limit = DefaultMapSearchLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaMapExceeded;
                exceededMessage = "Khách đã tìm quán đủ 03 lần (FN-AUTH-02). Vui lòng đăng ký tài khoản để tìm kiếm quán chay không giới hạn.";
                successMessage = "Sử dụng thành công 01 lượt tìm quán chay dùng thử.";
                break;

            default:
                throw AppException.BadRequest(ErrorCodes.ErrValidation, "Loại tính năng dùng thử không hợp lệ.");
        }

        // Thực hiện đếm và tiêu hao nguyên tử trên Redis / In-Memory (chống đua lệnh)
        var (allowed, current, remaining) = await _redisService.TryConsumeQuotaAsync(key, limit, expiry, ct);

        if (!allowed)
        {
            _logger.LogInformation("Khách {GuestUuid} đã vượt hạn ngạch dùng thử {TrialType} (Current: {Current}/{Limit})", 
                guestUuid, trialType, current, limit);
            throw AppException.TooManyRequests(errorCode, exceededMessage);
        }

        return new GuestTrialResultDto
        {
            TrialType = trialType.ToString(),
            Used = (int)current,
            Limit = limit,
            Remaining = (int)remaining,
            Message = successMessage
        };
    }
}
