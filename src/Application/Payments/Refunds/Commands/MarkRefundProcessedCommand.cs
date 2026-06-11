using Application.Identity.Tokens.Claims;
using System;

namespace Application.Payments.Refunds.Commands;

public sealed class MarkRefundProcessedCommand
{
    public required Guid RefundId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public string? ProviderRefundId { get; init; }
}
