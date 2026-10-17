using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VeggieLife.DAL.External.Google;

/// <summary>
/// Client hiện thực xác thực Google ID Token sử dụng Google.Apis.Auth (FN-AUTH-07).
/// Tự động xác minh chữ ký số RSA từ JWKS Google (https://www.googleapis.com/oauth2/v3/certs),
/// kiểm tra thời hạn (exp), aud khớp ClientId và trạng thái email_verified.
/// </summary>
public class GoogleAuthClient : IGoogleAuthClient
{
    private readonly GoogleAuthSettings _settings;
    private readonly ILogger<GoogleAuthClient> _logger;

    public GoogleAuthClient(IOptions<GoogleAuthSettings> options, ILogger<GoogleAuthClient> logger)
    {
        _settings = options.Value ?? new GoogleAuthSettings();
        _logger = logger;
    }

    public async Task<GoogleTokenPayload?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings();

            if (!string.IsNullOrWhiteSpace(_settings.ClientId) &&
                !_settings.ClientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
            {
                validationSettings.Audience = new[] { _settings.ClientId };
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            if (payload == null)
            {
                _logger.LogWarning("GoogleJsonWebSignature xác thực token trả về null.");
                return null;
            }

            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Email Google {Email} chưa được xác thực (email_verified=false).", payload.Email);
                return null;
            }

            return new GoogleTokenPayload
            {
                Subject = payload.Subject,
                Email = payload.Email,
                EmailVerified = payload.EmailVerified,
                Name = payload.Name,
                Picture = payload.Picture
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google ID Token không hợp lệ hoặc đã hết hạn: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi bất thường khi xác thực Google ID Token: {Message}", ex.Message);
            return null;
        }
    }
}
