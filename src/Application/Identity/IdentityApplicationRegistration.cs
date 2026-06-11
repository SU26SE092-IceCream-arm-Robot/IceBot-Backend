using Application.Identity.Abstractions;
using Application.Identity.Authentication.Commands;
using Application.Identity.Authentication.Services;
using Application.Identity.CurrentAccount.Commands;
using Application.Identity.CurrentAccount.Queries;
using Application.Identity.InternalAccounts.Commands;
using Application.Identity.InternalAccounts.Queries;
using Application.Identity.Invitations.Commands;
using Application.Identity.Invitations.Services;
using Application.Identity.PasswordReset.Commands;
using Application.Identity.Roles.Queries;
using Application.Identity.Tokens.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Identity;

public static class IdentityApplicationRegistration
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        services.AddScoped<ListManagementRolesQueryHandler>();
        services.AddScoped<GetPermissionMatrixQueryHandler>();

        services.AddScoped<IAccountAuthenticationService, AccountAuthenticationService>();
        services.AddScoped<LoginAccountCommandHandler>();
        services.AddScoped<GoogleLoginCommandHandler>();
        services.AddScoped<RefreshAccessTokenCommandHandler>();
        services.AddScoped<RevokeRefreshTokenCommandHandler>();
        services.AddScoped<RevokeCurrentAccountTokensCommandHandler>();

        services.AddScoped<GetCurrentAccountQueryHandler>();
        services.AddScoped<UpdateCurrentAccountProfileCommandHandler>();
        services.AddScoped<ChangeCurrentAccountPasswordCommandHandler>();

        services.AddScoped<ListInternalAccountsQueryHandler>();
        services.AddScoped<GetInternalAccountQueryHandler>();
        services.AddScoped<CreateInternalAccountCommandHandler>();
        services.AddScoped<UpdateInternalAccountCommandHandler>();
        services.AddScoped<DisableInternalAccountCommandHandler>();
        services.AddScoped<SetInternalAccountPasswordCommandHandler>();
        services.AddScoped<AssignInternalAccountRoleCommandHandler>();
        services.AddScoped<UpdateInternalAccountRolesCommandHandler>();
        services.AddScoped<CreateInternalAccountInvitationCommandHandler>();
        services.AddScoped<GetInternalAccountEffectiveAccessQueryHandler>();
        services.AddScoped<AcceptInvitationCommandHandler>();
        services.AddScoped<GetCurrentAccountAccessQueryHandler>();

        services.AddScoped<RequestPasswordResetCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();

        services.AddScoped<RefreshTokenService>();
        services.AddScoped<AccountTokenService>();
        services.AddScoped<AccountInvitationService>();
        return services;
    }
}
