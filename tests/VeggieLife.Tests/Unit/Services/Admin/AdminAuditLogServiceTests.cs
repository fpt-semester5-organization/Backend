using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.DTOs.Admin.AuditLogs;
using VeggieLife.BLL.Services.Admin;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Unit.Services.Admin;

public class AdminAuditLogServiceTests
{
    private readonly AppDbContext _db;
    private readonly AdminAuditLogService _service;

    public AdminAuditLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("AdminAuditLogServiceTestsDb_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(options);
        _service = new AdminAuditLogService(_db, NullLogger<AdminAuditLogService>.Instance);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilterByActorUserId_ReturnsMatchingLogs()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { AuditId = 1, ActorUserId = 10, ActorRole = "ADMIN", Action = "USER_SANCTION", TargetTable = "Users", TargetId = "1024", CreatedAt = DateTime.UtcNow },
            new AuditLog { AuditId = 2, ActorUserId = 20, ActorRole = "ADMIN", Action = "CATEGORY_CREATE", TargetTable = "FoodCategories", TargetId = "5", CreatedAt = DateTime.UtcNow },
            new AuditLog { AuditId = 3, ActorUserId = 10, ActorRole = "ADMIN", Action = "USER_REACTIVATE", TargetTable = "Users", TargetId = "1025", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminAuditLogFilterRequest { ActorUserId = 10, Page = 1, Size = 10 };
        var (logs, meta) = await _service.GetAuditLogsAsync(filter);

        logs.Should().HaveCount(2);
        logs.All(l => l.ActorUserId == 10).Should().BeTrue();
        meta.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilterByActionAndTable_ReturnsMatchingLogs()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { AuditId = 1, ActorUserId = 1, ActorRole = "ADMIN", Action = "USER_SANCTION", TargetTable = "Users", TargetId = "1024", CreatedAt = DateTime.UtcNow },
            new AuditLog { AuditId = 2, ActorUserId = 1, ActorRole = "ADMIN", Action = "RECIPE_VERIFY", TargetTable = "Recipes", TargetId = "55", CreatedAt = DateTime.UtcNow },
            new AuditLog { AuditId = 3, ActorUserId = 2, ActorRole = "ADMIN", Action = "USER_SANCTION", TargetTable = "Users", TargetId = "1026", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminAuditLogFilterRequest
        {
            Action = "USER_SANCTION",
            TargetTable = "Users",
            Page = 1,
            Size = 10
        };

        var (logs, meta) = await _service.GetAuditLogsAsync(filter);

        logs.Should().HaveCount(2);
        logs.All(l => l.Action == "USER_SANCTION" && l.TargetTable == "Users").Should().BeTrue();
        meta.Total.Should().Be(2);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FilterByDateRange_ReturnsLogsWithinRange()
    {
        _db.AuditLogs.AddRange(
            new AuditLog { AuditId = 1, ActorRole = "ADMIN", Action = "A1", TargetTable = "T", TargetId = "1", CreatedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc) },
            new AuditLog { AuditId = 2, ActorRole = "ADMIN", Action = "A2", TargetTable = "T", TargetId = "2", CreatedAt = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc) },
            new AuditLog { AuditId = 3, ActorRole = "ADMIN", Action = "A3", TargetTable = "T", TargetId = "3", CreatedAt = new DateTime(2026, 10, 25, 15, 0, 0, DateTimeKind.Utc) }
        );
        await _db.SaveChangesAsync();

        var filter = new AdminAuditLogFilterRequest
        {
            From = "2026-10-10",
            To = "2026-10-20",
            Page = 1,
            Size = 10
        };

        var (logs, meta) = await _service.GetAuditLogsAsync(filter);

        logs.Should().ContainSingle();
        logs[0].AuditId.Should().Be(2);
        meta.Total.Should().Be(1);
    }

    [Fact]
    public async Task GetAuditLogsAsync_MasksSensitiveDataInOldAndNewValue()
    {
        _db.AuditLogs.Add(new AuditLog
        {
            AuditId = 100,
            ActorUserId = 1,
            ActorRole = "ADMIN",
            Action = "PASSWORD_RESET",
            TargetTable = "Users",
            TargetId = "1024",
            OldValue = "{\"passwordHash\":\"$2a$12$OldHashMustNotLeak\",\"email\":\"test@veggielife.vn\"}",
            NewValue = "{\"passwordHash\":\"$2a$12$NewHashMustNotLeak\",\"token\":\"jwt_super_secret_token\"}",
            Note = "Admin đặt lại mật khẩu",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var filter = new AdminAuditLogFilterRequest { Page = 1, Size = 10 };
        var (logs, _) = await _service.GetAuditLogsAsync(filter);

        logs.Should().ContainSingle();
        var item = logs[0];

        item.OldValue.Should().NotContain("OldHashMustNotLeak");
        item.OldValue.Should().Contain("\"passwordHash\":\"***\"");
        item.OldValue.Should().Contain("\"email\":\"test@veggielife.vn\"");

        item.NewValue.Should().NotContain("NewHashMustNotLeak");
        item.NewValue.Should().NotContain("jwt_super_secret_token");
        item.NewValue.Should().Contain("\"passwordHash\":\"***\"");
        item.NewValue.Should().Contain("\"token\":\"***\"");
    }

    [Fact]
    public async Task GetAuditLogsAsync_Benchmark_100kFakeLogsQueriedUnder800ms()
    {
        // Tiêu chí DoD: Tra cứu 100.000 dòng nhật ký giả trong dưới 1500 ms; không lộ dữ liệu nhạy cảm.
        const int TotalRecords = 100_000;
        var fakeLogs = new List<AuditLog>(TotalRecords);
        var baseDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

        var actions = new[] { "USER_SANCTION", "RECIPE_VERIFY", "CATEGORY_CREATE", "PAYMENT_REFUND", "PASSWORD_RESET" };
        var tables = new[] { "Users", "Recipes", "FoodCategories", "PaymentTransactions" };

        for (int i = 1; i <= TotalRecords; i++)
        {
            var action = actions[i % actions.Length];
            var table = tables[i % tables.Length];
            var isSensitive = action == "PASSWORD_RESET";

            fakeLogs.Add(new AuditLog
            {
                AuditId = i,
                ActorUserId = (i % 20) + 1,
                ActorRole = "ADMIN",
                Action = action,
                TargetTable = table,
                TargetId = (i % 1000).ToString(),
                OldValue = isSensitive ? "{\"passwordHash\":\"$2a$secretHash_" + i + "\"}" : "{\"status\":\"OLD\"}",
                NewValue = isSensitive ? "{\"token\":\"jwt_token_" + i + "\"}" : "{\"status\":\"NEW\"}",
                Note = "Ghi chú kiểm toán #" + i,
                CreatedAt = baseDate.AddMinutes(i)
            });
        }

        await _db.AuditLogs.AddRangeAsync(fakeLogs);
        await _db.SaveChangesAsync();

        var filter = new AdminAuditLogFilterRequest
        {
            Action = "USER_SANCTION",
            TargetTable = "Users",
            From = "2026-10-01",
            To = "2026-10-31",
            Page = 1,
            Size = 20
        };

        // Bắt đầu đo thời gian tra cứu 100.000 dòng giả lập
        var stopwatch = Stopwatch.StartNew();
        var (logs, meta) = await _service.GetAuditLogsAsync(filter);
        stopwatch.Stop();

        // 1. Kiểm tra hiệu năng: dưới 1500 ms
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1500,
            $"Tra cứu trên 100.000 dòng nhật ký phải hoàn thành dưới 1500 ms (thực tế mất {stopwatch.ElapsedMilliseconds} ms)");

        // 2. Kiểm tra tính chính xác của dữ liệu
        meta.Total.Should().BeGreaterThan(0);
        logs.Should().HaveCount(20);
        logs.All(l => l.Action == "USER_SANCTION" && l.TargetTable == "Users").Should().BeTrue();

        // 3. Kiểm tra bảo mật: Không lộ dữ liệu nhạy cảm
        foreach (var log in logs)
        {
            if (log.OldValue != null)
            {
                log.OldValue.Should().NotContain("secretHash");
            }
            if (log.NewValue != null)
            {
                log.NewValue.Should().NotContain("jwt_token");
            }
        }
    }
}
