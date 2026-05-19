using Application.Identity.Abstractions;
using Infrastructure.Firebase;
using Infrastructure.Identity.Bootstrap;
using Infrastructure.Identity.ExternalAuth;
using Infrastructure.Identity.Persistence;
using Infrastructure.Identity.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Identity;

public static class IdentityInfrastructureRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IIdentityAccountStore, IdentityAccountStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IFirebaseClient, FirebaseClient>();
        services.AddSingleton<IExternalIdentityProvider, FirebaseExternalIdentityProvider>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddHostedService<IdentityBootstrapHostedService>();
        return services;
    }
}
