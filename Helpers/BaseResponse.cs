namespace AspCoreApi.Helpers;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static BaseResponse<T> SuccessResponse(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static BaseResponse<T> FailureResponse(string message) =>
        new() { Success = false, Message = message };
}