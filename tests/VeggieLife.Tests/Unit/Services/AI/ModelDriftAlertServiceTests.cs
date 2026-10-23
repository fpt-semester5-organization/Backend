using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Email;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using VeggieLife.Tests.Helpers;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.AI;

/// <summary>
/// Unit Tests cho ModelDriftAlertService (FN-CHAT-06, BR-ADM-AI-004).
/// Kiểm thử với đồng hồ giả (FakeTimeProvider): đánh giá 1-2 sao vượt 20% trong 7 ngày liên tiếp; chạy lại 2 lần không đổi kết quả.
/// </summary>
public class ModelDriftAlertServiceTests
{
    private readonly AppDbContext _db;
    private readonly Mock<IEmailService> _mockEmail;
    private readonly FakeTimeProvider _fakeClock;
    private readonly ModelDriftAlertService _service;

    public ModelDriftAlertServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "ModelDriftTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _mockEmail = new Mock<IEmailService>();
        _fakeClock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 24, 1, 0, 0, TimeSpan.Zero));
        _service = new ModelDriftAlertService(_db, _mockEmail.Object, NullLogger<ModelDriftAlertService>.Instance, _fakeClock);
    }

    [Fact]
    public async Task Check_WhenNoSurveys_ReturnsFalseAndNoAlert()
    {
        // ARRANGE: Không có khảo sát nào
        // ACT
        var isAlert = await _service.CheckAndAlertModelDriftAsync();

        // ASSERT
        isAlert.Should().BeFalse();
        (await _db.AuditLogs.AnyAsync(a => a.Action == "MODEL_DRIFT_ALERT")).Should().BeFalse();
    }

    [Fact]
    public async Task Check_WhenNegativeRateUnder20Percent_ReturnsFalseAndNoAlert()
    {
        // ARRANGE: 7 ngày liên tiếp, mỗi ngày có 10 khảo sát nhưng chỉ có 1 đánh giá 1 sao (10% <= 20%)
        var baseDate = _fakeClock.GetUtcNow().UtcDateTime.Date.AddDays(-7);

        for (int day = 0; day < 7; day++)
        {
            var date = baseDate.AddDays(day).AddHours(12);

            // 1 đánh giá tiêu cực (1 sao)
            _db.Surveys.Add(new Survey
            {
                UserId = 1,
                RatingScore = 1,
                ContextQuestion = "AI có tư vấn tốt không?",
                CreatedAt = date
            });

            // 9 đánh giá tích cực (5 sao)
            for (int j = 0; j < 9; j++)
            {
                _db.Surveys.Add(new Survey
                {
                    UserId = 1,
                    RatingScore = 5,
                    ContextQuestion = "AI có tư vấn tốt không?",
                    CreatedAt = date.AddMinutes(j + 1)
                });
            }
        }
        await _db.SaveChangesAsync();

        // ACT
        var isAlert = await _service.CheckAndAlertModelDriftAsync();

        // ASSERT
        isAlert.Should().BeFalse();
        (await _db.AuditLogs.AnyAsync(a => a.Action == "MODEL_DRIFT_ALERT")).Should().BeFalse();
    }

    [Fact]
    public async Task Check_WhenOneDayBreaksTheChain_ReturnsFalseAndNoAlert()
    {
        // ARRANGE: 6 ngày có tỷ lệ 1-2 sao là 30% (>20%), nhưng ngày thứ 4 chỉ có 10%
        var baseDate = _fakeClock.GetUtcNow().UtcDateTime.Date.AddDays(-7);

        for (int day = 0; day < 7; day++)
        {
            var date = baseDate.AddDays(day).AddHours(10);
            var negativeCount = (day == 3) ? 1 : 3; // Ngày thứ 4 (day 3) chỉ có 1/10 = 10%

            for (int n = 0; n < negativeCount; n++)
            {
                _db.Surveys.Add(new Survey
                {
                    UserId = 1,
                    RatingScore = 2,
                    ContextQuestion = "Đánh giá AI",
                    CreatedAt = date.AddMinutes(n)
                });
            }

            for (int p = 0; p < (10 - negativeCount); p++)
            {
                _db.Surveys.Add(new Survey
                {
                    UserId = 1,
                    RatingScore = 4,
                    ContextQuestion = "Đánh giá AI",
                    CreatedAt = date.AddMinutes(negativeCount + p)
                });
            }
        }
        await _db.SaveChangesAsync();

        // ACT
        var isAlert = await _service.CheckAndAlertModelDriftAsync();

        // ASSERT: Chuỗi 7 ngày liên tiếp bị đứt quãng tại ngày thứ 4 -> Không phát cảnh báo
        isAlert.Should().BeFalse();
        (await _db.AuditLogs.AnyAsync(a => a.Action == "MODEL_DRIFT_ALERT")).Should().BeFalse();
    }

    [Fact]
    public async Task Check_WhenAll7ConsecutiveDaysExceed20Percent_AlertsAdminAndInsertsAuditLog()
    {
        // ARRANGE: Tạo Admin để nhận email
        _db.Users.Add(new User
        {
            UserId = 99,
            Email = "admin@veggielife.vn",
            FullName = "Admin VeggieLife",
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });

        // 7 ngày liên tiếp, mỗi ngày có 10 khảo sát và 3 đánh giá tiêu cực (30% > 20%)
        var baseDate = _fakeClock.GetUtcNow().UtcDateTime.Date.AddDays(-7);

        for (int day = 0; day < 7; day++)
        {
            var date = baseDate.AddDays(day).AddHours(14);

            // 3 đánh giá 1-2 sao (30%)
            _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 1, ContextQuestion = "Hỏi calo", CreatedAt = date });
            _db.Surveys.Add(new Survey { UserId = 2, RatingScore = 2, ContextQuestion = "Hỏi calo", CreatedAt = date.AddMinutes(1) });
            _db.Surveys.Add(new Survey { UserId = 3, RatingScore = 2, ContextQuestion = "Hỏi calo", CreatedAt = date.AddMinutes(2) });

            // 7 đánh giá 4-5 sao
            for (int p = 0; p < 7; p++)
            {
                _db.Surveys.Add(new Survey { UserId = 4, RatingScore = 5, ContextQuestion = "Hỏi calo", CreatedAt = date.AddMinutes(3 + p) });
            }
        }
        await _db.SaveChangesAsync();

        // ACT
        var isAlert = await _service.CheckAndAlertModelDriftAsync();

        // ASSERT
        isAlert.Should().BeTrue();

        // Kiểm tra AuditLog hệ thống đã được ghi nhận
        var audit = await _db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "MODEL_DRIFT_ALERT" && a.TargetTable == "Surveys");
        audit.Should().NotBeNull();
        audit!.ActorRole.Should().Be("SYSTEM");
        audit.TargetId.Should().Be("SYSTEM");

        // Kiểm tra email cảnh báo đã gửi tới Admin
        _mockEmail.Verify(e => e.SendNotificationEmailAsync(
            "admin@veggielife.vn",
            It.Is<string>(s => s.Contains("Model Drift")),
            It.Is<string>(body => body.Contains("1–2 sao")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Check_IsIdempotent_RunningTwiceInSameDayDoesNotDuplicateAuditLog()
    {
        // ARRANGE: Tạo Admin và dữ liệu 7 ngày liên tiếp > 20%
        _db.Users.Add(new User
        {
            UserId = 98,
            Email = "admin2@veggielife.vn",
            FullName = "Admin Hai",
            Role = UserRoles.Admin,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow
        });

        var baseDate = _fakeClock.GetUtcNow().UtcDateTime.Date.AddDays(-7);
        for (int day = 0; day < 7; day++)
        {
            var date = baseDate.AddDays(day).AddHours(11);
            // 4 tiêu cực / 10 = 40%
            for (int i = 0; i < 4; i++)
            {
                _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 1, ContextQuestion = "AI", CreatedAt = date.AddMinutes(i) });
            }
            for (int i = 0; i < 6; i++)
            {
                _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 5, ContextQuestion = "AI", CreatedAt = date.AddMinutes(4 + i) });
            }
        }
        await _db.SaveChangesAsync();

        // ACT 1: Chạy lần thứ nhất
        var firstResult = await _service.CheckAndAlertModelDriftAsync();
        firstResult.Should().BeTrue();

        // ACT 2: Chạy lần thứ hai ngay sau đó trong cùng một ngày
        var secondResult = await _service.CheckAndAlertModelDriftAsync();
        secondResult.Should().BeTrue();

        // ASSERT: Chỉ có đúng 1 bản ghi AuditLog MODEL_DRIFT_ALERT duy nhất
        var auditLogs = await _db.AuditLogs
            .Where(a => a.Action == "MODEL_DRIFT_ALERT" && a.TargetTable == "Surveys")
            .ToListAsync();

        auditLogs.Should().HaveCount(1);
    }

    [Fact]
    public async Task Check_WithFakeClock_AdvancesAcrossDaysAndDetectsDrift()
    {
        // ARRANGE: Ban đầu ngày 2026-10-18: chỉ mới có 5 ngày dữ liệu >20%
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 22, 1, 0, 0, TimeSpan.Zero)); // 2026-10-22

        var start = new DateTime(2026, 10, 17, 10, 0, 0, DateTimeKind.Utc);
        for (int day = 0; day < 5; day++)
        {
            var date = start.AddDays(day);
            _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 1, ContextQuestion = "Q", CreatedAt = date });
            _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 5, CreatedAt = date.AddMinutes(1), ContextQuestion = "Q" });
            _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 5, CreatedAt = date.AddMinutes(2), ContextQuestion = "Q" });
        }
        await _db.SaveChangesAsync();

        // Mới 5 ngày -> chưa đủ 7 ngày liên tiếp
        var alertDay5 = await _service.CheckAndAlertModelDriftAsync();
        alertDay5.Should().BeFalse();

        // Thêm 2 ngày tiếp theo (ngày 6 và ngày 7) cũng > 20%
        var day6 = start.AddDays(5);
        _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 2, ContextQuestion = "Q", CreatedAt = day6 });
        _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 5, ContextQuestion = "Q", CreatedAt = day6.AddMinutes(1) });

        var day7 = start.AddDays(6);
        _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 2, ContextQuestion = "Q", CreatedAt = day7 });
        _db.Surveys.Add(new Survey { UserId = 1, RatingScore = 5, ContextQuestion = "Q", CreatedAt = day7.AddMinutes(1) });
        await _db.SaveChangesAsync();

        // Tua đồng hồ giả tới ngày 2026-10-24 01:00:00 (sau khi kết thúc ngày 7)
        _fakeClock.SetUtcNow(new DateTimeOffset(2026, 10, 24, 1, 0, 0, TimeSpan.Zero));

        // ACT
        var alertDay7 = await _service.CheckAndAlertModelDriftAsync();

        // ASSERT: Lúc này đã đủ 7 ngày liên tiếp > 20% -> Kích hoạt cảnh báo
        alertDay7.Should().BeTrue();
        (await _db.AuditLogs.AnyAsync(a => a.Action == "MODEL_DRIFT_ALERT")).Should().BeTrue();
    }
}
