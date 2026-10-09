using Microsoft.EntityFrameworkCore;
using VeggieLife.API.Extensions;
using VeggieLife.API.Middlewares;
using VeggieLife.DAL.Context;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình bộ nhớ đệm phân tán Redis
builder.Services.AddRedisCaching(builder.Configuration);

// 3. Đăng ký các dịch vụ nghiệp vụ BLL, DAL, Cloudinary CDN & Auth
builder.Services.AddApplicationServices(builder.Configuration);

// 4. Cấu hình Xác thực JWT Bearer & Phân quyền Roles (GUEST, MEMBER, VIP, ADMIN)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();

// 5. Cấu hình Controllers với JSON Serialization quy chuẩn camelCase
builder.Services.AddControllers()
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
