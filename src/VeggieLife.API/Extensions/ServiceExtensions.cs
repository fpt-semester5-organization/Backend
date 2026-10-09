using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Responses;
using VeggieLife.BLL.Services.AI;
using VeggieLife.BLL.Services.Auth;
using VeggieLife.BLL.Services.Cache;
using VeggieLife.BLL.Services.Email;
using VeggieLife.BLL.Services.MasterData;
using VeggieLife.BLL.Services.Media;
using VeggieLife.BLL.Services.Payment;
using VeggieLife.BLL.Services.Profile;
using VeggieLife.DAL.External.Cloudinary;

namespace VeggieLife.API.Extensions;

/// <summary>
/// Các phương thức mở rộng cấu hình DI container cho tầng API.
/// </summary>
public static class ServiceExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Cấu hình xác thực người dùng bằng JWT Bearer Token.
    /// Bắt các sự kiện OnChallenge (401) và OnForbidden (403) để trả về ApiErrorResponse chuẩn.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("Cấu hình 'Jwt:SecretKey' chưa được thiết lập trong appsettings.json.");
        var issuer = configuration["Jwt:Issuer"] ?? configuration["JwtSettings:Issuer"] ?? "VeggieLife.API";
        var audience = configuration["Jwt:Audience"] ?? configuration["JwtSettings:Audience"] ?? "VeggieLife.Client";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Phục vụ môi trường dev
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var errorResponse = new ApiErrorResponse(
                        StatusCodes.Status401Unauthorized,
                        ErrorCodes.ErrAuthInvalidToken,
                        "Yêu cầu chưa được xác thực hoặc phiên đăng nhập đã hết hạn.");

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json; charset=utf-8";

                    var principal = context.Principal ?? context.HttpContext?.User;
                    var isGuest = principal?.IsInRole(UserRoles.Guest) == true ||
                                  principal?.Claims.Any(c => 
                                      (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type.EndsWith("/role")) && 
                                      string.Equals(c.Value, UserRoles.Guest, StringComparison.OrdinalIgnoreCase)) == true;

                    var path = context.HttpContext?.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                    var isCommunityAction = path.Contains("/videos") || 
                                           path.Contains("/posts") || 
                                           path.Contains("/comments") || 
                                           path.Contains("/votes") || 
                                           path.Contains("/violations") ||
                                           (path.Contains("/recipes") && context.HttpContext?.Request.Method != "GET");

                    string errorCode;
                    string errorMessage;

                    if (isGuest)
                    {
                        if (isCommunityAction)
                        {
                            errorCode = ErrorCodes.ErrGuestCommunityActionDenied;
                            errorMessage = "Khách vãng lai thực hiện thao tác ghi cộng đồng (FN-AUTH-03).";
                        }
                        else
                        {
                            errorCode = ErrorCodes.ErrGuestPersonalizationRestricted;
                            errorMessage = "Khách vãng lai gọi tính năng cá nhân hóa (FN-AUTH-03).";
                        }
                    }
                    else
                    {
                        errorCode = ErrorCodes.ErrAdmForbidden;
                        errorMessage = "Bạn không có quyền truy cập vào tài nguyên này.";
                    }

                    var errorResponse = new ApiErrorResponse(
                        StatusCodes.Status403Forbidden,
                        ErrorCodes.ErrAdmForbidden,
                        "Bạn không có quyền truy cập vào tài nguyên này.");

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Cấu hình các chính sách phân quyền theo vai trò (GUEST, MEMBER, VIP, ADMIN).
    /// </summary>
    public static IServiceCollection AddRoleAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.RequireAdmin, policy =>
                policy.RequireRole(UserRoles.Admin));

            options.AddPolicy(PolicyNames.RequireVip, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.Vip));

            options.AddPolicy(PolicyNames.RequireMember, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.Vip, UserRoles.Member));

            options.AddPolicy(PolicyNames.RequireGuestOrAbove, policy =>
                policy.RequireRole(UserRoles.Admin, UserRoles.Vip, UserRoles.Member, UserRoles.Guest));
        });

        return services;
    }

    /// <summary>
    /// Cấu hình Swagger UI kèm Bearer Token Authentication.
    /// </summary>
    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VeggieLife API",
                Version = "v1",
                Description = "Hệ sinh thái hỗ trợ lối sống thuần chay và dinh dưỡng cá nhân hóa VeggieLife - Backend API",
                Contact = new OpenApiContact
                {
                    Name = "VeggieLife Dev Team",
                    Email = "support@veggielife.vn"
                }
            });

            // Cấu hình nút Authorize trên giao diện Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Nhập token JWT theo định dạng: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Nối kết bộ nhớ đệm phân tán Redis với cơ chế abortConnect=false để không chặn tiến trình khi Redis cục bộ chưa chạy.
    /// </summary>
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false,connectTimeout=500,syncTimeout=500,asyncTimeout=500";
        if (!redisConn.Contains("connectTimeout", StringComparison.OrdinalIgnoreCase))
        {
            redisConn += ",connectTimeout=500,syncTimeout=500,asyncTimeout=500";
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConn;
            options.InstanceName = "VeggieLife_";
        });

        services.AddSingleton<IRedisService, RedisService>();

        return services;
    }

    /// <summary>
    /// Đăng ký các dịch vụ cốt lõi và các interface nghiệp vụ dùng chung (AI, Vector DB, Email, Media, Payment, Auth).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Dịch vụ JWT, Auth & Cache
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Cấu hình & Dịch vụ Cloudinary CDN dùng chung
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));
        services.AddSingleton<ICloudinaryClient, CloudinaryClient>();
        services.AddScoped<IMediaService, MediaService>();

        // Dịch vụ Hồ sơ cá nhân & Danh mục dùng chung
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IMasterDataService, MasterDataService>();

        // Dịch vụ Công thức món chay (FN-POST-01, FN-POST-02)
        services.AddScoped<VeggieLife.BLL.Services.Recipes.IRecipeService, VeggieLife.BLL.Services.Recipes.RecipeService>();

        // Dịch vụ Công thức món chay (FN-POST-01, FN-POST-02)
        services.AddScoped<VeggieLife.BLL.Services.Recipes.IRecipeService, VeggieLife.BLL.Services.Recipes.RecipeService>();

        // Dịch vụ Video & Phương tiện nấu ăn (FN-POST-08)
        services.AddScoped<VeggieLife.BLL.Services.Videos.IVideoService, VeggieLife.BLL.Services.Videos.VideoService>();

        // Các interface dịch vụ dùng chung khác
        services.AddScoped<IAiService, AiServiceStub>();
        services.AddScoped<IVectorDbService, VectorDbServiceStub>();
        services.AddScoped<IEmailService, EmailServiceStub>();
        services.AddScoped<IPaymentGatewayService, PaymentGatewayServiceStub>();

        return services;
    }

    /// <summary>
    /// Cấu hình dịch vụ kiểm tra sức khỏe hệ thống (Health Check).
    /// </summary>
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    /// <summary>
    /// Ánh xạ endpoint kiểm tra sức khỏe /health trả về JSON trạng thái.
    /// </summary>
    public static IEndpointRouteBuilder MapCustomHealthCheck(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = new
                {
                    status = report.Status.ToString(),
                    message = report.Status == HealthStatus.Healthy ? "VeggieLife API đang hoạt động bình thường." : "Một số thành phần gặp sự cố.",
                    timestamp = DateTime.UtcNow,
                    totalDurationMs = report.TotalDuration.TotalMilliseconds,
                    entries = report.Entries.Select(e => new
                    {
                        key = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        durationMs = e.Value.Duration.TotalMilliseconds
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
            }
        });

        return endpoints;
    }
}
