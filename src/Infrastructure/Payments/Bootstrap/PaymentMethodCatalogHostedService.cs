using Domain.Payments.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Payments.Bootstrap;

public sealed class PaymentMethodCatalogHostedService : IHostedService
{
    private const string PayOsMethodCode = "payos";
    private const string CashMethodCode = "cash";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;

    public PaymentMethodCatalogHostedService(IServiceScopeFactory scopeFactory, IHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _environment = environment;
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

        var cashPaymentMethod = await dbContext.PaymentMethods
            .SingleOrDefaultAsync(method => method.Code == CashMethodCode, cancellationToken);
        if (cashPaymentMethod is null && _environment.IsDevelopment())
        {
            cashPaymentMethod = new PaymentMethod
            {
                Code = CashMethodCode,
                Name = "Cash",
                Description = "Staff-confirmed cash payment for local development.",
                Provider = "Cash",
                MethodType = "Cash",
                IsOnline = false,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.PaymentMethods.Add(cashPaymentMethod);
        }
        else if (cashPaymentMethod is not null)
        {
            cashPaymentMethod.Name = "Cash";
            cashPaymentMethod.Description = "Staff-confirmed cash payment.";
            cashPaymentMethod.Provider = "Cash";
            cashPaymentMethod.MethodType = "Cash";
            cashPaymentMethod.IsOnline = false;
            cashPaymentMethod.IsActive = _environment.IsDevelopment();
            cashPaymentMethod.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
