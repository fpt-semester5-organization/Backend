using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.DTOs.Admin.Subscriptions;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.Entities;
using Xunit;

namespace VeggieLife.Tests.Integration.Controllers;

/// <summary>
/// Bộ kiểm thử tích hợp (Integration Tests) gọi qua HTTP pipeline cho các endpoint quản trị gói VIP và đối soát giao dịch (FN-ADM-13).
/// </summary>
public class AdminSubscriptionsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IRedisService> _mockRedis = new();
    private readonly string _testDbName = "VeggieLifeAdminSubscriptionsIntegrationDb_" + Guid.NewGuid();

    public AdminSubscriptionsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_testDbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisService));
                if (redisDescriptor != null)
                {
                    services.Remove(redisDescriptor);
                }
                services.AddSingleton(_mockRedis.Object);
            });
        });

        SeedTestData();
    }

    private void SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Users
        var admin = new User
        {
            UserId = 1,
            Email = "admin@veggielife.vn",
            FullName = "Admin Quản Trị",
            Role = UserRoles.Admin,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        var member = new User
        {
            UserId = 2,
            Email = "member@veggielife.vn",
            FullName = "Hội Viên 2",
            Role = UserRoles.Member,
            AccountStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.AddRange(admin, member);

        // 2. Plans
        var plan1 = new SubscriptionPlan
        {
            PlanId = 1,
            AdminId = 1,
            PlanName = "VIP 1 tháng",
            DurationDays = 30,
            Price = 49000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.SubscriptionPlans.Add(plan1);

        // 3. Transactions
        var txn1 = new PaymentTransaction
        {
            TransactionId = 3001,
            PlanId = 1,
            UserId = 2,
            GatewayProvider = "MOMO",
            PartnerTransactionCode = "VL_20261022_3001",
            AmountVnd = 49000,
            TransactionStatus = "PENDING",
            SignatureHash = "sig_3001",
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            StartsAt = DateTime.UtcNow,
            EndsAt = DateTime.UtcNow.AddDays(30)
        };
        db.PaymentTransactions.Add(txn1);

        db.SaveChanges();
    }

    private string GenerateJwtToken(int userId, string role)
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secretKey = config["Jwt:SecretKey"]
            ?? config["JwtSettings:SecretKey"]
            ?? "veggielife-development-super-secret-jwt-key-32-chars-long";
        var issuer = config["Jwt:Issuer"] ?? config["JwtSettings:Issuer"] ?? "VeggieLife.API";
        var audience = config["Jwt:Audience"] ?? config["JwtSettings:Audience"] ?? "VeggieLife.Client";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, $"user{userId}@veggielife.vn")
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // ==========================================
    // 1. GET /api/v1/admin/subscription-plans
    // ==========================================

    [Fact]
    public async Task GetSubscriptionPlans_WithAdminToken_Returns200OK()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminSubscriptionPlanItemDto>>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("OK");
        envelope.Data.Should().NotBeEmpty();
        envelope.Data.Should().Contain(p => p.PlanId == 1 && p.PlanName == "VIP 1 tháng");
    }

    [Fact]
    public async Task GetSubscriptionPlans_WithMemberToken_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(2, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAdmForbidden);
    }

    [Fact]
    public async Task GetSubscriptionPlans_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAuthInvalidToken);
    }

    // ==========================================
    // 2. POST /api/v1/admin/subscription-plans
    // ==========================================

    [Fact]
    public async Task CreateSubscriptionPlan_WithAdminToken_Returns201Created()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "VIP 6 tháng đặc biệt",
            DurationDays = 180,
            PriceVnd = 249000
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/subscription-plans", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<CreateAdminSubscriptionPlanResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(201);
        envelope.Message.Should().Be("Đã tạo gói.");
        envelope.Data!.PlanId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSubscriptionPlan_DuplicateName_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "VIP 1 tháng", // đã tồn tại
            DurationDays = 30,
            PriceVnd = 50000
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/subscription-plans", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPlanDuplicate);
    }

    [Fact]
    public async Task CreateSubscriptionPlan_InvalidPrice_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateAdminSubscriptionPlanRequest
        {
            PlanName = "VIP Hợp Lệ",
            DurationDays = 30,
            PriceVnd = 0 // <= 0
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/subscription-plans", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // ==========================================
    // 3. PUT /api/v1/admin/subscription-plans/{id}
    // ==========================================

    [Fact]
    public async Task UpdateSubscriptionPlan_WithAdminToken_Returns200OK()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateAdminSubscriptionPlanRequest
        {
            PriceVnd = 45000
        };

        var response = await client.PutAsJsonAsync("/api/v1/admin/subscription-plans/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<UpdateAdminSubscriptionPlanResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã cập nhật gói.");
        envelope.Data!.PlanId.Should().Be(1);
        envelope.Data!.PriceVnd.Should().Be(45000);
    }

    [Fact]
    public async Task UpdateSubscriptionPlan_NonExistent_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateAdminSubscriptionPlanRequest { PriceVnd = 50000 };

        var response = await client.PutAsJsonAsync("/api/v1/admin/subscription-plans/9999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    // ==========================================
    // 4. POST /api/v1/admin/payments/{id}/reconcile
    // ==========================================

    [Fact]
    public async Task ReconcilePayment_Compensate_Returns200OK_And_SecondReconcile_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Đã trừ tiền MoMo nhưng chưa kích hoạt VIP."
        };

        // Lần 1: Thành công 200 OK
        var response1 = await client.PostAsJsonAsync("/api/v1/admin/payments/3001/reconcile", request);

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await response1.Content.ReadFromJsonAsync<ApiResponse<ReconcileAdminPaymentResponseData>>();
        envelope.Should().NotBeNull();
        envelope!.StatusCode.Should().Be(200);
        envelope.Message.Should().Be("Đã đối soát.");
        envelope.Data!.TransactionId.Should().Be(3001);
        envelope.Data!.Status.Should().Be("SUCCESS");

        // Lần 2: Bị từ chối 409 Conflict với ERR_PAY_ALREADY_RECONCILED (Điều kiện hoàn thành của Task)
        var response2 = await client.PostAsJsonAsync("/api/v1/admin/payments/3001/reconcile", request);

        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response2.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPayAlreadyReconciled);
        error.Message.Should().Be("Giao dịch đã được đối soát.");
    }

    [Fact]
    public async Task ReconcilePayment_NonExistent_Returns404NotFound()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ReconcileAdminPaymentRequest
        {
            Action = "REFUND",
            Reason = "Hoàn tiền giao dịch không tồn tại."
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/payments/99999/reconcile", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrAdmNotFound);
    }

    [Fact]
    public async Task ReconcilePayment_InvalidReasonLength_Returns400BadRequest()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new ReconcileAdminPaymentRequest
        {
            Action = "COMPENSATE",
            Reason = "Ngắn" // < 10 ký tự
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/payments/3001/reconcile", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrValidation);
    }

    // ==========================================
    // 5. GET /api/v1/admin/payments
    // ==========================================

    [Fact]
    public async Task ListPayments_AsAdmin_Returns200OKWithPagedEnvelope()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/payments?page=1&size=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<List<AdminPaymentTransactionItemDto>>>();
        content.Should().NotBeNull();
        content!.StatusCode.Should().Be(200);
        content.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ListPayments_AsNonAdmin_Returns403Forbidden()
    {
        var client = _factory.CreateClient();
        var token = GenerateJwtToken(2, UserRoles.Member);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/admin/payments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==========================================
    // 6. POST /api/v1/admin/payments/{id}/approve
    // ==========================================

    [Fact]
    public async Task ApprovePayment_AsAdmin_PendingTransaction_Returns200OKAndActivatesVip()
    {
        // Seed new pending transaction 3002
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.PaymentTransactions.AnyAsync(pt => pt.TransactionId == 3002))
            {
                db.PaymentTransactions.Add(new PaymentTransaction
                {
                    TransactionId = 3002,
                    PlanId = 1,
                    UserId = 2,
                    GatewayProvider = "VIETQR",
                    PartnerTransactionCode = "VL_20261022_3002",
                    AmountVnd = 49000,
                    TransactionStatus = "PENDING",
                    SignatureHash = "sig_3002",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    StartsAt = DateTime.UtcNow,
                    EndsAt = DateTime.UtcNow.AddDays(30)
                });
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/v1/admin/payments/3002/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<ApproveAdminPaymentResponseData>>();
        content.Should().NotBeNull();
        content!.Data!.Status.Should().Be("SUCCESS");

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var txn = await db.PaymentTransactions.FindAsync(3002L);
            txn!.TransactionStatus.Should().Be("SUCCESS");

            var user = await db.Users.FindAsync(2);
            user!.Role.Should().Be(UserRoles.Vip);
        }
    }

    [Fact]
    public async Task ApprovePayment_AlreadySuccess_Returns409Conflict()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.PaymentTransactions.AnyAsync(pt => pt.TransactionId == 3003))
            {
                db.PaymentTransactions.Add(new PaymentTransaction
                {
                    TransactionId = 3003,
                    PlanId = 1,
                    UserId = 2,
                    GatewayProvider = "VIETQR",
                    PartnerTransactionCode = "VL_20261022_3003",
                    AmountVnd = 49000,
                    TransactionStatus = "SUCCESS",
                    SignatureHash = "sig_3003",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                    StartsAt = DateTime.UtcNow,
                    EndsAt = DateTime.UtcNow.AddDays(30)
                });
                await db.SaveChangesAsync();
            }
        }

        var client = _factory.CreateClient();
        var token = GenerateJwtToken(1, UserRoles.Admin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync("/api/v1/admin/payments/3003/approve", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(ErrorCodes.ErrPayOrderAlreadySuccess);
    }
}
