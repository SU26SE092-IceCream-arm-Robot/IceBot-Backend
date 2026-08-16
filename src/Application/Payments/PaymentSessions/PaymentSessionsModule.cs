using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Queries;
using Application.Payments.PaymentSessions.Diagnostics;
using Application.Payments.PaymentSessions.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.PaymentSessions;

public static class PaymentSessionsModule
{
    public static IServiceCollection AddPaymentSessionsModule(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentSessionCommandHandler>();
        services.AddScoped<ConfirmCashPaymentCommandHandler>();
        services.AddScoped<HandlePaymentProviderNotificationCommandHandler>();
        services.AddScoped<ReconcilePendingPaymentSessionCommandHandler>();
        services.AddScoped<ManuallyReconcilePaymentSessionCommandHandler>();
        services.AddScoped<GetOrderPaymentStatusQueryHandler>();
        services.AddScoped<GetPaymentTransactionStatusQueryHandler>();
        services.AddScoped<GetOrderPaymentDiagnosticsQueryHandler>();
        services.AddScoped<ListPaymentSessionInterventionsQueryHandler>();
        services.AddScoped<IPaymentInterventionNotifier, PaymentInterventionNotifier>();
        return services;
    }
}
