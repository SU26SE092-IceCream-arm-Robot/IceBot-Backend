using Application.Payments.Abstractions;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        return _dbContext.Orders
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
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
            .FirstOrDefaultAsync(payment => payment.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public Task<PaymentTransaction?> GetPaymentTransactionByProviderOrderCodeAsync(string provider, string providerOrderCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
            .FirstOrDefaultAsync(
                payment => payment.Provider == provider && payment.ProviderOrderCode == providerOrderCode,
                cancellationToken);
    }

    public Task<PaymentTransaction?> GetLatestPaymentTransactionByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentTransactions
            .Include(payment => payment.Order)
            .Where(payment => payment.OrderId == orderId)
            .OrderByDescending(payment => payment.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> PaymentCallbackExistsAsync(string provider, string providerEventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PaymentCallbacks.AnyAsync(
            callback => callback.Provider == provider && callback.ProviderEventId == providerEventId,
            cancellationToken);
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
}
