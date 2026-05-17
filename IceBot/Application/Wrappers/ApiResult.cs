namespace Application.Wrappers;

public class ApiResult<T>
{
    private const string DefaultSuccessMessage = "Operation successful.";

    public bool Succeeded { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? Message { get; set; }
    public T? Data { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public Dictionary<string, List<string>>? ValidationErrors { get; set; }
    public string? BusinessError { get; set; }
    public string? SystemError { get; set; }

    public ApiResult()
    {
    }

    public static ApiResult<T> Success()
    {
        return new ApiResult<T>
        {
            Succeeded = true,
            Message = DefaultSuccessMessage
        };
    }

    public static ApiResult<T> Success(T data, string message = DefaultSuccessMessage, int statusCode = 200)
    {
        return new ApiResult<T>
        {
            Succeeded = true,
            StatusCode = statusCode,
            Message = message,
            Data = data
        };
    }

    public static ApiResult<T> Fail(
        string message,
        int statusCode = 400,
        string? businessError = null,
        string? systemError = null,
        Dictionary<string, List<string>>? validationErrors = null)
    {
        return new ApiResult<T>
        {
            Succeeded = false,
            StatusCode = statusCode,
            Message = message,
            BusinessError = businessError,
            SystemError = systemError,
            ValidationErrors = validationErrors
        };
    }

    public ApiResult<T> AddDetail(string key, object value)
    {
        Details ??= new Dictionary<string, object>();
        Details[key] = value;
        return this;
    }

    public ApiResult<T> AddValidationError(string key, string message)
    {
        ValidationErrors ??= new Dictionary<string, List<string>>();
        if (!ValidationErrors.TryGetValue(key, out var errors))
        {
            errors = new List<string>();
            ValidationErrors[key] = errors;
        }

        errors.Add(message);
        return this;
    }

    public ApiResult<T> SetBusinessError(string message)
    {
        BusinessError = message;
        return this;
    }

    public ApiResult<T> SetSystemError(string message)
    {
        SystemError = message;
        return this;
    }

    public ApiResult<T> SetStatusCode(int code)
    {
        StatusCode = code;
        return this;
    }
}
