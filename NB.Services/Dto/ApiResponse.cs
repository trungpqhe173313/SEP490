using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.Dto
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public int StatusCode { get; set; } = 200;
        public T? Data { get; set; }
        public ApiError? Error { get; set; }

        public static ApiResponse<T> Ok(T data) => new()
        {
            Data = data,
            Success = true,
            StatusCode = 200
        };

        public static ApiResponse<T> Fail(string message, int statusCode = 400) => new()
        {
            Success = false,
            StatusCode = statusCode,
            Error = new ApiError { Message = message }
        };

        public static ApiResponse<T> Fail(List<string> messages, int statusCode = 400) => new()
        {
            Success = false,
            StatusCode = statusCode,
            Error = new ApiError
            {
                Message = "Đã xảy ra lỗi",
                Messages = messages
            }
        };


        public static ApiResponse<T> OkWithWarnings(T data, List<string> warnings) => new()
        {
            Data = data,
            Success = true,
            StatusCode = 200,
            Error = new ApiError
            {
                Message = warnings.Any() ? $"Thành công nhưng có {warnings.Count} cảnh báo" : string.Empty,
                Messages = warnings
            }
        };
    }

    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
        public List<string>? Messages { get; set; }
        public string? Code { get; set; }
    }
}

