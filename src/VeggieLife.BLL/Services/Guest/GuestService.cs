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
    private readonly IIpGeolocationService _geoService;
    private readonly GuestSecuritySettings _securitySettings;
    private readonly ILogger<GuestService> _logger;

    public const int DefaultChatbotTrialLimit = 3;
    public const int DefaultFridgeVisionTrialLimit = 3;
    public const int DefaultVideoSttTrialLimit = 3;
    public const int DefaultBarcodeDailyLimit = 3;
    public const int DefaultMapSearchLimit = 3;

    public const int DefaultAiTrialLimit = 3;

    public GuestService(
        IRedisService redisService,
        IJwtTokenService jwtTokenService,
        IOptions<GuestSecuritySettings> securitySettings,
        ILogger<GuestService> logger,
        IIpGeolocationService? geoService = null)
    {
        _redisService = redisService;
        _jwtTokenService = jwtTokenService;
        _securitySettings = securitySettings.Value;
        _logger = logger;
        _geoService = geoService ?? new IpGeolocationService(redisService, Microsoft.Extensions.Logging.Abstractions.NullLogger<IpGeolocationService>.Instance);
    }

    public async Task<CreateAnonymousSessionResponse> CreateAnonymousSessionAsync(
        string? cookieGuestUuid,
        string? clientIp,
        string? clientCountry,
        CancellationToken ct = default)
    {
        // 1. Phân giải quốc gia từ IP và Header (có lưu cache Redis 24 giờ - BR-GUEST-06, FN-AUTH-01)
        var country = await _geoService.ResolveCountryAsync(clientIp, clientCountry, ct);

        // Kiểm tra giới hạn vùng địa lý nếu công tắc EnforceVietnamOnly đang bật
        if (_securitySettings.EnforceVietnamOnly)
        {
            if (!_securitySettings.AllowedCountryCodes.Contains(country, StringComparer.OrdinalIgnoreCase))
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

        // 3. Khởi tạo phiên khách trên Redis nếu chưa tồn tại (TTL 24 giờ, KHÔNG ghi bảng SQL - BR-TECH-GUEST-02, FN-AUTH-01)
        var sessionKey = $"guest_session:{guestUuid}";
        var sessionExists = await _redisService.ExistsAsync(sessionKey, ct);
        if (!sessionExists)
        {
            var sessionData = new DTOs.Guest.GuestSessionData
            {
                GuestId = guestUuid,
                CountryCode = country,
                ClientIp = clientIp,
                CreatedAt = DateTime.UtcNow
            };
            await _redisService.SetAsync(sessionKey, sessionData, TimeSpan.FromHours(24), ct);
        }

        // 4. Cấp Anonymous JWT Token hiệu lực 24h mang vai trò GUEST, guestId, country (BR-TECH-GUEST-03)
        var tokenResult = _jwtTokenService.GenerateGuestToken(guestUuid, country)
                          ?? _jwtTokenService.GenerateGuestToken(guestUuid)
                          ?? throw new InvalidOperationException("Không thể cấp Anonymous JWT cho khách vãng lai.");

        return new CreateAnonymousSessionResponse
        {
            TokenType = "Bearer",
            AccessToken = tokenResult.AccessToken,
            ExpiresIn = tokenResult.ExpiresInSeconds,
            Role = UserRoles.Guest,
            GuestId = guestUuid,
            CountryCode = country
        };
    }

    public async Task<GetGuestQuotaResponse> GetGuestQuotaAsync(string guestUuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(guestUuid))
        {
            throw AppException.BadRequest(ErrorCodes.ErrValidation, "Mã định danh khách vãng lai (guestUuid) không được để trống.");
        }

        var chatbotKey = $"guest_quota:ai:{guestUuid}";
        var fridgeKey = $"guest_quota:fridge:{guestUuid}";
        var sttKey = $"guest_quota:stt:{guestUuid}";
        var barcodeKey = $"guest_quota:barcode:{guestUuid}:{DateTime.UtcNow:yyyyMMdd}";
        var mapKey = $"guest_quota:map:{guestUuid}";

        var chatbotUsed = (await _redisService.GetAsync<long?>($"guest_quota:chatbot:{guestUuid}", ct))
                          ?? (await _redisService.GetAsync<long?>(chatbotKey, ct))
                          ?? 0;
        var fridgeUsed = await _redisService.GetAsync<long?>(fridgeKey, ct) ?? 0;
        var sttUsed = await _redisService.GetAsync<long?>(sttKey, ct) ?? 0;
        var barcodeUsed = await _redisService.GetAsync<long?>(barcodeKey, ct) ?? 0;
        var mapUsed = await _redisService.GetAsync<long?>(mapKey, ct) ?? 0;

        var resetAt = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        return new GetGuestQuotaResponse
        {
            Chatbot = new GuestQuotaItemDto
            {
                Used = (int)chatbotUsed,
                Limit = DefaultChatbotTrialLimit,
                Remaining = Math.Max(0, DefaultChatbotTrialLimit - (int)chatbotUsed)
            },
            FridgeVision = new GuestQuotaItemDto
            {
                Used = (int)fridgeUsed,
                Limit = DefaultFridgeVisionTrialLimit,
                Remaining = Math.Max(0, DefaultFridgeVisionTrialLimit - (int)fridgeUsed)
            },
            VideoStt = new GuestQuotaItemDto
            {
                Used = (int)sttUsed,
                Limit = DefaultVideoSttTrialLimit,
                Remaining = Math.Max(0, DefaultVideoSttTrialLimit - (int)sttUsed)
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
            case GuestTrialType.Ai: // Cũng là GuestTrialType.Chatbot
                key = $"guest_quota:ai:{guestUuid}";
                limit = DefaultChatbotTrialLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaAiExceeded;
                exceededMessage = $"Khách đã dùng hết {DefaultChatbotTrialLimit:D2} lượt Chatbot AI dùng thử (FN-AUTH-02, BR-GUEST-AI-01). Vui lòng đăng ký tài khoản để tiếp tục nhận trợ giúp từ trợ lý AI.";
                successMessage = "Sử dụng thành công 01 lượt Chatbot AI dùng thử.";
                break;

            case GuestTrialType.FridgeVision:
                key = $"guest_quota:fridge:{guestUuid}";
                limit = DefaultFridgeVisionTrialLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaFridgeExceeded;
                exceededMessage = $"Khách đã dùng hết {DefaultFridgeVisionTrialLimit:D2} lượt nhận diện nguyên liệu tủ lạnh dùng thử (FN-AUTH-02). Vui lòng đăng ký tài khoản để nhận diện nguyên liệu tủ lạnh không giới hạn.";
                successMessage = "Sử dụng thành công 01 lượt nhận diện nguyên liệu tủ lạnh dùng thử.";
                break;

            case GuestTrialType.VideoStt:
                key = $"guest_quota:stt:{guestUuid}";
                limit = DefaultVideoSttTrialLimit;
                expiry = TimeSpan.FromHours(24);
                errorCode = ErrorCodes.ErrQuotaSttExceeded;
                exceededMessage = $"Khách đã dùng hết {DefaultVideoSttTrialLimit:D2} lượt xem bản phiên âm video dùng thử (FN-AUTH-02, BR-AI-STT-03). Vui lòng đăng ký tài khoản để xem toàn bộ bản phiên âm và công thức.";
                successMessage = "Sử dụng thành công 01 lượt xem phiên âm video dùng thử.";
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
