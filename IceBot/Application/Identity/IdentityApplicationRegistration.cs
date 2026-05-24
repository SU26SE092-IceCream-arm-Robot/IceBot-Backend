using Application.Identity.Abstractions;
using Application.Identity.Authentication.Services;
using Application.Identity.CurrentAccount.Services;
using Application.Identity.InternalAccounts.Services;
using Application.Identity.Tokens.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Identity;

public static class IdentityApplicationRegistration
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<CurrentAccountService>();
        services.AddScoped<InternalAccountService>();
        services.AddScoped<RefreshTokenService>();
        services.AddScoped<AccountTokenService>();
        return services;
    }
}
