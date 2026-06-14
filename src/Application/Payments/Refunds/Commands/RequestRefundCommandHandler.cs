using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace Application.Payments.Refunds.Commands;

public sealed class RequestRefundCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IRealtimeNotificationPublisher _publisher;

    public RequestRefundCommandHandler(IPaymentStore paymentStore, IRealtimeNotificationPublisher publisher)
    {
        _paymentStore = paymentStore;
        _publisher = publisher;
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

        Guid? orgId = null;
        Guid? storeId = null;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
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

            if (!string.Equals(command.RefundMethod, "FullMoneyRefund", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(command.RefundMethod, "Voucher", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<RefundResult>.Fail("Invalid refund method. Supported methods: FullMoneyRefund, Voucher.", 400);
            }

            if (transaction.Amount != order.TotalAmount)
            {
                return ApiResult<RefundResult>.Fail("Paid transaction amount must match order total for refund V1.", 409);
            }

            orgId = order.OrganizationId;
            storeId = order.StoreId;

            decimal refundAmount = order.TotalAmount;
            if (string.Equals(command.RefundMethod, "Voucher", StringComparison.OrdinalIgnoreCase))
            {
                if (command.VoucherValue.HasValue)
                {
                    if (command.VoucherValue.Value <= 0)
                    {
                        return ApiResult<RefundResult>.Fail("Voucher value must be greater than zero.", 400);
                    }

                    if (command.VoucherValue.Value != order.TotalAmount)
                    {
                        return ApiResult<RefundResult>.Fail("Voucher value must equal order total amount in refund V1.", 400);
                    }
                }
            }

            var note = command.Note;
            if (note?.Length > 100) note = note[..100];
            var reasonText = reason ?? string.Empty;
            if (reasonText.Length > 200) reasonText = reasonText[..200];

            var metadata = new
            {
                Method = command.RefundMethod,
                Code = command.VoucherCode,
                Value = string.Equals(command.RefundMethod, "Voucher", StringComparison.OrdinalIgnoreCase)
                    ? command.VoucherValue
                    : null,
                Note = note,
                Text = reasonText
            };
            var serializedReason = System.Text.Json.JsonSerializer.Serialize(metadata);

            var now = DateTimeOffset.UtcNow;

            var refundNumber = $"REF-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
            var refund = new Refund
            {
                Id = Guid.NewGuid(),
                PaymentTransactionId = transaction.Id,
                RequestedByAccountId = command.UserContext.AccountId,
                RefundNumber = refundNumber,
                IdempotencyKey = idempotencyKey,
                Amount = refundAmount,
                Currency = transaction.Currency,
                Reason = serializedReason,
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

        if (result.Succeeded && result.Data is not null)
        {
            await _publisher.PublishDashboardInvalidatedAsync(new DashboardInvalidatedEvent
            {
                Scope = "Organization",
                OrganizationId = orgId,
                StoreId = storeId,
                Reason = "RefundChanged",
                UpdatedAt = DateTimeOffset.UtcNow,
            }, cancellationToken);
        }

        return result;
    }
}
