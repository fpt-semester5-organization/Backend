using Microsoft.EntityFrameworkCore;
using VeggieLife.API.Extensions;
using VeggieLife.API.Middlewares;
using VeggieLife.DAL.Context;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Database Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình Controllers và JSON Serialization quy chuẩn camelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 3. Cấu hình Swagger UI kèm Bearer Token Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

// 4. Cấu hình Xác thực JWT Bearer & Phân quyền Roles (GUEST, MEMBER, VIP, ADMIN)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();

// 5. Cấu hình Redis Cache
builder.Services.AddRedisCaching(builder.Configuration);

// 6. Đăng ký các dịch vụ cốt lõi và các interface dùng chung (AI, Vector DB, Email, Media, Payment, Auth)
builder.Services.AddApplicationServices();

// 7. Cấu hình Health Checks
builder.Services.AddAppHealthChecks();

var app = builder.Build();

// 8. Pipeline: Xử lý ngoại lệ tập trung toàn cục (Global Exception Handling)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 9. Pipeline: Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VeggieLife API v1");
        c.RoutePrefix = "swagger";
    });
}

// 10. Pipeline: Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 11. Pipeline: Endpoint kiểm tra sức khỏe hệ thống /health
app.MapCustomHealthCheck();

// 12. Pipeline: Map Controllers
app.MapControllers();

app.Run();
