using System;
using System.Collections.Generic;
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
    private readonly VeggieLife.DAL.External.Google.IGoogleAuthClient? _googleAuthClient;

    private const int OtpExpirySeconds = 300; // 5 phút
    private const int LockoutMinutes = 15; // 15 phút khóa khi sai 5 lần
    private const int MaxFailedAttempts = 5;

    public AuthService(
        AppDbContext dbContext,
        IRedisService redisService,
        IEmailService emailService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger,
        VeggieLife.DAL.External.Google.IGoogleAuthClient? googleAuthClient = null)
    {
        _dbContext = dbContext;
        _redisService = redisService;
        _emailService = emailService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _googleAuthClient = googleAuthClient;
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

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = existingUser.UserId,
                ActorRole = UserRoles.Member,
                Action = "USER_REGISTERED",
                TargetTable = "Users",
                TargetId = existingUser.UserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Note = "Cập nhật lại thông tin đăng ký (chưa kích hoạt) qua Email."
            });

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

            // 5. Ghi AuditLogs (Actor_Role = MEMBER, Action = USER_REGISTERED) theo FN-AUTH-04
            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = newUser.UserId,
                ActorRole = UserRoles.Member,
                Action = "USER_REGISTERED",
                TargetTable = "Users",
                TargetId = newUser.UserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Note = "Đăng ký tài khoản mới qua Email và Mật khẩu."
            });
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

        // 7. Ghi AuditLogs (Actor_Role = MEMBER, Action = USER_ACTIVATED) theo FN-AUTH-05
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = user.Role ?? UserRoles.Member,
            Action = "USER_ACTIVATED",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow,
            Note = "Kích hoạt tài khoản thành công qua xác thực OTP email."
        });

        await _dbContext.SaveChangesAsync(ct);

        // 7. Kiểm tra trạng thái VIP: còn PaymentTransactions SUCCESS có EndsAt > DateTime.UtcNow (D104)
        var isVip = user.Role == UserRoles.Vip || await _dbContext.PaymentTransactions
            .AnyAsync(pt => pt.UserId == user.UserId && pt.TransactionStatus == "SUCCESS" && pt.EndsAt > DateTime.UtcNow, ct);

        // 8. Cấp Access Token 15 phút và Refresh Token 7 ngày
        var tokenResult = _jwtTokenService.GenerateUserToken(user, isVip);

        // 9. Lưu Refresh Token lên Redis với thời hạn 7 ngày (BR-AUTH-004)
        var tokenHash = ComputeSha256(tokenResult.RefreshToken);
        var refreshSession = new RefreshTokenSession
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        await _redisService.SetAsync($"refresh:{tokenHash}", refreshSession, TimeSpan.FromDays(7), ct);

        var userSessionsKey = $"user_sessions:{user.UserId}";
        var sessions = await _redisService.GetAsync<List<string>>(userSessionsKey, ct) ?? new List<string>();
        if (!sessions.Contains(tokenHash))
        {
            sessions.Add(tokenHash);
        }
        await _redisService.SetAsync(userSessionsKey, sessions, TimeSpan.FromDays(7), ct);

        // 11. Kiểm tra xem người dùng có hồ sơ dinh dưỡng ban đầu chưa (BR-USR-01)
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
                FullName = user.FullName ?? string.Empty,
                Role = user.Role ?? UserRoles.Member,
                IsVip = isVip,
                AccountStatus = user.AccountStatus ?? AccountStatuses.Active,
                IsProfileCompleted = isProfileCompleted
            },
            RedirectUrl = isProfileCompleted ? "/dashboard" : "/onboarding/health-profile"
        };
    }

    public async Task ResendOtpAsync(ResendOtpRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra khóa 15 phút (otp_lock:{email}) theo BR-USR-AUTH-01
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Chức năng gửi lại mã OTP đang bị tạm khóa trong 15 phút do nhập sai quá nhiều lần. Vui lòng thử lại sau.");
        }

        // Chuẩn hóa Purpose (mặc định EMAIL_VERIFY nếu để trống)
        var purpose = string.IsNullOrWhiteSpace(request.Purpose)
            ? OtpPurposes.EmailVerify
            : request.Purpose.Trim().ToUpperInvariant();

        // 2. Tìm người dùng trong CSDL:
        // Theo FN-AUTH-06: EMAIL_VERIFY yêu cầu tài khoản PENDING_ACTIVATION; PASSWORD_RESET yêu cầu ACTIVE.
        // Nếu không thỏa: bỏ qua xử lý gửi mã nhưng VẪN TRẢ THÀNH CÔNG để không làm lộ email đã đăng ký hay chưa.
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        bool isEligible = false;
        if (purpose == OtpPurposes.EmailVerify)
        {
            isEligible = user != null && user.AccountStatus == AccountStatuses.PendingActivation;
        }
        else if (purpose == OtpPurposes.PasswordReset)
        {
            isEligible = user != null && user.AccountStatus == AccountStatuses.Active;
        }

        if (!isEligible)
        {
            _logger.LogInformation(
                "Yêu cầu gửi lại OTP cho {Email} với mục đích {Purpose} bị bỏ qua do người dùng không tồn tại hoặc trạng thái tài khoản không thỏa điều kiện.",
                normalizedEmail, purpose);
            return;
        }

        // 3. Sinh OTP mới 6 số bằng CSPRNG
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otpHash = ComputeSha256(otpCode);

        // 4. Ghi đè OTP cũ trên Redis với thời hạn 5 phút (TTL 300s): otp:{purpose}:{email} (SET, TTL 5m)
        var otpKey = $"otp:{purpose}:{normalizedEmail}";
        var cacheItem = new OtpCacheItem
        {
            OtpHash = otpHash,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            Purpose = purpose
        };

        await _redisService.SetAsync(otpKey, cacheItem, TimeSpan.FromSeconds(OtpExpirySeconds), ct);

        // 5. Gửi email bất đồng bộ
        await _emailService.SendOtpEmailAsync(normalizedEmail, otpCode, purpose, ct);

        _logger.LogInformation("Đã gửi lại mã OTP thành công cho {Email} với mục đích {Purpose}.", normalizedEmail, purpose);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? clientIp = null, string? userAgent = null, CancellationToken ct = default)
    {
        // 1. Kiểm tra đầu vào hợp lệ
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Email và mật khẩu không được để trống.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Tìm người dùng trong CSDL theo Email (bỏ qua query filter để xử lý tài khoản SOFT_DELETED trong 30 ngày)
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        // Bảo mật theo BR-AUTH-005: nếu user không tồn tại, trả lỗi chung không tiết lộ email
        if (user == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidCredentials,
                "Email hoặc mật khẩu không chính xác.");
        }

        // 3. Kiểm tra khóa đăng nhập 15 phút (Locked_Until) do sai quá 5 lần (BR-AUTH-005)
        if (user.LockedUntil.HasValue)
        {
            if (user.LockedUntil.Value > DateTime.UtcNow)
            {
                var remainingLockSeconds = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                _logger.LogWarning("Tài khoản {Email} bị khóa đến {LockedUntil}. Từ chối đăng nhập (429).", normalizedEmail, user.LockedUntil.Value);

                throw AppException.TooManyRequests(
                    ErrorCodes.ErrAuthLoginLocked,
                    "Tài khoản đã bị tạm khóa 15 phút do nhập sai mật khẩu 5 lần liên tiếp. Vui lòng thử lại sau.",
                    new { remainingLockSeconds, lockedUntil = user.LockedUntil.Value });
            }

            // Đã qua thời gian khóa 15 phút -> Mở khóa
            user.LockedUntil = null;
            user.FailedLoginCount = 0;
        }

        // 4. Xác thực mật khẩu bằng BCrypt
        bool isPasswordValid = false;
        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                isPasswordValid = false;
            }
        }

        if (!isPasswordValid)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogWarning("Tài khoản {Email} đã nhập sai mật khẩu 5 lần liên tiếp. Khóa 15 phút.", normalizedEmail);

                throw AppException.TooManyRequests(
                    ErrorCodes.ErrAuthLoginLocked,
                    "Tài khoản đã bị tạm khóa 15 phút do nhập sai mật khẩu 5 lần liên tiếp. Vui lòng thử lại sau.",
                    new { lockedUntil = user.LockedUntil.Value });
            }

            await _dbContext.SaveChangesAsync(ct);

            var remainingAttempts = MaxFailedAttempts - user.FailedLoginCount;
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidCredentials,
                $"Email hoặc mật khẩu không chính xác. Bạn còn {remainingAttempts} lần thử.",
                new { remainingAttempts });
        }

        // 5. Kiểm tra trạng thái tài khoản theo BR-ADM-USR-002, BR-ADM-USR-004
        if (user.AccountStatus == AccountStatuses.PendingActivation)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthEmailNotVerified,
                "Tài khoản chưa được kích hoạt qua email. Vui lòng xác thực mã OTP trước khi đăng nhập.");
        }

        if (user.AccountStatus == AccountStatuses.Suspended || user.AccountStatus == AccountStatuses.Banned)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthAccountBanned,
                "Tài khoản của bạn đã bị đình chỉ hoặc cấm do vi phạm điều khoản dịch vụ.");
        }

        if (user.AccountStatus == AccountStatuses.SoftDeleted)
        {
            var gracePeriodDays = 30;
            var deletedDate = user.UpdatedAt ?? user.CreatedAt;
            var daysSinceDeleted = (DateTime.UtcNow - deletedDate).TotalDays;

            if (daysSinceDeleted > gracePeriodDays)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrAuthAccountBanned,
                    "Tài khoản đã vượt quá thời gian ân hạn 30 ngày và đã bị xóa vĩnh viễn.");
            }

            if (request.RestoreAccount)
            {
                user.AccountStatus = AccountStatuses.Active;
                user.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Tài khoản {Email} đã được khôi phục thành công từ trạng thái SOFT_DELETED.", normalizedEmail);
            }
            else
            {
                var remainingGraceDays = Math.Max(0, gracePeriodDays - (int)daysSinceDeleted);
                throw AppException.Conflict(
                    ErrorCodes.ErrAuthAccountPendingDeletion,
                    "Tài khoản đang trong thời gian ân hạn xóa 30 ngày. Vui lòng xác nhận khôi phục tài khoản để tiếp tục đăng nhập.",
                    new { gracePeriodRemainingDays = remainingGraceDays });
            }
        }
        else if (user.AccountStatus != AccountStatuses.Active)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthAccountBanned,
                "Trạng thái tài khoản không hợp lệ để đăng nhập.");
        }

        // 6. Quy tắc riêng cho tài khoản ADMIN (BR-ADM-SYS-003)
        if (user.Role == UserRoles.Admin)
        {
            var adminSessionKey = $"admin_session:{user.UserId}";
            try
            {
                var activeAdminIp = await _redisService.GetAsync<string>(adminSessionKey, ct);
                if (!string.IsNullOrEmpty(activeAdminIp) && !string.IsNullOrEmpty(clientIp) && activeAdminIp != clientIp)
                {
                    throw AppException.Forbidden(
                        ErrorCodes.ErrAdmForbidden,
                        "Tài khoản Administrator đang có phiên đăng nhập hoạt động từ một địa chỉ IP khác (BR-ADM-SYS-003). Vui lòng đăng xuất phiên trước đó.");
                }

                await _redisService.SetAsync(adminSessionKey, clientIp ?? "127.0.0.1", TimeSpan.FromMinutes(30), ct);
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi kiểm tra phiên Admin trên Redis.");
            }
        }

        // 7. Reset số lần đăng nhập sai & cập nhật thời gian đăng nhập
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        // 8. Hợp nhất ngữ cảnh hội thoại khách vãng lai nếu có (BR-GUEST-AUTH-01)
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

        // 9. Cấp token: Access Token (15 phút) mang claim sub, role, isVip; Refresh Token (7 ngày) trên Redis
        var isVip = user.Role == UserRoles.Vip || await _dbContext.PaymentTransactions
            .AnyAsync(pt => pt.UserId == user.UserId && pt.TransactionStatus == "SUCCESS" && pt.EndsAt > DateTime.UtcNow, ct);

        var tokenResult = _jwtTokenService.GenerateUserToken(user, isVip);
        var tokenHash = ComputeSha256(tokenResult.RefreshToken);

        var refreshSession = new RefreshTokenSession
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            ClientIp = clientIp,
            UserAgent = userAgent
        };

        try
        {
            await _redisService.SetAsync($"refresh:{tokenHash}", refreshSession, TimeSpan.FromDays(7), ct);

            var userSessionsKey = $"user_sessions:{user.UserId}";
            var sessions = await _redisService.GetAsync<List<string>>(userSessionsKey, ct) ?? new List<string>();
            if (!sessions.Contains(tokenHash))
            {
                sessions.Add(tokenHash);
            }
            await _redisService.SetAsync(userSessionsKey, sessions, TimeSpan.FromDays(7), ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lưu trữ phiên refresh token trên Redis cho user {UserId}.", user.UserId);
        }

        // 10. Ghi AuditLog đăng nhập
        var auditLog = new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = user.Role,
            Action = "LOGIN",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            Note = "Người dùng đăng nhập thành công bằng Email/Mật khẩu.",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.AuditLogs.Add(auditLog);

        await _dbContext.SaveChangesAsync(ct);

        // 11. Kiểm tra trạng thái hoàn thiện hồ sơ dinh dưỡng
        var isProfileCompleted = await _dbContext.HealthProfiles
            .AnyAsync(hp => hp.UserId == user.UserId, ct);

        _logger.LogInformation("Người dùng {Email} (UserId: {UserId}, Role: {Role}) đăng nhập thành công.",
            normalizedEmail, user.UserId, user.Role);

        return new LoginResponse
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
                IsVip = isVip,
                AccountStatus = user.AccountStatus,
                IsProfileCompleted = isProfileCompleted
            }
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(string? refreshToken, string? clientIp = null, string? userAgent = null, CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số Refresh Token
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthRefreshTokenInvalid,
                "Không tìm thấy Refresh Token hoặc phiên đăng nhập đã hết hạn.");
        }

        var tokenHash = ComputeSha256(refreshToken.Trim());
        var refreshKey = $"refresh:{tokenHash}";

        // 2. Tra SHA-256(Refresh Token) trên Redis; không có thì 401
        RefreshTokenSession? session;
        try
        {
            session = await _redisService.GetAsync<RefreshTokenSession>(refreshKey, ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối Redis khi tra cứu refresh token.");
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrSysRedisUnavailable,
                "Không kết nối được Redis; hệ thống từ chối thay vì bỏ qua hạn mức.");
        }

        if (session == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthRefreshTokenInvalid,
                "Mã làm mới không hợp lệ hoặc đã hết hạn sử dụng.");
        }

        // 3. Phát hiện dùng lại mã làm mới cũ (TOKEN_REUSE_DETECTED theo BR-AUTH-004)
        if (session.IsRevoked)
        {
            _logger.LogWarning("Phát hiện Refresh Token đã thu hồi bị dùng lại cho User {UserId}.", session.UserId);

            // Thu hồi toàn bộ phiên của tài khoản đó trên Redis
            try
            {
                var userSessionsKey = $"user_sessions:{session.UserId}";
                var activeHashes = await _redisService.GetAsync<List<string>>(userSessionsKey, ct);
                if (activeHashes != null)
                {
                    foreach (var hash in activeHashes)
                    {
                        await _redisService.RemoveAsync($"refresh:{hash}", ct);
                    }
                }
                await _redisService.RemoveAsync(userSessionsKey, ct);
                await _redisService.RemoveAsync($"admin_session:{session.UserId}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi thu hồi toàn bộ phiên trên Redis cho User {UserId}.", session.UserId);
            }

            // Ghi AuditLog TOKEN_REUSE_DETECTED
            var existingUser = await _dbContext.Users.FindAsync(new object[] { session.UserId }, ct);
            var auditLog = new AuditLog
            {
                ActorUserId = session.UserId,
                ActorRole = existingUser?.Role ?? UserRoles.Member,
                Action = "TOKEN_REUSE_DETECTED",
                TargetTable = "Users",
                TargetId = session.UserId.ToString(),
                Note = "Phát hiện mã làm mới đã bị thu hồi được sử dụng lại. Toàn bộ phiên đăng nhập đã bị hủy.",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(ct);

            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthRefreshTokenReused,
                "Token đã thu hồi bị dùng lại; toàn bộ phiên bị hủy vì lý do an ninh.");
        }

        // 4. Kiểm tra trạng thái tài khoản người dùng trong CSDL
        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == session.UserId, ct);

        if (user == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthRefreshTokenInvalid,
                "Người dùng không tồn tại hoặc đã bị xóa khỏi hệ thống.");
        }

        if (user.AccountStatus == AccountStatuses.Suspended || user.AccountStatus == AccountStatuses.Banned)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthAccountBanned,
                "Tài khoản của bạn đã bị đình chỉ hoặc cấm.");
        }

        // 5. Thu hồi token cũ và cấp cặp Token mới (Token Rotation)
        session.IsRevoked = true;

        var isVip = user.Role == UserRoles.Vip || await _dbContext.PaymentTransactions
            .AnyAsync(pt => pt.UserId == user.UserId && pt.TransactionStatus == "SUCCESS" && pt.EndsAt > DateTime.UtcNow, ct);

        var tokenResult = _jwtTokenService.GenerateUserToken(user, isVip);
        var newTokenHash = ComputeSha256(tokenResult.RefreshToken);
        session.ReplacedByTokenHash = newTokenHash;

        try
        {
            // Lưu lại session cũ đã thu hồi với TTL 7 ngày để phát hiện dùng lại
            await _redisService.SetAsync(refreshKey, session, TimeSpan.FromDays(7), ct);

            // Lưu session mới vào Redis
            var newSession = new RefreshTokenSession
            {
                UserId = user.UserId,
                TokenHash = newTokenHash,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                ClientIp = clientIp,
                UserAgent = userAgent
            };
            await _redisService.SetAsync($"refresh:{newTokenHash}", newSession, TimeSpan.FromDays(7), ct);

            // Cập nhật danh sách session của người dùng
            var userSessionsKey = $"user_sessions:{user.UserId}";
            var sessions = await _redisService.GetAsync<List<string>>(userSessionsKey, ct) ?? new List<string>();
            if (!sessions.Contains(newTokenHash))
            {
                sessions.Add(newTokenHash);
            }
            await _redisService.SetAsync(userSessionsKey, sessions, TimeSpan.FromDays(7), ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối Redis khi cập nhật phiên làm mới.");
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrSysRedisUnavailable,
                "Không kết nối được Redis; hệ thống từ chối thay vì bỏ qua hạn mức.");
        }

        _logger.LogInformation("Làm mới token thành công cho User {UserId} ({Email}).", user.UserId, user.Email);

        return new RefreshTokenResponse
        {
            TokenType = "Bearer",
            AccessToken = tokenResult.AccessToken,
            RefreshToken = tokenResult.RefreshToken,
            ExpiresIn = tokenResult.ExpiresInSeconds
        };
    }

    public async Task LogoutAsync(long userId, string? userRole, string? jti, DateTime? tokenExpiryUtc, string? refreshToken, CancellationToken ct = default)
    {
        // 1. Đưa jti của Access Token vào danh sách đen Redis đến khi hết hạn (BR-AUTH-004)
        try
        {
            if (!string.IsNullOrEmpty(jti) && tokenExpiryUtc.HasValue)
            {
                var remainingTtl = tokenExpiryUtc.Value - DateTime.UtcNow;
                if (remainingTtl > TimeSpan.Zero)
                {
                    await _redisService.SetAsync($"blacklist:{jti}", "revoked", remainingTtl, ct);
                }
            }

            // 2. Thu hồi Refresh Token hiện tại trên Redis
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenHash = ComputeSha256(refreshToken.Trim());
                await _redisService.RemoveAsync($"refresh:{tokenHash}", ct);

                if (userId > 0)
                {
                    var userSessionsKey = $"user_sessions:{userId}";
                    var sessions = await _redisService.GetAsync<List<string>>(userSessionsKey, ct);
                    if (sessions != null && sessions.Remove(tokenHash))
                    {
                        await _redisService.SetAsync(userSessionsKey, sessions, TimeSpan.FromDays(7), ct);
                    }
                }
            }

            // 3. Xóa admin_session nếu là tài khoản Admin
            if (userId > 0)
            {
                await _redisService.RemoveAsync($"admin_session:{userId}", ct);
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối Redis khi đăng xuất.");
            throw AppException.ServiceUnavailable(
                ErrorCodes.ErrSysRedisUnavailable,
                "Không kết nối được Redis; hệ thống từ chối thay vì bỏ qua hạn mức.");
        }

        // 4. Ghi AuditLog USER_LOGOUT
        if (userId > 0)
        {
            var auditLog = new AuditLog
            {
                ActorUserId = (int)userId,
                ActorRole = userRole ?? UserRoles.Member,
                Action = "USER_LOGOUT",
                TargetTable = "Users",
                TargetId = userId.ToString(),
                Note = "Người dùng đăng xuất khỏi hệ thống thành công.",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Người dùng UserId {UserId} đã đăng xuất thành công.", userId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra khóa OTP 15 phút (otp_lock:{email}) theo BR-USR-AUTH-01
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Địa chỉ email này đang bị tạm khóa xác thực OTP trong vòng 15 phút. Vui lòng thử lại sau.");
        }

        // 2. Tìm Users ACTIVE theo Email; không có hoặc không ACTIVE thì bỏ qua nhưng vẫn trả thành công (chống User Enumeration)
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null || user.AccountStatus != AccountStatuses.Active)
        {
            _logger.LogInformation(
                "Yêu cầu quên mật khẩu cho {Email} bị bỏ qua do người dùng không tồn tại hoặc tài khoản không ở trạng thái ACTIVE.",
                normalizedEmail);
            return;
        }

        // 3. Sinh mã OTP 6 chữ số bằng CSPRNG (BR-USR-AUTH-01)
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var otpHash = ComputeSha256(otpCode);

        // 4. Lưu OTP trên Redis: otp:PASSWORD_RESET:{email} (SET, TTL 5m)
        var otpKey = $"otp:{OtpPurposes.PasswordReset}:{normalizedEmail}";
        var cacheItem = new OtpCacheItem
        {
            OtpHash = otpHash,
            FailedAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            Purpose = OtpPurposes.PasswordReset
        };

        await _redisService.SetAsync(otpKey, cacheItem, TimeSpan.FromSeconds(OtpExpirySeconds), ct);

        // 5. Gửi email bất đồng bộ
        await _emailService.SendOtpEmailAsync(normalizedEmail, otpCode, OtpPurposes.PasswordReset, ct);

        _logger.LogInformation("Đã gửi mã OTP đặt lại mật khẩu thành công tới email {Email}.", normalizedEmail);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Kiểm tra chuẩn mật khẩu mới theo BR-AUTH-001 (≥ 8 ký tự, hoa, thường, số, ký tự đặc biệt)
        var (isPassValid, passError) = PasswordValidator.Validate(request.NewPassword);
        if (!isPassValid)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthWeakPassword,
                passError ?? "Mật khẩu không đạt tiêu chuẩn bảo mật.");
        }

        // 2. Kiểm tra khóa 15 phút (otp_lock:{email}) theo BR-USR-AUTH-01
        var lockKey = $"otp_lock:{normalizedEmail}";
        if (await _redisService.ExistsAsync(lockKey, ct))
        {
            throw AppException.TooManyRequests(
                ErrorCodes.ErrAuthOtpLocked,
                "Bạn đã nhập sai mã OTP 5 lần liên tiếp. Chức năng xác thực bị tạm khóa trong vòng 15 phút. Vui lòng thử lại sau.");
        }

        // 3. Đọc OTP trên Redis; nếu không có hoặc hết hạn thì báo lỗi chung
        var otpKey = $"otp:{OtpPurposes.PasswordReset}:{normalizedEmail}";
        var cacheItem = await _redisService.GetAsync<OtpCacheItem>(otpKey, ct);

        if (cacheItem == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthOtpExpired,
                "Mã xác thực không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu gửi lại mã mới.");
        }

        // 4. So sánh chuỗi băm SHA-256 theo thời gian hằng (Constant-time comparison)
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

                _logger.LogWarning("Email {Email} đã nhập sai OTP đặt lại mật khẩu 5 lần. Đã khóa 15 phút.", normalizedEmail);

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

        // 5. Mã hợp lệ: Xóa OTP khỏi Redis
        await _redisService.RemoveAsync(otpKey, ct);

        // 6. Tìm người dùng trong CSDL
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user == null)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthOtpExpired,
                "Mã xác thực không hợp lệ hoặc tài khoản không tồn tại.");
        }

        // 7. Cập nhật mật khẩu mới, mở khóa tài khoản và reset đếm đăng nhập sai (FN-AUTH-12)
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;

        // 8. Thu hồi toàn bộ refresh token của tài khoản (BR-AUTH-004)
        try
        {
            var userSessionsKey = $"user_sessions:{user.UserId}";
            var activeHashes = await _redisService.GetAsync<List<string>>(userSessionsKey, ct);
            if (activeHashes != null)
            {
                foreach (var hash in activeHashes)
                {
                    await _redisService.RemoveAsync($"refresh:{hash}", ct);
                }
            }
            await _redisService.RemoveAsync(userSessionsKey, ct);
            await _redisService.RemoveAsync($"admin_session:{user.UserId}", ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi thu hồi các phiên đăng nhập cũ trên Redis cho User {UserId}.", user.UserId);
        }

        // 9. Ghi AuditLogs PASSWORD_RESET
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = user.Role ?? UserRoles.Member,
            Action = "PASSWORD_RESET",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow,
            Note = "Đặt lại mật khẩu thành công qua xác thực mã OTP."
        });

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Người dùng {Email} (UserId: {UserId}) đã đặt lại mật khẩu thành công.", normalizedEmail, user.UserId);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, string? currentRefreshToken = null, CancellationToken ct = default)
    {
        // 1. Kiểm tra tham số và tìm người dùng trong CSDL
        if (userId <= 0)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserId == (int)userId, ct);

        if (user == null)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidToken,
                "Người dùng không tồn tại hoặc phiên đăng nhập không hợp lệ.");
        }

        // 2. Không áp dụng cho tài khoản chỉ dùng Google (chưa tạo mật khẩu)
        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Tài khoản đăng nhập bằng Google không thể đổi mật khẩu qua chức năng này.");
        }

        // 3. Xác minh mật khẩu hiện tại bằng BCrypt
        var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthInvalidCredentials,
                "Mật khẩu hiện tại không chính xác.");
        }

        // 4. Kiểm tra độ mạnh mật khẩu mới theo chuẩn BR-AUTH-001
        var (isPassValid, passError) = PasswordValidator.Validate(request.NewPassword);
        if (!isPassValid)
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthWeakPassword,
                passError ?? "Mật khẩu mới không đạt tiêu chuẩn bảo mật.");
        }

        // 5. Kiểm tra mật khẩu mới phải khác mật khẩu hiện tại
        if (request.CurrentPassword == request.NewPassword || BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrAuthWeakPassword,
                "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        // 6. Cập nhật PasswordHash mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        user.UpdatedAt = DateTime.UtcNow;

        // 7. Thu hồi các refresh token khác của tài khoản (BR-AUTH-004)
        try
        {
            var userSessionsKey = $"user_sessions:{user.UserId}";
            var activeHashes = await _redisService.GetAsync<List<string>>(userSessionsKey, ct);
            if (activeHashes != null)
            {
                string? currentHash = !string.IsNullOrEmpty(currentRefreshToken)
                    ? ComputeSha256(currentRefreshToken.Trim())
                    : null;

                var remainingSessions = new List<string>();

                foreach (var hash in activeHashes)
                {
                    if (currentHash != null && hash == currentHash)
                    {
                        // Giữ lại phiên hiện tại của thiết bị đang gọi
                        remainingSessions.Add(hash);
                    }
                    else
                    {
                        // Thu hồi các phiên khác
                        await _redisService.RemoveAsync($"refresh:{hash}", ct);
                    }
                }

                if (remainingSessions.Count > 0)
                {
                    await _redisService.SetAsync(userSessionsKey, remainingSessions, TimeSpan.FromDays(7), ct);
                }
                else
                {
                    await _redisService.RemoveAsync(userSessionsKey, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi thu hồi các phiên đăng nhập khác trên Redis cho User {UserId}.", user.UserId);
        }

        // 8. Ghi AuditLogs PASSWORD_CHANGED
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.UserId,
            ActorRole = user.Role ?? UserRoles.Member,
            Action = "PASSWORD_CHANGED",
            TargetTable = "Users",
            TargetId = user.UserId.ToString(),
            CreatedAt = DateTime.UtcNow,
            Note = "Người dùng đổi mật khẩu thành công. Các phiên đăng nhập khác đã bị thu hồi."
        });

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Người dùng UserId {UserId} ({Email}) đã đổi mật khẩu thành công.", user.UserId, user.Email);
    }

    /// <summary>
    /// FN-AUTH-07: Đăng nhập hoặc đăng ký nhanh bằng Google OAuth ID Token.
    /// Xác thực Google ID Token; tạo tài khoản ACTIVE mới (bỏ qua OTP), đăng nhập tài khoản đã liên kết,
    /// hoặc liên kết Google vào tài khoản mật khẩu cùng Email sau khi người dùng xác nhận mật khẩu hiện tại (BR-USR-AUTH-02).
    /// </summary>
    public async Task<LoginWithGoogleResponse> LoginWithGoogleAsync(
        LoginWithGoogleRequest request,
        string? clientIp = null,
        string? userAgent = null,
        CancellationToken ct = default)
    {
        // 1. Kiểm tra dữ liệu đầu vào bắt buộc
        if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw AppException.BadRequest(
                ErrorCodes.ErrValidation,
                "Dữ liệu đầu vào sai định dạng hoặc thiếu trường bắt buộc.");
        }

        // 2. Xác minh chữ ký Google, aud, exp, email_verified qua client Google
        if (_googleAuthClient == null)
        {
            _logger.LogError("IGoogleAuthClient chưa được cấu hình hoặc tiêm phụ thuộc trong AuthService.");
            throw AppException.Internal(
                ErrorCodes.ErrInternal,
                "Dịch vụ xác thực Google chưa sẵn sàng.");
        }

        VeggieLife.DAL.External.Google.GoogleTokenPayload? payload;
        try
        {
            payload = await _googleAuthClient.ValidateIdTokenAsync(request.IdToken.Trim(), ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi xảy ra trong quá trình xác thực Google ID Token.");
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthOauthFailed,
                "ID Token không hợp lệ.");
        }

        if (payload == null ||
            string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            !payload.EmailVerified)
        {
            throw AppException.Unauthorized(
                ErrorCodes.ErrAuthOauthFailed,
                "ID Token không hợp lệ.");
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();
        bool isNewAccount = false;
        User? user;

        try
        {
            // 3. Tìm Users theo Google_ID; có thì sang bước đăng nhập
            user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject, ct);

            if (user == null)
            {
                // Tìm kiếm tài khoản theo Email
                var existingUserByEmail = await _dbContext.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

                if (existingUserByEmail == null)
                {
                    // Email chưa tồn tại: INSERT Users (Account_Status = ACTIVE, Google_ID, Password_Hash = NULL)
                    var displayName = !string.IsNullOrWhiteSpace(payload.Name)
                        ? payload.Name.Trim()
                        : normalizedEmail.Split('@')[0];

                    user = new User
                    {
                        FullName = displayName,
                        Email = normalizedEmail,
                        GoogleId = payload.Subject,
                        PasswordHash = null,
                        AvatarUrl = payload.Picture,
                        Role = UserRoles.Member,
                        AccountStatus = AccountStatuses.Active,
                        FailedLoginCount = 0,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync(ct);

                    isNewAccount = true;

                    // Ghi AuditLogs (Actor_Role = MEMBER, Action = USER_REGISTERED)
                    _dbContext.AuditLogs.Add(new AuditLog
                    {
                        ActorUserId = user.UserId,
                        ActorRole = user.Role,
                        Action = "USER_REGISTERED",
                        TargetTable = "Users",
                        TargetId = user.UserId.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        Note = "Đăng ký tài khoản mới qua Google OAuth (bỏ qua OTP, trạng thái ACTIVE)."
                    });

                    await _dbContext.SaveChangesAsync(ct);
                }
                else
                {
                    // Email đã tồn tại: Kiểm tra nếu đã liên kết Google_ID khác
                    if (!string.IsNullOrEmpty(existingUserByEmail.GoogleId) &&
                        existingUserByEmail.GoogleId != payload.Subject)
                    {
                        throw AppException.Conflict(
                            ErrorCodes.ErrAuthAccountLinkRequired,
                            "Email này đã được liên kết với một tài khoản Google khác.");
                    }

                    // Email đã có tài khoản mật khẩu: thiếu currentPassword thì trả 409 yêu cầu liên kết (BR-USR-AUTH-02)
                    if (!string.IsNullOrEmpty(existingUserByEmail.PasswordHash))
                    {
                        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                        {
                            throw AppException.Conflict(
                                ErrorCodes.ErrAuthAccountLinkRequired,
                                "Email đã có tài khoản mật khẩu; cần currentPassword.",
                                new { email = normalizedEmail });
                        }

                        // Đúng mật khẩu thì UPDATE Users.Google_ID (BR-USR-AUTH-02)
                        bool isPasswordValid = false;
                        try
                        {
                            isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, existingUserByEmail.PasswordHash);
                        }
                        catch
                        {
                            isPasswordValid = false;
                        }

                        if (!isPasswordValid)
                        {
                            throw AppException.Unauthorized(
                                ErrorCodes.ErrAuthInvalidCredentials,
                                "Mật khẩu xác nhận liên kết sai.");
                        }

                        existingUserByEmail.GoogleId = payload.Subject;
                        if (existingUserByEmail.AccountStatus == AccountStatuses.PendingActivation)
                        {
                            existingUserByEmail.AccountStatus = AccountStatuses.Active;
                        }
                        if (string.IsNullOrEmpty(existingUserByEmail.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                        {
                            existingUserByEmail.AvatarUrl = payload.Picture;
                        }
                        existingUserByEmail.UpdatedAt = DateTime.UtcNow;

                        _dbContext.AuditLogs.Add(new AuditLog
                        {
                            ActorUserId = existingUserByEmail.UserId,
                            ActorRole = existingUserByEmail.Role,
                            Action = "ACCOUNT_LINKED",
                            TargetTable = "Users",
                            TargetId = existingUserByEmail.UserId.ToString(),
                            CreatedAt = DateTime.UtcNow,
                            Note = "Liên kết Google OAuth vào tài khoản mật khẩu thành công (BR-USR-AUTH-02)."
                        });

                        await _dbContext.SaveChangesAsync(ct);
                        user = existingUserByEmail;
                        isNewAccount = false;
                    }
                    else
                    {
                        // Tài khoản chưa có PasswordHash -> cập nhật Google_ID trực tiếp
                        existingUserByEmail.GoogleId = payload.Subject;
                        if (existingUserByEmail.AccountStatus == AccountStatuses.PendingActivation)
                        {
                            existingUserByEmail.AccountStatus = AccountStatuses.Active;
                        }
                        if (string.IsNullOrEmpty(existingUserByEmail.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                        {
                            existingUserByEmail.AvatarUrl = payload.Picture;
                        }
                        existingUserByEmail.UpdatedAt = DateTime.UtcNow;

                        await _dbContext.SaveChangesAsync(ct);
                        user = existingUserByEmail;
                        isNewAccount = false;
                    }
                }
            }
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối cơ sở dữ liệu khi xác thực Google OAuth: {Message}", ex.Message);
            throw AppException.Internal(
                ErrorCodes.ErrSysDatabaseUnavailable,
                "Không kết nối được SQL Server.",
                ex.Message);
        }

        // 4. Account_Status SUSPENDED / BANNED thì từ chối (BR-ADM-USR-002)
        if (user.AccountStatus == AccountStatuses.Suspended || user.AccountStatus == AccountStatuses.Banned)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthAccountBanned,
                "Tài khoản bị đình chỉ/cấm.");
        }

        if (user.AccountStatus == AccountStatuses.SoftDeleted)
        {
            var gracePeriodDays = 30;
            var deletedDate = user.UpdatedAt ?? user.CreatedAt;
            var daysSinceDeleted = (DateTime.UtcNow - deletedDate).TotalDays;

            if (daysSinceDeleted > gracePeriodDays)
            {
                throw AppException.Forbidden(
                    ErrorCodes.ErrAuthAccountBanned,
                    "Tài khoản bị đình chỉ/cấm.");
            }

            user.AccountStatus = AccountStatuses.Active;
            user.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Tài khoản {Email} đã khôi phục thành công từ SOFT_DELETED qua Google OAuth.", normalizedEmail);
        }
        else if (user.AccountStatus != AccountStatuses.Active)
        {
            throw AppException.Forbidden(
                ErrorCodes.ErrAuthAccountBanned,
                "Tài khoản bị đình chỉ/cấm.");
        }

        // Đặt lại số lần sai & thời gian đăng nhập
        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        // 5. Hợp nhất ngữ cảnh Khách nếu có GuestUuid (BR-GUEST-AUTH-01)
        if (!string.IsNullOrWhiteSpace(request.GuestUuid))
        {
            try
            {
                var guestSessions = await _dbContext.ChatSessions
                    .Where(cs => cs.GuestUuid == request.GuestUuid && cs.UserId == null)
                    .ToListAsync(ct);

                foreach (var session in guestSessions)
                {
                    session.UserId = user.UserId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi hợp nhất ngữ cảnh khách vãng lai cho user {UserId}.", user.UserId);
            }
        }

        // 6. Cấp token (BR-AUTH-004) Access Token mang claim sub, role (MEMBER | VIP | ADMIN, lấy từ Users.Role)
        // và isVip (true khi còn PaymentTransactions SUCCESS có Ends_At > hiện tại) (D104);
        bool isVip = false;
        try
        {
            isVip = user.Role == UserRoles.Vip || await _dbContext.PaymentTransactions
                .AnyAsync(pt => pt.UserId == user.UserId && pt.TransactionStatus == "SUCCESS" && pt.EndsAt > DateTime.UtcNow, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi kiểm tra trạng thái VIP của user {UserId}.", user.UserId);
        }

        var tokenResult = _jwtTokenService.GenerateUserToken(user, isVip);
        var tokenHash = ComputeSha256(tokenResult.RefreshToken);

        var refreshSession = new RefreshTokenSession
        {
            UserId = user.UserId,
            TokenHash = tokenHash,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            ClientIp = clientIp,
            UserAgent = userAgent
        };

        try
        {
            await _redisService.SetAsync($"refresh:{tokenHash}", refreshSession, TimeSpan.FromDays(7), ct);

            var userSessionsKey = $"user_sessions:{user.UserId}";
            var sessions = await _redisService.GetAsync<List<string>>(userSessionsKey, ct) ?? new List<string>();
            if (!sessions.Contains(tokenHash))
            {
                sessions.Add(tokenHash);
            }
            await _redisService.SetAsync(userSessionsKey, sessions, TimeSpan.FromDays(7), ct);
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lưu trữ phiên refresh token trên Redis cho user {UserId}.", user.UserId);
        }

        // Ghi AuditLogs đăng nhập
        try
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user.UserId,
                ActorRole = user.Role,
                Action = "LOGIN",
                TargetTable = "Users",
                TargetId = user.UserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Note = isNewAccount
                    ? "Đăng nhập lần đầu sau khi tạo tài khoản bằng Google OAuth."
                    : "Đăng nhập thành công bằng Google OAuth."
            });

            await _dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi ghi nhận AuditLog khi đăng nhập Google cho user {UserId}.", user.UserId);
        }

        // Kiểm tra hoàn thiện hồ sơ dinh dưỡng ban đầu
        var isProfileCompleted = false;
        try
        {
            isProfileCompleted = await _dbContext.HealthProfiles
                .AnyAsync(hp => hp.UserId == user.UserId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi kiểm tra hồ sơ sức khỏe cho user {UserId}.", user.UserId);
        }

        _logger.LogInformation("Người dùng {Email} (UserId: {UserId}, isNewAccount: {IsNewAccount}) đăng nhập Google thành công.",
            normalizedEmail, user.UserId, isNewAccount);

        return new LoginWithGoogleResponse
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
                IsVip = isVip,
                AccountStatus = user.AccountStatus,
                IsProfileCompleted = isProfileCompleted
            },
            IsNewAccount = isNewAccount
        };
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
