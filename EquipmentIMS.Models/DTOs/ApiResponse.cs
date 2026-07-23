namespace EquipmentIMS.Models.DTOs;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, string>? Errors { get; set; }

    public static ApiResponse Ok(object? data = null)
    {
        return new ApiResponse { Success = true, Message = "操作成功", Data = data };
    }

    public static ApiResponse Ok(string message, object? data = null)
    {
        return new ApiResponse { Success = true, Message = message, Data = data };
    }

    public static ApiResponse Fail(string message, string errorCode)
    {
        return new ApiResponse { Success = false, Message = message, ErrorCode = errorCode };
    }

    public static ApiResponse FailValidation(Dictionary<string, string> errors)
    {
        return new ApiResponse
        {
            Success = false,
            Message = "参数校验失败",
            ErrorCode = "VAL_ERROR",
            Errors = errors
        };
    }
}
