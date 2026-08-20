using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
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

        if (!ScopedIdempotencyKey.TryNormalize(command.IdempotencyKey, out var idempotencyKey))
        {
            return ApiResult<RefundResult>.Fail(
                $"Idempotency-Key is required and must be at most {ScopedIdempotencyKey.MaxClientKeyLength} characters.",
                400);
        }

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await _paymentStore.AcquireOrderWorkflowLockAsync(command.OrderId, ct);
            var order = await _paymentStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<RefundResult>.Fail("Order not found.", 404);
            }

            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.RefundsRequest,
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

            var paidTransactions = (await _paymentStore.ListPaymentTransactionsByOrderIdAsync(order.Id, ct))
                .Where(candidate =>
                    candidate.Status == PaymentTransactionStatus.Paid &&
                    candidate.SettlementDisposition != PaymentSettlementDisposition.DuplicateResolved)
                .ToList();
            var duplicatePayments = paidTransactions
                .Where(candidate =>
                    candidate.SettlementDisposition == PaymentSettlementDisposition.DuplicateRefundRequired)
                .ToList();
            PaymentTransaction? transaction;
            if (command.PaymentTransactionId.HasValue)
            {
                transaction = paidTransactions.FirstOrDefault(candidate =>
                    candidate.Id == command.PaymentTransactionId.Value);
                if (transaction is null)
                {
                    return ApiResult<RefundResult>.Fail(
                        "The selected paid transaction does not belong to this order.", 409);
                }


                if (duplicatePayments.Count > 0 &&
                    transaction.SettlementDisposition != PaymentSettlementDisposition.DuplicateRefundRequired)
                {
                    return ApiResult<RefundResult>.Fail(
                        "An unresolved duplicate payment must be selected before refunding the primary settlement.",
                        409);
                }
            }
            else
            {
                if (duplicatePayments.Count > 1)
                {
                    return ApiResult<RefundResult>.Fail(
                        "PaymentTransactionId is required because this order has multiple duplicate payments awaiting resolution.",
                        409);
                }

                transaction = duplicatePayments.SingleOrDefault() ?? paidTransactions
                    .Where(candidate =>
                        candidate.SettlementDisposition is PaymentSettlementDisposition.Primary or
                            PaymentSettlementDisposition.Unassigned)
                    .OrderByDescending(candidate =>
                        candidate.SettlementDisposition == PaymentSettlementDisposition.Primary)
                    .ThenBy(candidate => candidate.PaidAt ?? candidate.RequestedAt)
                    .FirstOrDefault();
            }

            if (transaction is null)
            {
                return ApiResult<RefundResult>.Fail("No paid transaction found for this order.", 409);
            }

            await _paymentStore.AcquireRefundRequestLockAsync(transaction.Id, ct);
            var scopedIdempotencyKey = ScopedIdempotencyKey.ForPaymentTransaction(transaction.Id, idempotencyKey);
            var existingByIdempotencyKey = await _paymentStore.GetRefundByIdempotencyKeyAsync(scopedIdempotencyKey, ct);
            if (existingByIdempotencyKey is not null)
            {
                return ApiResult<RefundResult>.Success(
                    RefundResultMapper.ToResult(existingByIdempotencyKey),
                    "Refund request already exists.");
            }

            var exists = await _paymentStore.RefundExistsForTransactionAsync(transaction.Id, ct);
            if (exists)
            {
                return ApiResult<RefundResult>.Fail("A refund has already been requested/processed for this transaction.", 409);
            }

            if (!TryParseCompensationMethod(command.RefundMethod, out var compensationMethod))
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
            if (compensationMethod == RefundCompensationMethod.Voucher)
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
                Method = compensationMethod.ToString(),
                Code = command.VoucherCode,
                Value = compensationMethod == RefundCompensationMethod.Voucher
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
                IdempotencyKey = scopedIdempotencyKey,
                Amount = refundAmount,
                Currency = transaction.Currency,
                CompensationMethod = compensationMethod,
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

    private static bool TryParseCompensationMethod(string? value, out RefundCompensationMethod method)
    {
        if (string.Equals(value, nameof(RefundCompensationMethod.FullMoneyRefund), StringComparison.OrdinalIgnoreCase))
        {
            method = RefundCompensationMethod.FullMoneyRefund;
            return true;
        }

        if (string.Equals(value, nameof(RefundCompensationMethod.Voucher), StringComparison.OrdinalIgnoreCase))
        {
            method = RefundCompensationMethod.Voucher;
            return true;
        }

        method = default;
        return false;
    }
}
