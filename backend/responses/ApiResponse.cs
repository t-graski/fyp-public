namespace backend.responses;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }

    private ApiResponse()
    {
    }

    public static ApiResponse<T> Ok(T data, int statusCode = StatusCodes.Status200OK)
        => new()
        {
            Success = true,
            StatusCode = statusCode,
            Data = data,
            Error = null
        };

    public static ApiResponse<T> Fail(int statusCode, string errorCode, string message)
        => new()
        {
            Success = false,
            StatusCode = statusCode,
            Data = default,
            Error = new ApiError(errorCode, message)
        };
}

public record ApiError(string Code, string Message);