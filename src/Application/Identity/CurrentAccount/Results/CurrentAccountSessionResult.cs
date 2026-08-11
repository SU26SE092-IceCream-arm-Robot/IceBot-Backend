namespace Application.Identity.CurrentAccount.Results;

public sealed class CurrentAccountSessionResult
{
    public Guid SessionId { get; init; }

    public bool IsCurrentSession { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string DeviceName { get; init; } = string.Empty;
}
