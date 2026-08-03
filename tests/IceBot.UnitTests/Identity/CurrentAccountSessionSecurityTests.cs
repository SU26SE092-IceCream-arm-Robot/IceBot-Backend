using Application.Identity.Abstractions;
using Application.Identity.CurrentAccount.Commands;
using Application.Identity.CurrentAccount.Queries;
using Application.Identity.CurrentAccount.Requests;
using Application.Identity.Tokens.Services;
using Domain.Identity.Entities;
using Domain.Identity.ValueObjects;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class CurrentAccountSessionSecurityTests
{
    [Fact]
    public async Task ChangePassword_UsesOneAccountTransactionForPasswordAndSessionRevocation()
    {
        var account = new Account
        {
            LocalLoginEnabled = true,
            Password = HashedPassword.From("CURRENT_HASH")
        };
        var accounts = Substitute.For<IIdentityAccountStore>();
        accounts.ExecuteInTransactionAsync(
                Arg.Any<Func<Task<Application.Shared.Wrappers.ApiResult<bool>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<Application.Shared.Wrappers.ApiResult<bool>>>>(0)());
        accounts.GetByIdAsync(account.Id, false, Arg.Any<CancellationToken>()).Returns(account);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.VerifyPassword("current-password", "CURRENT_HASH").Returns(true);
        passwordHasher.HashPassword("new-password").Returns("NEW_HASH");

        var refreshTokens = Substitute.For<IRefreshTokenStore>();
        refreshTokens.ExecuteInTransactionAsync(Arg.Any<Func<Task<int>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<int>>>(0)());
        refreshTokens.ListActiveByAccountIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns([]);

        var handler = new ChangeCurrentAccountPasswordCommandHandler(
            accounts,
            passwordHasher,
            new RefreshTokenService(refreshTokens));

        var result = await handler.HandleAsync(new ChangeCurrentAccountPasswordCommand
        {
            AccountId = account.Id,
            Request = new ChangeCurrentAccountPasswordRequest
            {
                CurrentPassword = "current-password",
                NewPassword = "new-password"
            },
            IpAddress = "127.0.0.1",
            UserAgent = "unit-test"
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("NEW_HASH", account.Password!.Value);
        await accounts.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<Task<Application.Shared.Wrappers.ApiResult<bool>>>>(),
            Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await refreshTokens.Received(1).AcquireAccountSessionLockAsync(account.Id, Arg.Any<CancellationToken>());
        await refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListSessions_ReturnsOnlyActiveSessionsWithoutRefreshTokenSecret()
    {
        var accountId = Guid.NewGuid();
        var newest = new RefreshToken
        {
            AccountId = accountId,
            TokenHash = "MUST_NOT_BE_EXPOSED",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = "10.0.0.2",
            CreatedByUserAgent = "Browser B"
        };
        var oldest = new RefreshToken
        {
            AccountId = accountId,
            TokenHash = "MUST_NOT_BE_EXPOSED_EITHER",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = "10.0.0.1",
            CreatedByUserAgent = "Browser A"
        };
        var refreshTokens = Substitute.For<IRefreshTokenStore>();
        refreshTokens.ListActiveByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns([oldest, newest]);
        var handler = new ListCurrentAccountSessionsQueryHandler(refreshTokens);

        var result = await handler.HandleAsync(new ListCurrentAccountSessionsQuery(accountId));

        Assert.True(result.Succeeded, result.Message);
        Assert.Collection(result.Data!,
            session =>
            {
                Assert.Equal(newest.Id, session.SessionId);
                Assert.Equal("10.0.0.2", session.IpAddress);
                Assert.Equal("Browser B", session.UserAgent);
            },
            session => Assert.Equal(oldest.Id, session.SessionId));
    }
}
