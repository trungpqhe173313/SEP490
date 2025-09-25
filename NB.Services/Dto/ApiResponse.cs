using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.Dto
{
    class ApiResponse<T>
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
    }

    public class ApiError
    {
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}

