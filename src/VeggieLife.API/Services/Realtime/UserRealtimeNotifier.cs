using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VeggieLife.API.Hubs;
using VeggieLife.BLL.Services.Realtime;

namespace VeggieLife.API.Services.Realtime;

/// <summary>
/// Triển khai dịch vụ bắn thông điệp SignalR tới người dùng.
/// </summary>
public class UserRealtimeNotifier : IUserRealtimeNotifier
{
    private readonly IHubContext<UserHub> _hubContext;
    private readonly ILogger<UserRealtimeNotifier> _logger;

    public UserRealtimeNotifier(IHubContext<UserHub> hubContext, ILogger<UserRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyForceLogoutAsync(long userId, string reason, string action, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                eventType = "FORCE_LOGOUT",
                userId = userId,
                action = action,
                reason = reason,
                message = "Tài khoản của bạn đã bị quản trị viên khóa hoặc cấm. Phiên làm việc đã bị thu hồi ngay lập tức.",
                timestamp = DateTime.UtcNow
            };

            // Gửi tới cả Client theo UserId và theo Group user_{userId}
            var userTask = _hubContext.Clients.User(userId.ToString()).SendAsync("ForceLogout", payload, ct);
            var groupTask = _hubContext.Clients.Group($"user_{userId}").SendAsync("ForceLogout", payload, ct);

            await Task.WhenAll(userTask, groupTask);

            _logger.LogInformation("Đã phát thông điệp SignalR ForceLogout thành công tới User ID #{UserId}. Lý do: {Reason}", userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi phát thông điệp SignalR ForceLogout tới User ID #{UserId}.", userId);
        }
    }
}

