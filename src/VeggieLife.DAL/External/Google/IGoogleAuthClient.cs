using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.DAL.External.Google;

/// <summary>
/// Interface client xác thực Google OAuth 2.0 ID Token từ client gửi lên (FN-AUTH-07).
/// </summary>
public interface IGoogleAuthClient
{
    /// <summary>
    /// Xác thực tính hợp lệ của Google ID Token (chữ ký số, aud, exp, email_verified).
    /// </summary>
    /// <param name="idToken">Chuỗi JWT ID Token nhận từ Google OAuth Sign-In.</param>
    /// <param name="ct">Cancellation Token.</param>
    /// <returns>Đối tượng <see cref="GoogleTokenPayload"/> nếu token hợp lệ; ngược lại trả về null.</returns>
    Task<GoogleTokenPayload?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
