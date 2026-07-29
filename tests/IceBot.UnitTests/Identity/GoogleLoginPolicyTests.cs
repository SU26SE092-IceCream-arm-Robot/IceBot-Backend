using Application.Identity.Abstractions;
using Application.Identity.Authentication.Requests;
using Application.Identity.Authentication.Services;
using Application.Identity.Tokens.Services;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class GoogleLoginPolicyTests
{
    [Fact]
    public async Task Login_DoesNotFallBackToPrimaryAccountEmail()
    {
        var accounts = Substitute.For<IIdentityAccountStore>();
        var externalAuth = Substitute.For<IExternalIdentityProvider>();
        externalAuth.ValidateTokenAsync("token", Arg.Any<CancellationToken>())
            .Returns(new ExternalAuthUser("primary@example.com", true, "google-subject", null, null));

        var service = CreateService(accounts, externalAuth);
        var result = await service.LoginWithExternalProviderAsync(new ExternalLoginRequest { IdToken = "token" });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await accounts.Received(1).GetByGoogleEmailAsync("primary@example.com", false, Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().GetByEmailOrUserNameAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_RejectsVerifiedEmailThatDiffersFromConfiguredGoogleEmail()
    {
        var accounts = Substitute.For<IIdentityAccountStore>();
        var externalAuth = Substitute.For<IExternalIdentityProvider>();
        var account = ActiveGoogleAccount("allowed@example.com", "google-subject");

        externalAuth.ValidateTokenAsync("token", Arg.Any<CancellationToken>())
            .Returns(new ExternalAuthUser("other@example.com", true, "google-subject", null, null));
        accounts.GetByGoogleSubjectIdAsync("google-subject", false, Arg.Any<CancellationToken>())
            .Returns(account);

        var service = CreateService(accounts, externalAuth);
        var result = await service.LoginWithExternalProviderAsync(new ExternalLoginRequest { IdToken = "token" });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Google account email is not allowed for this account.", result.Message);
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_RejectsDifferentSubjectForConfiguredGoogleEmail()
    {
        var accounts = Substitute.For<IIdentityAccountStore>();
        var externalAuth = Substitute.For<IExternalIdentityProvider>();
        var account = ActiveGoogleAccount("allowed@example.com", "bound-subject");

        externalAuth.ValidateTokenAsync("token", Arg.Any<CancellationToken>())
            .Returns(new ExternalAuthUser("allowed@example.com", true, "different-subject", null, null));
        accounts.GetByGoogleEmailAsync("allowed@example.com", false, Arg.Any<CancellationToken>())
            .Returns(account);

        var service = CreateService(accounts, externalAuth);
        var result = await service.LoginWithExternalProviderAsync(new ExternalLoginRequest { IdToken = "token" });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Google identity does not match the account binding.", result.Message);
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static AccountAuthenticationService CreateService(
        IIdentityAccountStore accounts,
        IExternalIdentityProvider externalAuth)
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
            externalAuth,
            Substitute.For<IPasswordHasher>());
    }

    private static Account ActiveGoogleAccount(string googleEmail, string googleSubjectId)
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            UserName = "internal-user",
            Email = "primary@example.com",
            Status = AccountStatus.Active,
            GoogleLoginEnabled = true,
            GoogleEmail = googleEmail,
            GoogleSubjectId = googleSubjectId
        };
    }
}
