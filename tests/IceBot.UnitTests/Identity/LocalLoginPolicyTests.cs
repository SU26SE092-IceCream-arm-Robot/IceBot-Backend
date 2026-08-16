using Application.Identity.Abstractions;
using Application.Identity.Authentication.Requests;
using Application.Identity.Authentication.Services;
using Application.Identity.Tokens.Services;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class LocalLoginPolicyTests
{
    [Fact]
    public async Task Login_IgnoresLegacyTemporaryLockAndReturnsInvalidCredentialsForWrongPassword()
    {
        var accounts = Substitute.For<IIdentityAccountStore>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserName = "demo-orgadmin",
            Email = "demo-orgadmin@icebot.local",
            Status = AccountStatus.Active,
            LocalLoginEnabled = true,
            LockedUntil = DateTimeOffset.UtcNow.AddHours(1),
            FailedLoginCount = 5
        };
        accounts.GetByEmailOrUserNameAsync(account.Email, false, Arg.Any<CancellationToken>()).Returns(account);

        var service = CreateService(accounts, passwordHasher);
        var result = await service.LoginAsync(new LoginAccountRequest
        {
            EmailOrUsername = account.Email,
            Password = "wrong-password"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("Invalid credentials.", result.Message);
        Assert.Equal(5, account.FailedLoginCount);
        Assert.NotNull(account.LockedUntil);
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static AccountAuthenticationService CreateService(
        IIdentityAccountStore accounts,
        IPasswordHasher passwordHasher)
    {
        var refreshTokens = new RefreshTokenService(Substitute.For<IRefreshTokenStore>());
        var tokenService = new AccountTokenService(
            Substitute.For<IAccessTokenGenerator>(),
            refreshTokens,
            accounts);

        return new AccountAuthenticationService(
            accounts,
            tokenService,
            Substitute.For<ILogger<AccountAuthenticationService>>(),
            Substitute.For<IExternalIdentityProvider>(),
            passwordHasher);
    }
}
