namespace VeggieLife.BLL.Common.Responses;

/// <summary>
/// Lớp bọc kết quả trả về chuẩn của hệ thống VeggieLife cho các phản hồi thành công.
/// Quy chuẩn cấu trúc: { statusCode, message, data }
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của payload trả về.</typeparam>
public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(int statusCode, string message, T? data = default)
    {
        StatusCode = statusCode;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> Success(T? data, string message = "Thao tác thành công.", int statusCode = 200)
    {
        return new ApiResponse<T>(statusCode, message, data);
    }
}

/// <summary>
/// Lớp bọc kết quả trả về thành công không kèm dữ liệu payload.
/// </summary>
public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;

    public ApiResponse()
    {
    }

    public ApiResponse(int statusCode, string message)
    {
        StatusCode = statusCode;
        Message = message;
    }

    public static ApiResponse Success(string message = "Thao tác thành công.", int statusCode = 200)
    {
        return new ApiResponse(statusCode, message);
    }
}
