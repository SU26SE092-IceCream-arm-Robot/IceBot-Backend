namespace Application.Wrappers;

public class InternalResult<T>
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public Exception? Exception { get; init; }
    public Dictionary<string, object>? Details { get; private set; }

    private InternalResult()
    {
    }

    public static InternalResult<T> Success(T data, string? message = null)
        => new()
        {
            Succeeded = true,
            Message = message,
            Data = data
        };

    public static InternalResult<T> Success(string? message = null)
        => new()
        {
            Succeeded = true,
            Message = message
        };

    public static InternalResult<T> Fail(string message, Exception? exception = null)
        => new()
        {
            Succeeded = false,
            Message = message,
            Exception = exception
        };

    public InternalResult<T> AddDetail(string key, object value)
    {
        Details ??= new Dictionary<string, object>();
        Details[key] = value;
        return this;
    }
}
