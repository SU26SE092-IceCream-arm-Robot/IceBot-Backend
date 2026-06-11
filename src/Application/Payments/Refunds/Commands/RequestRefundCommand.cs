using Application.Identity.Tokens.Claims;
using System;

namespace Application.Payments.Refunds.Commands;

public sealed class RequestRefundCommand
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required string Reason { get; init; }
    public string? IdempotencyKey { get; init; }
}
