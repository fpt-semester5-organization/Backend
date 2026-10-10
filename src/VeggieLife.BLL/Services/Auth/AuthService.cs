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
        var isVip = user.Role == UserRoles.Vip;
        var tokenResult = _jwtTokenService.GenerateUserToken(user, isVip);

        // Lưu Refresh Token lên Redis với thời hạn 7 ngày (BR-AUTH-004)
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

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
