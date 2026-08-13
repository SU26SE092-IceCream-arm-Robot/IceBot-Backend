using Application.Identity.Abstractions;
using Application.Identity.NotificationDevices.Abstractions;
using Application.Identity.NotificationDevices.Delivery;
using Infrastructure.Firebase;
using Infrastructure.Identity.Bootstrap;
using Infrastructure.Identity.ExternalAuth;
using Infrastructure.Identity.Persistence;
using Infrastructure.Identity.Security;
using Infrastructure.Identity.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity;

public static class IdentityInfrastructureRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IIdentityAccountStore, IdentityAccountStore>();
        services.AddScoped<IAccountNotificationDeviceStore, AccountNotificationDeviceStore>();
        services.AddScoped<IAccountPushNotificationSender, FirebaseAccountPushNotificationSender>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IPasswordResetRequestStore, PasswordResetRequestStore>();
        services.AddScoped<IAccountInvitationStore, AccountInvitationStore>();
        services.AddScoped<StaffSessionRevocationReconciler>();
        services.AddOptions<StaffSessionRevocationReconciliationOptions>()
            .Bind(config.GetSection(StaffSessionRevocationReconciliationOptions.SectionName))
            .Validate(options => options.IntervalSeconds is >= 5 and <= 3600 && options.BatchSize is >= 1 and <= 500,
                "Staff session revocation reconciliation settings are invalid.")
            .ValidateOnStart();
        services.AddSingleton<IFirebaseClient, FirebaseClient>();
        services.AddOptions<FirebaseAuthResilienceOptions>()
            .Bind(config.GetSection(FirebaseAuthResilienceOptions.SectionName))
            .Validate(options =>
                    options.OperationTimeoutSeconds is >= 1 and <= 120 &&
                    options.RetryCount is >= 0 and <= 3 &&
                    options.RetryDelayMilliseconds is >= 1 and <= 10000 &&
                    options.CircuitBreakerFailureRatio is > 0 and <= 1 &&
                    options.CircuitBreakerMinimumThroughput >= 2 &&
                    options.CircuitBreakerSamplingDurationSeconds >= 1 &&
                    options.CircuitBreakerBreakDurationSeconds >= 1,
                "Firebase resilience settings are invalid.")
            .ValidateOnStart();
        services.AddOptions<FirebasePushDeliveryOptions>()
            .Bind(config.GetSection(FirebasePushDeliveryOptions.SectionName))
            .Validate(options => options.OperationTimeoutSeconds is >= 1 and <= 120,
                "Firebase push delivery operation timeout must be between 1 and 120 seconds.")
            .Validate(options => options.OperationTimeoutSeconds <
                config.GetValue<int>("NotificationDelivery:ProcessingTimeoutSeconds", 120),
                "Firebase push delivery operation timeout must be lower than the notification delivery processing timeout.")
            .ValidateOnStart();
        services.AddSingleton<FirebaseAuthResiliencePipeline>();
        services.AddSingleton<FirebasePushDeliveryTimeoutPolicy>();
        services.AddSingleton<IExternalIdentityProvider, FirebaseExternalIdentityProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddHostedService<IdentityBootstrapHostedService>();
        services.AddHostedService<StaffSessionRevocationReconciliationJob>();
        return services;
    }
}
