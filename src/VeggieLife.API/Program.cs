using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeggieLife.API.Extensions;
using VeggieLife.API.Middlewares;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Services.Products;
using VeggieLife.DAL.Context;
using VeggieLife.DAL.External.OpenFoodFacts;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server hoặc In-Memory (khi Database:UseInMemory = true hoặc chưa cấu hình SQL Server thật)
var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory || string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_PASSWORD"))
    {
        options.UseInMemoryDatabase("VeggieLifeDb");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// 2. Cấu hình bộ nhớ đệm phân tán Redis
builder.Services.AddRedisCaching(builder.Configuration);

// 3. Đăng ký các dịch vụ nghiệp vụ BLL, DAL, Cloudinary CDN & Auth
builder.Services.AddApplicationServices(builder.Configuration);

// 4. Cấu hình Xác thực JWT Bearer & Phân quyền Roles (GUEST, MEMBER, VIP, ADMIN)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();

// 5. Cấu hình Routing chữ thường chuẩn RESTful (ví dụ: /api/v1/auth/login thay vì /api/v1/Auth/login)
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// 6. Cấu hình Controllers với JSON Serialization quy chuẩn camelCase & chuẩn hóa lỗi Validation
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(msg => !string.IsNullOrEmpty(msg))
                ?? "Dữ liệu đầu vào sai định dạng hoặc thiếu trường bắt buộc.";

            var errorResponse = new ApiErrorResponse(
                StatusCodes.Status400BadRequest,
                ErrorCodes.ErrValidation,
                firstError);

            return new BadRequestObjectResult(errorResponse);
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 6. Cấu hình Health Checks
builder.Services.AddAppHealthChecks();

// 7. Cấu hình Swagger UI kèm Bearer Token Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

// Tự động nạp dữ liệu mẫu khi chạy chế độ In-Memory Database cục bộ (không chạy trong Test Runner)
if ((useInMemory || app.Environment.IsDevelopment()) && !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.Contains("xunit") == true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName?.Contains("InMemory") == true)
    {
        VeggieLife.API.Extensions.DevDataSeeder.SeedIfEmpty(db);
        VeggieLife.API.Extensions.DevInMemoryDataSeeder.Seed(db);
    }
}

app.UseCors();

// 8. Pipeline: Xử lý ngoại lệ tập trung toàn cục (Global Exception Handling)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 9. Pipeline: Swagger UI phục vụ kiểm thử
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VeggieLife API v1");
        c.DocumentTitle = "VeggieLife Swagger UI";
        c.RoutePrefix = "swagger";
    });

    // Tự động chuyển hướng từ "/" sang "/swagger" để tiện kiểm thử
    app.MapGet("/", () => Results.Redirect("/swagger"));
}
else
{
    app.UseHttpsRedirection();
}

// 10. Pipeline: Authentication & Authorization
app.UseAuthentication();
app.UseMiddleware<GuestSecurityMiddleware>();
app.UseAuthorization();

// 11. Pipeline: Map Controllers & Custom Health Checks
app.MapControllers();
app.MapCustomHealthCheck();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
