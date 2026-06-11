using Application.Identity.Tokens.Claims;

namespace Application.Payments.Refunds.Commands;

public sealed class MarkRefundProcessedCommand
{
    public required Guid RefundId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public string? ProviderRefundId { get; init; }
    public bool? MoneyWasRefunded { get; init; }
}
