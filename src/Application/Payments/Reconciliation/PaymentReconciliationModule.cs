using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.Reconciliation;

public static class PaymentReconciliationModule
{
    public static IServiceCollection AddPaymentReconciliationModule(this IServiceCollection services)
    {
        services.AddScoped<GetDailyPaymentReconciliationQueryHandler>();
        services.AddScoped<ListPaymentReconciliationDiscrepanciesQueryHandler>();
        return services;
    }
}
