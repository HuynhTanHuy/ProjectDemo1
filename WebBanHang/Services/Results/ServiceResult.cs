namespace WebBanHang.Services.Results
{
    public class ServiceResult
    {
        public bool Success { get; init; }
        public string ErrorCode { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        public static ServiceResult Ok(string? message = null) =>
            new() { Success = true, Message = message ?? string.Empty };

        public static ServiceResult Fail(string code, string message) =>
            new() { Success = false, ErrorCode = code, Message = message };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message ?? string.Empty };

        public new static ServiceResult<T> Fail(string code, string message) =>
            new() { Success = false, ErrorCode = code, Message = message };
    }
}
