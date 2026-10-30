using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Services.Cache;

namespace VeggieLife.API.Hubs;

/// <summary>
/// SignalR Hub quản lý kết nối thời gian thực hai chiều giữa người dùng và máy chủ.
/// Hỗ trợ cơ chế thu hồi phiên và thông báo đăng xuất tức thì (Force Logout) khi tài khoản bị khóa/cấm.
/// </summary>
[Authorize]
public class UserHub : Hub
{
    private readonly IRedisService _redis;
    private readonly ILogger<UserHub> _logger;

    public UserHub(IRedisService redis, ILogger<UserHub> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Kiểm tra nếu tài khoản đang nằm trong blacklist -> ngắt kết nối ngay
            var isBlacklisted = await _redis.ExistsAsync($"user_blacklist:{userId}");
            if (isBlacklisted)
            {
                _logger.LogWarning("Từ chối kết nối SignalR của người dùng đã bị cấm ID {UserId}.", userId);
                Context.Abort();
                return;
            }

            // Đưa connection vào nhóm riêng của user theo ID
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("Người dùng ID {UserId} đã kết nối thành công tới UserHub (ConnectionId: {ConnectionId}).", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("Người dùng ID {UserId} đã ngắt kết nối UserHub.", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}

