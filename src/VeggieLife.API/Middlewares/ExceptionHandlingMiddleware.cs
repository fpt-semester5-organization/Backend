using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VeggieLife.BLL.Common.Constants;
using VeggieLife.BLL.Common.Exceptions;
using VeggieLife.BLL.Common.Responses;

namespace VeggieLife.API.Middlewares;

/// <summary>
/// Middleware xử lý ngoại lệ tập trung toàn cục (Global Exception Handling Middleware).
/// Bắt mọi lỗi xảy ra trong pipeline và đóng gói thành ApiErrorResponse chuẩn { statusCode, errorCode, message, details }.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException appEx)
        {
            _logger.LogWarning(appEx, "Ngoại lệ nghiệp vụ AppException [Code: {ErrorCode}, Status: {StatusCode}]: {Message}", 
                appEx.ErrorCode, appEx.StatusCode, appEx.Message);

            await HandleExceptionAsync(context, appEx.StatusCode, appEx.ErrorCode, appEx.Message, appEx.Details);
        }
        catch (BadHttpRequestException badReqEx)
        {
            _logger.LogWarning(badReqEx, "Dữ liệu HTTP request không hợp lệ: {Message}", badReqEx.Message);

            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, ErrorCodes.ErrValidation, 
                "Cấu trúc yêu cầu HTTP không hợp lệ hoặc dữ liệu gửi lên sai định dạng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ngoại lệ hệ thống chưa được kiểm soát (Unhandled Exception): {Message}", ex.Message);

            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, ErrorCodes.ErrInternal, 
                "Đã xảy ra lỗi nội bộ máy chủ. Vui lòng thử lại sau hoặc liên hệ quản trị viên.");
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context, 
        int statusCode, 
        string errorCode, 
        string message, 
        object? details = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var errorResponse = new ApiErrorResponse(statusCode, errorCode, message, details);
        var json = JsonSerializer.Serialize(errorResponse, JsonOptions);

        await context.Response.WriteAsync(json);
    }
}
