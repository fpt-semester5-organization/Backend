using System;
using VeggieLife.BLL.Common.Constants;

namespace VeggieLife.BLL.Common.Exceptions;

/// <summary>
/// Ngoại lệ nghiệp vụ cơ sở của hệ thống VeggieLife.
/// Được Middleware tự động bắt và chuyển thành ApiErrorResponse { statusCode, errorCode, message, details }.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public object? Details { get; }

    public AppException(int statusCode, string errorCode, string message, object? details = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Details = details;
    }

    public static AppException BadRequest(string errorCode = ErrorCodes.ErrValidation, string message = "Dữ liệu yêu cầu không hợp lệ.", object? details = null)
        => new(400, errorCode, message, details);

    public static AppException Unauthorized(string errorCode = ErrorCodes.ErrAuthInvalidToken, string message = "Yêu cầu chưa được xác thực hoặc phiên đăng nhập hết hạn.", object? details = null)
        => new(401, errorCode, message, details);

    public static AppException Forbidden(string errorCode = ErrorCodes.ErrAdmForbidden, string message = "Bạn không có quyền thực hiện thao tác này.", object? details = null)
        => new(403, errorCode, message, details);

    public static AppException NotFound(string errorCode = ErrorCodes.ErrContentNotFound, string message = "Không tìm thấy tài nguyên yêu cầu.", object? details = null)
        => new(404, errorCode, message, details);

    public static AppException Conflict(string errorCode = ErrorCodes.ErrAuthEmailExists, string message = "Tài nguyên đã tồn tại hoặc xảy ra xung đột trạng thái.", object? details = null)
        => new(409, errorCode, message, details);

    public static AppException TooManyRequests(string errorCode = ErrorCodes.ErrGuestRateLimitExceeded, string message = "Thao tác quá nhanh hoặc vượt quá giới hạn cho phép.", object? details = null)
        => new(429, errorCode, message, details);

    public static AppException Internal(string errorCode = ErrorCodes.ErrInternal, string message = "Đã xảy ra lỗi nội bộ hệ thống.", object? details = null)
        => new(500, errorCode, message, details);

    public static AppException BadGateway(string errorCode = ErrorCodes.ErrExtCloudinaryFailed, string message = "Không thể kết nối đến máy chủ dịch vụ bên ngoài (502 Bad Gateway).", object? details = null)
        => new(502, errorCode, message, details);
}
