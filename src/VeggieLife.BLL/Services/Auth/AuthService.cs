using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.DTOs.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Validators.Auth;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.Auth;

/// <summary>
/// Triển khai dịch vụ xác thực tài khoản và quản lý vòng đời mã OTP (FN-AUTH-04, FN-AUTH-05, FN-AUTH-06).
/// Tuân thủ các quy tắc nghiệp vụ BR-AUTH-001, BR-USR-AUTH-01, BR-ADM-USR-002, BR-GUEST-AUTH-01.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    private const int OtpExpirySeconds = 300; // 5 phút
    private const int LockoutMinutes = 15; // 15 phút khóa khi sai 5 lần
    private const int MaxFailedAttempts = 5;

    public AuthService(
        AppDbContext dbContext,
        IRedisService redisService,
        IEmailService emailService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _redisService = redisService;
        _emailService = emailService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // 1. Kiểm tra xác nhận mật khẩu
        if (request.Password != request.ConfirmPassword)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.");
        }

        // 2. Kiểm tra độ mạnh mật khẩu theo BR-AUTH-001 (≥ 8 ký tự, hoa, thường, số, ký tự đặc biệt)
        var (isPassValid, passError) = PasswordValidator.Validate(request.Password);
        if (!isPassValid)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthWeakPassword,
                passError ?? "Mật khẩu không đạt tiêu chuẩn bảo mật.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 3. Kiểm tra xem Email đã tồn tại trong CSDL hay chưa
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (existingUser != null)
        {
            if (existingUser.AccountStatus == AccountStatuses.Active)
            {
                // Email đã kích hoạt trước đó
                throw AppException.Conflict(
                    ErrorCodes.ErrAuthEmailExists,
                    "Email này đã được đăng ký trong hệ thống. Vui lòng đăng nhập hoặc sử dụng chức năng quên mật khẩu.");
            }

            // Nếu tài khoản đang ở trạng thái PENDING_ACTIVATION:
            // Cập nhật lại họ tên và mật khẩu mới để người dùng kích hoạt tiếp
            existingUser.FullName = request.FullName.Trim();
            existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
            existingUser.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);
        }
        else
        {
            // 4. Tạo tài khoản mới ở trạng thái PENDING_ACTIVATION (BR-ADM-USR-002)
            var newUser = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
                Role = UserRoles.Member,
                AccountStatus = AccountStatuses.PendingActivation,
                CreatedAt = DateTime.UtcNow,
                FailedLoginCount = 0
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync(ct);
        }

        // 5. Kiểm tra trạng thái khóa OTP 15 phút (otp_lock:{email})
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Địa chỉ email này đang bị tạm khóa xác thực OTP trong vòng 15 phút. Vui lòng thử lại sau.");
        }

        // 6. Sinh mã OTP 6 số ngẫu nhiên an toàn mật mã CSPRNG (BR-USR-AUTH-01)
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otpHash = ComputeSha256(otpCode);

        // 7. Lưu SHA-256(OTP) vào Redis với thời gian sống 5 phút (TTL 300s)
        var otpKey = $"otp:EMAIL_VERIFY:{normalizedEmail}";
        var cacheItem = new OtpCacheItem
        {
            OtpHash = otpHash,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.EmailVerify
        };

        await _redisService.SetAsync(otpKey, cacheItem, TimeSpan.FromSeconds(OtpExpirySeconds), ct);

        // 8. Gửi Email thông báo mã OTP bất đồng bộ
        await _emailService.SendOtpEmailAsync(normalizedEmail, otpCode, OtpPurposes.EmailVerify, ct);

        _logger.LogInformation("Đăng ký thành công cho {Email}, mã OTP đã gửi qua email.", normalizedEmail);

        return new RegisterResponse
        {
            Email = normalizedEmail,
            OtpExpiresIn = OtpExpirySeconds
        };
    }

    public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra khóa 15 phút (otp_lock:{email}) theo BR-USR-AUTH-01
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Bạn đã nhập sai mã OTP 5 lần liên tiếp. Chức năng xác thực bị tạm khóa trong vòng 15 phút. Vui lòng thử lại sau.");
        }

        // 2. Đọc OTP trên Redis; nếu không có hoặc hết hạn thì báo lỗi chung (không tiết lộ Email có tồn tại)
        var otpKey = $"otp:EMAIL_VERIFY:{normalizedEmail}";
        var cacheItem = await _redisService.GetAsync<OtpCacheItem>(otpKey, ct);

        if (cacheItem == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthOtpExpired,
                "Mã xác thực không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã mới.");
        }

        // 3. So sánh chuỗi băm SHA-256 theo thời gian hằng (Constant-time comparison) để chống Side-channel attack
        var inputHash = ComputeSha256(request.Otp.Trim());
        var isMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(inputHash),
            Encoding.UTF8.GetBytes(cacheItem.OtpHash));

        if (!isMatch)
        {
            cacheItem.FailedAttempts++;

            // Nếu sai đủ 5 lần: xóa mã OTP và kích hoạt khóa 15 phút
            if (cacheItem.FailedAttempts >= MaxFailedAttempts)
            {
                await _redisService.RemoveAsync(otpKey, ct);
                await _redisService.SetAsync(lockKey, "LOCKED", TimeSpan.FromMinutes(LockoutMinutes), ct);

                _logger.LogWarning("Email {Email} đã nhập sai OTP 5 lần. Đã khóa 15 phút.", normalizedEmail);

                throw AppException.TooManyRequests(
                    ErrorCodes.ErrAuthOtpLocked,
                    "Bạn đã nhập sai mã OTP 5 lần liên tiếp. Chức năng xác thực bị tạm khóa trong vòng 15 phút. Vui lòng thử lại sau.");
            }

            // Chưa đủ 5 lần: cập nhật lại số lần sai trên Redis với thời gian sống còn lại
            var elapsed = DateTime.UtcNow - cacheItem.CreatedAt;
            var remainingTtl = TimeSpan.FromSeconds(OtpExpirySeconds) - elapsed;
            if (remainingTtl <= TimeSpan.Zero)
            {
                remainingTtl = TimeSpan.FromSeconds(10);
            }

            await _redisService.SetAsync(otpKey, cacheItem, remainingTtl, ct);

            var remainingAttempts = MaxFailedAttempts - cacheItem.FailedAttempts;
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthOtpIncorrect,
                $"Mã xác thực OTP không chính xác. Bạn còn {remainingAttempts} lần thử.",
                new { remainingAttempts });
        }

        // 4. Mã hợp lệ: Xóa OTP khỏi Redis
        await _redisService.RemoveAsync(otpKey, ct);

        // 5. Cập nhật trạng thái người dùng trong CSDL: Account_Status = ACTIVE
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null)
        {
            // Bảo mật: không tiết lộ trạng thái cụ thể
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthOtpExpired,
                "Mã xác thực không hợp lệ hoặc tài khoản không tồn tại.");
        }

        user.AccountStatus = AccountStatuses.Active;
        user.FailedLoginCount = 0;
        user.UpdatedAt = DateTime.UtcNow;

        // 6. Hợp nhất ngữ cảnh hội thoại AI nếu có Guest_UUID (BR-GUEST-AUTH-01)
        if (!string.IsNullOrWhiteSpace(request.GuestUuid))
        {
            var guestSessions = await _dbContext.ChatSessions
                .Where(cs => cs.GuestUuid == request.GuestUuid && cs.UserId == null)
                .ToListAsync(ct);

            foreach (var session in guestSessions)
            {
                session.UserId = user.UserId;
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        // 7. Cấp Access Token và Refresh Token
        var tokenResult = _jwtTokenService.GenerateUserToken(user);

        // Kiểm tra xem người dùng có hồ sơ dinh dưỡng ban đầu chưa
        var isProfileCompleted = await _dbContext.HealthProfiles
            .AnyAsync(hp => hp.UserId == user.UserId, ct);

        _logger.LogInformation("Tài khoản {Email} đã kích hoạt thành công (UserId: {UserId}).", normalizedEmail, user.UserId);

        return new VerifyOtpResponse
        {
            TokenType = "Bearer",
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresIn = tokenResult.ExpiresInSeconds,
            User = new AuthUserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsVip = user.Role == UserRoles.Vip,
                AccountStatus = user.AccountStatus,
                IsProfileCompleted = isProfileCompleted
            },
            RedirectUrl = isProfileCompleted ? "/dashboard" : "/onboarding/health-profile"
        };
    }

    public async Task ResendOtpAsync(ResendOtpRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra khóa 15 phút (otp_lock:{email})
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Chức năng gửi lại mã OTP đang bị tạm khóa trong 15 phút do nhập sai quá nhiều lần. Vui lòng thử lại sau.");
        }

        // 2. Tìm người dùng trong CSDL:
        // Theo FN-AUTH-06: EMAIL_VERIFY yêu cầu tài khoản PENDING_ACTIVATION;
        // Nếu không thỏa, bỏ qua xử lý gửi mã nhưng VẪN TRẢ THÀNH CÔNG để không làm lộ email đã đăng ký hay chưa.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null || user.AccountStatus != AccountStatuses.PendingActivation)
        {
            _logger.LogInformation(
                "Yêu cầu gửi lại OTP cho {Email} bị bỏ qua do người dùng không tồn tại hoặc không ở trạng thái PENDING_ACTIVATION.", 
                normalizedEmail);
            return;
        }

        // 3. Sinh OTP mới 6 số
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otpHash = ComputeSha256(otpCode);

        // 4. Ghi đè OTP cũ trên Redis với thời hạn 5 phút (TTL 300s)
        var otpKey = $"otp:EMAIL_VERIFY:{normalizedEmail}";
        var cacheItem = new OtpCacheItem
        {
            OtpHash = otpHash,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.EmailVerify
        };

        await _redisService.SetAsync(otpKey, cacheItem, TimeSpan.FromSeconds(OtpExpirySeconds), ct);

        // 5. Gửi email
        await _emailService.SendOtpEmailAsync(normalizedEmail, otpCode, OtpPurposes.EmailVerify, ct);

        _logger.LogInformation("Đã gửi lại mã OTP thành công cho {Email}.", normalizedEmail);
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
