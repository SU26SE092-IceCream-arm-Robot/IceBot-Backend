using System.Text.RegularExpressions;

namespace Application.Shared.Wrappers;

public sealed partial record ApiBusinessErrorDefinition
{
    public ApiBusinessErrorDefinition(string code, int statusCode, string message)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern().IsMatch(code))
        {
            throw new ArgumentException("Business error codes must use uppercase dotted segments.", nameof(code));
        }

        if (statusCode is < 400 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), "Business error status codes must be HTTP error statuses.");
        }

        if (string.IsNullOrWhiteSpace(message) || message != message.Trim() || message.Contains('\r') || message.Contains('\n'))
        {
            throw new ArgumentException("Business error messages must be non-empty, trimmed, single-line text.", nameof(message));
        }

        Code = code;
        StatusCode = statusCode;
        Message = message;
    }

    public string Code { get; }
    public int StatusCode { get; }
    public string Message { get; }

    [GeneratedRegex("^[A-Z][A-Z0-9_]*(\\.[A-Z][A-Z0-9_]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
