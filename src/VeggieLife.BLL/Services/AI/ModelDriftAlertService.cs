using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;

namespace VeggieLife.BLL.Services.AI;

/// <summary>
/// Hiện thực dịch vụ đánh giá chất lượng phản hồi AI chatbot và phát cảnh báo cho Admin (FN-CHAT-06, BR-ADM-AI-004).
/// Chạy lặp lại an toàn (Idempotent).
/// </summary>
public class ModelDriftAlertService : IModelDriftAlertService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<ModelDriftAlertService> _logger;
    private readonly TimeProvider _timeProvider;

    public ModelDriftAlertService(
        AppDbContext db,
        IEmailService emailService,
        ILogger<ModelDriftAlertService> logger,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Tính tỷ lệ đánh giá 1-2 sao trên tổng Surveys của từng ngày trong 07 ngày gần nhất.
    /// Nếu cả 07 ngày liên tiếp đều > 20%, ghi AuditLogs (Action = MODEL_DRIFT_ALERT) để dashboard LlmOps
    /// hiển thị cờ cảnh báo suy thoái độ chính xác mô hình (BR-ADM-AI-004).
    /// </summary>
    public async Task<bool> CheckAndAlertModelDriftAsync(DateTime? customNow = null, CancellationToken ct = default)
    {
        var now = customNow ?? _timeProvider.GetUtcNow().UtcDateTime;
        var today = now.Date;

        // Lấy tất cả Surveys trong vòng 8 ngày trở lại đây để phân tích các cửa sổ 7 ngày liên tiếp
        var earliestDate = today.AddDays(-7);
        var latestDate = today.AddDays(1);

        var surveys = await _db.Surveys
            .Where(s => s.CreatedAt >= earliestDate && s.CreatedAt < latestDate)
            .ToListAsync(ct);

        // Kiểm tra 2 cửa sổ 7 ngày hợp lệ:
        // Cửa sổ A: 7 ngày hoàn chỉnh trước hôm nay [today - 7, today)
        // Cửa sổ B: 7 ngày tính cả hôm nay [today - 6, today + 1)
        var windowA = Evaluate7DaysWindow(surveys, today.AddDays(-7));
        var windowB = Evaluate7DaysWindow(surveys, today.AddDays(-6));

        var chosenWindow = windowA.IsDrift ? windowA : (windowB.IsDrift ? windowB : default);

        if (!chosenWindow.IsDrift)
        {
            _logger.LogInformation("ModelDriftAlertService: Tỷ lệ đánh giá 1-2 sao không vượt quá 20% trong 07 ngày liên tiếp. Trạng thái AI ổn định.");
            return false;
        }

        // Đã phát hiện suy thoái mô hình AI: Kiểm tra tính bất biến (Idempotent)
        // Nếu trong ngày hôm nay đã có bản ghi MODEL_DRIFT_ALERT thì không tạo trùng lặp
        var alreadyAlertedToday = await _db.AuditLogs.AnyAsync(a =>
            a.Action == "MODEL_DRIFT_ALERT" &&
            a.TargetTable == "Surveys" &&
            a.CreatedAt >= today && a.CreatedAt < latestDate, ct);

        if (!alreadyAlertedToday)
        {
            var auditLog = new AuditLog
            {
                ActorUserId = null,
                ActorRole = "SYSTEM",
                Action = "MODEL_DRIFT_ALERT",
                TargetTable = "Surveys",
                TargetId = "SYSTEM",
                NewValue = JsonSerializer.Serialize(new
                {
                    alert = true,
                    consecutiveDays = 7,
                    threshold = 0.20,
                    evaluatedAt = now.ToString("O"),
                    dailyStats = chosenWindow.Stats
                }),
                Note = "Cảnh báo suy thoái mô hình AI: Tỷ lệ đánh giá 1-2 sao vượt 20% trong 07 ngày liên tiếp (BR-ADM-AI-004).",
                CreatedAt = now
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("ModelDriftAlertService: CẢNH BÁO ADMIN - Tỷ lệ đánh giá 1-2 sao vượt quá 20% trong 07 ngày liên tiếp (BR-ADM-AI-004).");

            // Gửi email cảnh báo khẩn cấp tới tất cả Quản trị viên (ADMIN)
            try
            {
                var adminEmails = await _db.Users
                    .Where(u => u.Role == UserRoles.Admin && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync(ct);

                foreach (var adminEmail in adminEmails)
                {
                    await _emailService.SendNotificationEmailAsync(
                        adminEmail,
                        "[VeggieLife] CẢNH BÁO: Phát hiện suy thoái độ chính xác mô hình AI (Model Drift)",
                        "<h3>Cảnh báo chất lượng dịch vụ AI (BR-ADM-AI-004)</h3>" +
                        "<p>Hệ thống ghi nhận tỷ lệ người dùng đánh giá 1–2 sao vượt quá 20% trong 07 ngày liên tiếp.</p>" +
                        "<p>Vui lòng truy cập trang Quản trị Dashboard LlmOps (FN-ADM-16) để rà soát và tinh chỉnh prompt, cấu hình AI.</p>",
                        ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gửi email cảnh báo Model Drift tới Quản trị viên.");
            }
        }
        else
        {
            _logger.LogInformation("ModelDriftAlertService: Cảnh báo MODEL_DRIFT_ALERT đã được ghi nhận trong ngày (Idempotent).");
        }

        return true;
    }

    private static (bool IsDrift, List<object> Stats) Evaluate7DaysWindow(List<Survey> allSurveys, DateTime windowStartDate)
    {
        var stats = new List<object>();

        for (int i = 0; i < 7; i++)
        {
            var dayStart = windowStartDate.AddDays(i);
            var dayEnd = dayStart.AddDays(1);

            var surveysInDay = allSurveys
                .Where(s => s.CreatedAt >= dayStart && s.CreatedAt < dayEnd)
                .ToList();

            // Nếu một ngày trong chuỗi không có bất kỳ khảo sát nào -> không đủ điều kiện 7 ngày liên tiếp vượt ngưỡng
            if (surveysInDay.Count == 0)
            {
                return (false, stats);
            }

            var negativeCount = surveysInDay.Count(s => s.RatingScore <= 2);
            var negativeRate = (double)negativeCount / surveysInDay.Count;

            stats.Add(new
            {
                date = dayStart.ToString("yyyy-MM-dd"),
                total = surveysInDay.Count,
                negative = negativeCount,
                negativeRate = Math.Round(negativeRate, 4)
            });

            // Nếu ngày này tỷ lệ <= 20% (0.20) thì chuỗi bị ngắt
            if (negativeRate <= 0.20)
            {
                return (false, stats);
            }
        }

        return (true, stats);
    }
}
