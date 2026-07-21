using Application.Payments.Abstractions;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Orders.Support;
using Application.Payments.PaymentSessions.Support;

namespace Infrastructure.Payments.Persistence;

public sealed class PaymentStore : IPaymentStore
{
    private readonly IceBotDbContext _dbContext;

    public PaymentStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.WhereNotDeleted()
            .Include(order => order.Kiosk)
                .ThenInclude(kiosk => kiosk.Store)
            .Include(order => order.Kiosk)
                .ThenInclude(kiosk => kiosk.Organization)
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<PaymentMethod?> GetPaymentMethodByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentMethods
            .FirstOrDefaultAsync(method => method.Code == code, cancellationToken);
    }

    public Task<List<PaymentMethod>> ListPaymentMethodsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentMethods
            .OrderBy(method => method.DisplayOrder)
            .ThenBy(method => method.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions.WhereNotDeleted()
            .Include(payment => payment.Order)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
            .Include(payment => payment.PaymentMethod)
            .FirstOrDefaultAsync(payment => payment.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionSnapshotAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListPendingPaymentSessionReconciliationIdsAsync(
        DateTimeOffset requestedBefore,
        DateTimeOffset retryDueAt,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .Where(PaymentSessionInterventionPolicy.BuildReconciliationCandidatePredicate(retryDueAt))
            .Where(payment =>
                payment.RequestedAt <= requestedBefore &&
                payment.RetryCount < payment.MaxRetries &&
                (!payment.NextRetryAt.HasValue || payment.NextRetryAt <= retryDueAt))
            .OrderBy(payment => payment.NextRetryAt ?? payment.RequestedAt)
            .ThenBy(payment => payment.Id)
            .Select(payment => payment.Id)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);

    public Task<int> CountPaymentSessionInterventionsAsync(
        DateTimeOffset observedAt,
        string? provider,
        string? interventionCode,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default) =>
        ApplyPaymentSessionInterventionFilters(
                observedAt,
                provider,
                interventionCode,
                organizationId,
                storeId,
                kioskId,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<PaymentTransaction>> ListPaymentSessionInterventionsAsync(
        DateTimeOffset observedAt,
        string? provider,
        string? interventionCode,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await ApplyPaymentSessionInterventionFilters(
                observedAt,
                provider,
                interventionCode,
                organizationId,
                storeId,
                kioskId,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .OrderBy(payment => payment.NextRetryAt ?? payment.RequestedAt)
            .ThenBy(payment => payment.Id)
            .Skip((Math.Max(pageNumber, 1) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);

    public Task<Domain.Devices.Connectivity.KioskConnectivityProjection?> GetKioskConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.AsNoTracking()
            .FirstOrDefaultAsync(connectivity => connectivity.KioskId == kioskId, cancellationToken);

    public Task<PaymentTransaction?> GetActivePaymentTransactionByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions.WhereNotDeleted()
            .Include(payment => payment.Order)
            .Include(payment => payment.PaymentMethod)
            .Where(payment => payment.OrderId == orderId &&
                (payment.Status == Domain.Payments.Enums.PaymentTransactionStatus.Pending ||
                 payment.Status == Domain.Payments.Enums.PaymentTransactionStatus.Authorized))
            .OrderByDescending(payment => payment.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByProviderOrderCodeAsync(string provider, string providerOrderCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
                .ThenInclude(order => order.OrderItems)
            .FirstOrDefaultAsync(
                payment => payment.Provider == provider && payment.ProviderOrderCode == providerOrderCode,
                cancellationToken);
    }

    public Task<PaymentTransaction?> GetLatestPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions.WhereNotDeleted()
            .Include(payment => payment.Order)
            .Where(payment => payment.OrderId == orderId)
            .OrderByDescending(payment => payment.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PaymentTransaction?> GetLatestPaidPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions.WhereNotDeleted()
            .Include(payment => payment.Order)
            .Where(payment => payment.OrderId == orderId && payment.Status == Domain.Payments.Enums.PaymentTransactionStatus.Paid)
            .OrderByDescending(payment => payment.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PaymentCallback?> GetPaymentCallbackAsync(
        string provider,
        string providerEventId,
        CancellationToken cancellationToken = default) =>
        _dbContext.PaymentCallbacks.AsNoTracking()
            .Include(callback => callback.PaymentTransaction)
            .FirstOrDefaultAsync(
                callback => callback.Provider == provider && callback.ProviderEventId == providerEventId,
                cancellationToken);

    public async Task<IReadOnlyList<PaymentTransaction>> ListPaymentTransactionsByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .OrderByDescending(payment => payment.RequestedAt)
            .ThenByDescending(payment => payment.Id)
            .ToListAsync(cancellationToken);

    public Task AcquirePaymentSessionLockAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"payment-session:{orderId:N}", cancellationToken);

    public Task AcquireOrderWorkflowLockAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync(OrderWorkflowConcurrency.OrderLockKey(orderId), cancellationToken);

    public Task ReloadOrderAsync(Order order, CancellationToken cancellationToken = default) =>
        _dbContext.Entry(order).ReloadAsync(cancellationToken);

    public Task AcquirePaymentTransactionLockAsync(
        Guid paymentTransactionId,
        CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"payment-transaction:{paymentTransactionId:N}", cancellationToken);

    public Task AcquireRefundRequestLockAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"refund:{paymentTransactionId:N}", cancellationToken);

    public Task AcquireRefundLockAsync(Guid refundId, CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"refund-lifecycle:{refundId:N}", cancellationToken);

    public Task AcquirePaymentCallbackLockAsync(
        string provider,
        string providerEventId,
        CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"payment-callback:{provider}:{providerEventId}", cancellationToken);

    public Task AcquireProviderPaymentLockAsync(
        string provider,
        string providerOrderCode,
        CancellationToken cancellationToken = default) =>
        AcquireAdvisoryLockAsync($"provider-payment:{provider}:{providerOrderCode}", cancellationToken);

    private IQueryable<PaymentTransaction> ApplyPaymentSessionInterventionFilters(
        DateTimeOffset observedAt,
        string? provider,
        string? interventionCode,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .Include(payment => payment.Order)
            .Where(PaymentSessionInterventionPolicy.BuildQueuePredicate(observedAt));

        if (!string.IsNullOrWhiteSpace(provider))
        {
            query = query.Where(payment => payment.Provider == provider.Trim());
        }

        if (!string.IsNullOrWhiteSpace(interventionCode))
        {
            query = query.Where(payment => payment.LastErrorCode == interventionCode.Trim());
        }

        if (organizationId.HasValue)
        {
            query = query.Where(payment => payment.Order.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(payment => payment.Order.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(payment => payment.Order.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            query = query.Where(payment =>
                (payment.Order.OrganizationId.HasValue &&
                 allowedOrganizationIds.Contains(payment.Order.OrganizationId.Value)) ||
                (payment.Order.StoreId.HasValue &&
                 allowedStoreIds.Contains(payment.Order.StoreId.Value)) ||
                allowedKioskIds.Contains(payment.Order.KioskId));
        }

        return query;
    }

    public async Task AddPaymentMethodAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken);
    }

    public async Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentTransactions.AddAsync(paymentTransaction, cancellationToken);
    }

    public async Task AddPaymentCallbackAsync(PaymentCallback paymentCallback, CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentCallbacks.AddAsync(paymentCallback, cancellationToken);
    }

    public async Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken);
    }

    public Task<Refund?> GetRefundByIdAsync(Guid refundId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Refunds
            .Include(r => r.PaymentTransaction)
                .ThenInclude(p => p.Order)
            .FirstOrDefaultAsync(r => r.Id == refundId, cancellationToken);
    }

    public Task<Refund?> GetRefundByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.Refunds
            .Include(r => r.PaymentTransaction)
                .ThenInclude(p => p.Order)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        await _dbContext.Refunds.AddAsync(refund, cancellationToken);
    }

    public Task<bool> RefundExistsForTransactionAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Refunds.AnyAsync(r => r.PaymentTransactionId == transactionId && r.Status != Domain.Payments.Enums.RefundStatus.Cancelled, cancellationToken);
    }

    public Task<int> CountRefundsAsync(
        string? search,
        Domain.Payments.Enums.RefundStatus? status,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyRefundFiltersAndScope(
            search,
            status,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<Refund>> ListRefundsAsync(
        string? search,
        Domain.Payments.Enums.RefundStatus? status,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyRefundFiltersAndScope(
            search,
            status,
            organizationId,
            storeId,
            kioskId,
            isSystemAdmin,
            allowedOrganizationIds,
            allowedStoreIds,
            allowedKioskIds);

        return query
            .Include(r => r.PaymentTransaction)
                .ThenInclude(p => p.Order)
            .OrderByDescending(r => r.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Refund> ApplyRefundFiltersAndScope(
        string? search,
        Domain.Payments.Enums.RefundStatus? status,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds)
    {
        var query = _dbContext.Refunds.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(r =>
                r.RefundNumber.ToLower().Contains(searchLower) ||
                r.PaymentTransaction.Order.OrderNumber.ToLower().Contains(searchLower));
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(r => r.PaymentTransaction.Order.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(r => r.PaymentTransaction.Order.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(r => r.PaymentTransaction.Order.KioskId == kioskId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(r =>
                (r.PaymentTransaction.Order.OrganizationId.HasValue && allowedOrgs.Contains(r.PaymentTransaction.Order.OrganizationId.Value)) ||
                (r.PaymentTransaction.Order.StoreId.HasValue && allowedStores.Contains(r.PaymentTransaction.Order.StoreId.Value)) ||
                allowedKiosks.Contains(r.PaymentTransaction.Order.KioskId));
        }

        return query;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task AcquireAdvisoryLockAsync(string lockKey, CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
    }
}
