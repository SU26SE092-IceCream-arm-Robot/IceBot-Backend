using Application.Identity.Abstractions;
using Infrastructure.Firebase;
using Infrastructure.Identity.Bootstrap;
using Infrastructure.Identity.ExternalAuth;
using Infrastructure.Identity.Persistence;
using Infrastructure.Identity.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity;

public static class IdentityInfrastructureRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IIdentityAccountStore, IdentityAccountStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IPasswordResetRequestStore, PasswordResetRequestStore>();
        services.AddScoped<IAccountInvitationStore, AccountInvitationStore>();
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
        services.AddSingleton<FirebaseAuthResiliencePipeline>();
        services.AddSingleton<IExternalIdentityProvider, FirebaseExternalIdentityProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddHostedService<IdentityBootstrapHostedService>();
        return services;
    }
}
