using Application.Identity.Tokens.Claims;

namespace Application.Payments.Refunds.Commands;

public sealed class CancelRefundCommand
{
    public required Guid RefundId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public string? Reason { get; init; }
}
