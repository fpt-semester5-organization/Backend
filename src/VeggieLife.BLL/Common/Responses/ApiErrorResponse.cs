namespace VeggieLife.BLL.Common.Responses;

/// <summary>
/// Lớp bọc phản hồi lỗi chuẩn của hệ thống VeggieLife.
/// Cấu trúc quy chuẩn theo tài liệu kỹ thuật: { statusCode, errorCode, message, details }
/// </summary>
public class ApiErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object? Data { get; set; }

    public ApiErrorResponse()
    {
    }

    public ApiErrorResponse(int statusCode, string errorCode, string message, object? details = null)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Message = message;
        Details = details;
        Data = details;
    }

    public static ApiErrorResponse Create(int statusCode, string errorCode, string message, object? details = null)
    {
        return new ApiErrorResponse(statusCode, errorCode, message, details);
    }
}
