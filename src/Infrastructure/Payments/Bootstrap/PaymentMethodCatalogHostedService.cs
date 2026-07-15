using Domain.Payments.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Payments.Bootstrap;

public sealed class PaymentMethodCatalogHostedService : IHostedService
{
    private const string PayOsMethodCode = "payos";
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentMethodCatalogHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtext('payment-method-catalog:payos'))",
            cancellationToken);
        var paymentMethod = await dbContext.PaymentMethods
            .SingleOrDefaultAsync(method => method.Code == PayOsMethodCode, cancellationToken);

        if (paymentMethod is null)
        {
            paymentMethod = new PaymentMethod
            {
                Code = PayOsMethodCode,
                Name = "PayOS",
                Description = "PayOS payment gateway",
                Provider = "PayOS",
                MethodType = "BankTransferQr",
                IsOnline = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.PaymentMethods.Add(paymentMethod);
        }
        else
        {
            paymentMethod.Name = "PayOS";
            paymentMethod.Description = "PayOS payment gateway";
            paymentMethod.Provider = "PayOS";
            paymentMethod.MethodType = "BankTransferQr";
            paymentMethod.IsOnline = true;
            paymentMethod.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
