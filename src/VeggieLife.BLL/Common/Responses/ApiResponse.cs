using System.Collections.Generic;

namespace VeggieLife.BLL.Common.Responses
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; } = 200;
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse() { }

        public ApiResponse(T? data, string message = "OK", int statusCode = 200)
        {
            StatusCode = statusCode;
            Success = true;
            Message = message;
            Data = data;
        }
    }

    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse() { }

        public ApiResponse(object? data, string message = "OK", int statusCode = 200)
            : base(data, message, statusCode) { }
    }
}
