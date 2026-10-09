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

// 3. Đăng ký các dịch vụ nghiệp vụ BLL, DAL & Cloudinary CDN
builder.Services.AddApplicationServices(builder.Configuration);

// 4. Cấu hình JWT Bearer & Phân quyền Role
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();

// 5. Cấu hình Controllers & Health Checks
builder.Services.AddControllers();
builder.Services.AddAppHealthChecks();

// 6. Cấu hình Swagger UI kèm Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

// 7. Middleware xử lý ngoại lệ tập trung toàn cục (Chuẩn D52 Envelope)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 8. Swagger UI phục vụ kiểm thử
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

// 9. Xác thực & Phân quyền
app.UseAuthentication();
app.UseAuthorization();

// 10. Map Controllers & Custom Health Checks
app.MapControllers();
app.MapCustomHealthCheck();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
