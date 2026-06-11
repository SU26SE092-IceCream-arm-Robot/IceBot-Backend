using Application.Identity.Tokens.Claims;

namespace Application.Payments.Refunds.Commands;

public sealed class RejectRefundCommand
{
    public required Guid RefundId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required string Reason { get; init; }
}
