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

// 1. Cáº¥u hÃ¬nh DbContext káº¿t ná»‘i SQL Server (Báº¯t buá»™c dÃ¹ng SQL Server Ä‘á»ƒ lÆ°u bá»n vá»¯ng vÃ o CSDL, khÃ´ng lÆ°u trÃªn RAM)
var useInMemory = builder.Configuration.GetValue<bool>("Database:UseInMemory");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_PASSWORD"))
{
    connectionString = "Server=localhost;Database=VeggieLifeDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("VeggieLifeDb");
    }
    else
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
    }
});

// 2. Cáº¥u hÃ¬nh bá»™ nhá»› Ä‘á»‡m phÃ¢n tÃ¡n Redis
builder.Services.AddRedisCaching(builder.Configuration);

// 3. ÄÄƒng kÃ½ cÃ¡c dá»‹ch vá»¥ nghiá»‡p vá»¥ BLL, DAL, Cloudinary CDN & Auth
builder.Services.AddApplicationServices(builder.Configuration);

// 4. Cáº¥u hÃ¬nh XÃ¡c thá»±c JWT Bearer & PhÃ¢n quyá»n Roles (GUEST, MEMBER, VIP, ADMIN)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRoleAuthorization();

// 5. Cáº¥u hÃ¬nh Routing chá»¯ thÆ°á»ng chuáº©n RESTful (vÃ­ dá»¥: /api/v1/auth/login thay vÃ¬ /api/v1/Auth/login)
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// 6. Cáº¥u hÃ¬nh Controllers vá»›i JSON Serialization quy chuáº©n camelCase & chuáº©n hÃ³a lá»—i Validation
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
                ?? "Dá»¯ liá»‡u Ä‘áº§u vÃ o sai Ä‘á»‹nh dáº¡ng hoáº·c thiáº¿u trÆ°á»ng báº¯t buá»™c.";

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
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
        options.JsonSerializerOptions.Converters.Add(new VeggieLife.API.Extensions.UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new VeggieLife.API.Extensions.NullableUtcDateTimeConverter());
    });

// 6. Cáº¥u hÃ¬nh Health Checks
builder.Services.AddAppHealthChecks();

// 7. Cáº¥u hÃ¬nh Swagger UI kÃ¨m Bearer Token Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithAuth();

var app = builder.Build();

// Tá»± Ä‘á»™ng kiá»ƒm tra vÃ  khá»Ÿi táº¡o Database / Seed dá»¯ liá»‡u náº¿u lÃ  database má»›i (khÃ´ng cháº¡y trong Test Runner)
if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.Contains("xunit") == true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (db.Database.ProviderName?.Contains("SqlServer") == true)
    {
        try
        {
            // Tá»± Ä‘á»™ng táº¡o CSDL VeggieLifeDb vÃ  38 báº£ng náº¿u mÃ¡y má»›i chÆ°a cÃ³
            db.Database.EnsureCreated();
            VeggieLife.API.Extensions.DevDataSeeder.SeedIfEmpty(db);
            logger.LogInformation("CSDL SQL Server (VeggieLifeDb) Ä‘Ã£ káº¿t ná»‘i thÃ nh cÃ´ng vÃ  sáºµn sÃ ng lÆ°u trá»¯ bá»n vá»¯ng.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lá»–I Káº¾T Ná»I SQL SERVER: KhÃ´ng thá»ƒ káº¿t ná»‘i tá»›i SQL Server ({ConnectionString}). Vui lÃ²ng Ä‘áº£m báº£o dá»‹ch vá»¥ SQL Server (MSSQLSERVER/SQLEXPRESS) Ä‘ang cháº¡y trÃªn mÃ¡y.", connectionString);
            throw new InvalidOperationException($"[Lá»–I CSDL]: KhÃ´ng thá»ƒ káº¿t ná»‘i tá»›i SQL Server táº¡i '{connectionString}'. Vui lÃ²ng báº­t dá»‹ch vá»¥ SQL Server trÃªn mÃ¡y Ä‘á»ƒ lÆ°u dá»¯ liá»‡u bá»n vá»¯ng, khÃ´ng lÆ°u trÃªn RAM.", ex);
        }
    }
    else if (db.Database.ProviderName?.Contains("InMemory") == true)
    {
        logger.LogWarning("Cáº¢NH BÃO: á»¨ng dá»¥ng Ä‘ang cháº¡y á»Ÿ cháº¿ Ä‘á»™ IN-MEMORY DATABASE (lÆ°u trÃªn RAM). Má»i dá»¯ liá»‡u sáº½ bá»‹ máº¥t khi táº¯t á»©ng dá»¥ng!");
        VeggieLife.API.Extensions.DevDataSeeder.SeedIfEmpty(db);
        VeggieLife.API.Extensions.DevInMemoryDataSeeder.Seed(db);
    }
}

app.UseCors();

// 8. Pipeline: Xá»­ lÃ½ ngoáº¡i lá»‡ táº­p trung toÃ n cá»¥c (Global Exception Handling)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 9. Pipeline: Swagger UI phá»¥c vá»¥ kiá»ƒm thá»­
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VeggieLife API v1");
        c.DocumentTitle = "VeggieLife Swagger UI";
        c.RoutePrefix = "swagger";
    });

    // Tá»± Ä‘á»™ng chuyá»ƒn hÆ°á»›ng tá»« "/" sang "/swagger" Ä‘á»ƒ tiá»‡n kiá»ƒm thá»­ (áº©n khá»i Swagger contract)
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    app.UseHttpsRedirection();
}

// 10. Pipeline: Authentication & Authorization
app.UseAuthentication();
app.UseMiddleware<UserBlacklistMiddleware>();
app.UseMiddleware<GuestSecurityMiddleware>();
app.UseAuthorization();

// 11. Pipeline: Map Controllers & Custom Health Checks
app.MapControllers();
app.MapHub<VeggieLife.API.Hubs.UserHub>("/hubs/user");
app.MapHub<VeggieLife.API.Hubs.UserHub>("/hubs/auth");
app.MapCustomHealthCheck();

app.Run();

// Há»— trá»£ WebApplicationFactory trong Integration Tests
public partial class Program { }
