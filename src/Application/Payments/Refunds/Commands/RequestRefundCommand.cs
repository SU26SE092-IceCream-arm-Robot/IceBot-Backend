using Application.Identity.Tokens.Claims;

namespace Application.Payments.Refunds.Commands;

public sealed class RequestRefundCommand
{
    public required Guid OrderId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required string RefundMethod { get; init; }
    public required string Reason { get; init; }
    public string? VoucherCode { get; init; }
    public decimal? VoucherValue { get; init; }
    public string? Note { get; init; }
    public string? IdempotencyKey { get; init; }
}
