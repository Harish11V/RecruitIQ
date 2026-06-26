using System.Collections.Generic;

namespace RecruitIQ.Common;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public ApiResponse() { }

    public ApiResponse(bool success, string message, List<string>? errors = null)
    {
        Success = success;
        Message = message;
        if (errors != null) Errors = errors;
    }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(T data, bool success, string message, List<string>? errors = null)
        : base(success, message, errors)
    {
        Data = data;
    }
}
