using System.Threading;
using System.Threading.Tasks;

namespace VeggieLife.BLL.Services.Realtime;

/// <summary>
/// Giao diện dịch vụ gửi thông điệp thời gian thực (Realtime Notification) qua WebSocket/SignalR tới người dùng.
/// Phục vụ cơ chế thu hồi phiên và thông báo đăng xuất tức thì (Force Logout) khi tài khoản bị khóa/cấm.
/// </summary>
public interface IUserRealtimeNotifier
{
    /// <summary>
    /// Gửi thông điệp ép buộc đăng xuất (Force Logout) ngay lập tức tới client của người dùng bị cấm.
    /// </summary>
    /// <param name="userId">Mã định danh ID người dùng bị xử phạt.</param>
    /// <param name="reason">Lý do bị cấm / khóa tài khoản.</param>
    /// <param name="action">Hành động kỷ luật (BAN, SUSPEND, RESTRICT_COMMENT, etc.).</param>
    /// <param name="ct">Cancellation Token.</param>
    Task NotifyForceLogoutAsync(long userId, string reason, string action, CancellationToken ct = default);
}

