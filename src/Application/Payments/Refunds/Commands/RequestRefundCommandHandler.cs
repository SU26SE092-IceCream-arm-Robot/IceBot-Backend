using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.Refunds.Commands;

public sealed class RequestRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;

    public RequestRefundCommandHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<ApiResult<RefundResult>> HandleAsync(
        RequestRefundCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<RefundResult>.Fail("Reason is required to request a refund.", 400);
        }

        return await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var idempotencyKey = command.IdempotencyKey?.Trim();
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existing = await _paymentStore.GetRefundByIdempotencyKeyAsync(idempotencyKey, ct);
                if (existing is not null)
                {
                    var existingOrder = existing.PaymentTransaction.Order;
                    if (!ScopeAccessRules.CanAccessScopedRow(
                        command.UserContext,
                        existingOrder.OrganizationId,
                        existingOrder.StoreId,
                        existingOrder.KioskId))
                    {
                        return ApiResult<RefundResult>.Fail("Access denied.", 403);
                    }

                    return ApiResult<RefundResult>.Success(
                        RefundResultMapper.ToResult(existing),
                        "Refund request already exists.");
                }
            }

            var order = await _paymentStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<RefundResult>.Fail("Order not found.", 404);
            }

            if (!ScopeAccessRules.CanAccessScopedRow(
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<RefundResult>.Fail("Access denied.", 403);
            }

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                return ApiResult<RefundResult>.Fail("Refund can only be requested for paid orders.", 409);
            }

            if (order.Status != OrderStatus.RefundRequired)
            {
                return ApiResult<RefundResult>.Fail(
                    "Order must be flagged as refund required before creating a refund request.",
                    409);
            }

            var transaction = await _paymentStore.GetLatestPaidPaymentTransactionByOrderIdAsync(order.Id, ct);
            if (transaction is null)
            {
                return ApiResult<RefundResult>.Fail("No paid transaction found for this order.", 409);
            }

            var exists = await _paymentStore.RefundExistsForTransactionAsync(transaction.Id, ct);
            if (exists)
            {
                return ApiResult<RefundResult>.Fail("A refund has already been requested/processed for this transaction.", 409);
            }

            var now = DateTimeOffset.UtcNow;

            var refundNumber = $"REF-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentTransactionId = transaction.Id,
                RequestedByAccountId = command.UserContext.AccountId,
                RefundNumber = refundNumber,
                IdempotencyKey = idempotencyKey,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Reason = reason,
                Status = RefundStatus.Requested,
                RequestedAt = now,
                CreatedAt = now
            };

            await _paymentStore.AddRefundAsync(refund, ct);
            await _paymentStore.SaveChangesAsync(ct);

            // Fetch to ensure full navigation properties are loaded for mapping
            var createdRefund = await _paymentStore.GetRefundByIdAsync(refund.Id, ct);
            return ApiResult<RefundResult>.Success(
                RefundResultMapper.ToResult(createdRefund ?? refund),
                "Refund requested successfully.",
                201);
        }, cancellationToken);
    }
}
