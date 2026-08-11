namespace Application.Identity.CurrentAccount.Commands;

public sealed record RevokeCurrentAccountSessionCommand(
    Guid AccountId,
    Guid SessionId,
    string? IpAddress,
    string? UserAgent);
