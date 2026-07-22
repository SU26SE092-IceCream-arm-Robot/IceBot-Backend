using Application.Identity.Abstractions;
using Application.Identity.Invitations.Commands;
using Application.Identity.Invitations.Requests;
using Application.Identity.Invitations.Results;
using Application.Identity.NotificationDevices.Abstractions;
using Application.Identity.NotificationDevices.Commands;
using Application.Identity.NotificationDevices.Requests;
using Application.Identity.NotificationDevices.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using NSubstitute;
using System.Security.Cryptography;
using System.Text;

namespace IceBot.UnitTests.Identity;

public sealed class IdentityLifecycleHardeningTests
{
    [Fact]
    public async Task RefreshTokenRotation_RevokesSession_WhenAccountIsNotActive()
    {
        var accountId = Guid.NewGuid();
        var token = new RefreshToken
        {
            AccountId = accountId,
            TokenHash = "HASH",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var store = Substitute.For<IRefreshTokenStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<Task<(bool Ok, RefreshTokenIssue? NewToken, string? Error)>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<(bool, RefreshTokenIssue?, string?)>>>(0)());
        store.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(token);
        store.GetAccountStatusAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(AccountStatus.Disabled);
        store.ListActiveByAccountIdAsync(accountId, Arg.Any<CancellationToken>())
            .Returns([token]);
        var service = new RefreshTokenService(store);

        var result = await service.RotateAsync("raw-token", "127.0.0.1", "unit-test");

        Assert.False(result.Ok);
        Assert.Null(result.NewToken);
        Assert.Equal("Account is not active.", result.Error);
        Assert.NotNull(token.RevokedAt);
        Assert.Equal("Account is not active", token.RevokeReason);
        await store.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            store.GetByTokenHashAsync(Arg.Any<string>(), true, Arg.Any<CancellationToken>());
            store.AcquireAccountSessionLockAsync(accountId, Arg.Any<CancellationToken>());
            store.AcquireTokenLockAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            store.GetByTokenHashAsync(Arg.Any<string>(), false, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task AcceptInvitation_UsesSerializedTransaction_AndRevokesExistingSessions()
    {
        const string rawToken = "invitation-token";
        var account = new Account
        {
            Status = AccountStatus.Invited,
            GoogleLoginEnabled = true,
            Email = "owner@example.com",
            UserName = "owner"
        };
        var invitation = new AccountInvitation
        {
            AccountId = account.Id,
            Account = account,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            InvitedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        var invitations = Substitute.For<IAccountInvitationStore>();
        invitations.ExecuteAcceptanceTransactionAsync(
                invitation.TokenHash,
                Arg.Any<Func<CancellationToken, Task<ApiResult<AcceptInvitationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<ApiResult<AcceptInvitationResult>>>>(1)(
                call.ArgAt<CancellationToken>(2)));
        invitations.GetByTokenHashAsync(invitation.TokenHash, false, Arg.Any<CancellationToken>())
            .Returns(invitation);
        var refreshStore = Substitute.For<IRefreshTokenStore>();
        refreshStore.ExecuteInTransactionAsync(Arg.Any<Func<Task<int>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<int>>>(0)());
        refreshStore.ListActiveByAccountIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns([]);
        var handler = new AcceptInvitationCommandHandler(
            invitations,
            Substitute.For<IPasswordHasher>(),
            new RefreshTokenService(refreshStore));

        var result = await handler.HandleAsync(new AcceptInvitationCommand
        {
            Request = new AcceptInvitationRequest { Token = rawToken }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(AccountStatus.Active, account.Status);
        Assert.NotNull(invitation.AcceptedAt);
        await refreshStore.Received(1).AcquireAccountSessionLockAsync(
            account.Id, Arg.Any<CancellationToken>());
        await invitations.Received(1).ExecuteAcceptanceTransactionAsync(
            invitation.TokenHash,
            Arg.Any<Func<CancellationToken, Task<ApiResult<AcceptInvitationResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterNotificationDevice_ReassignmentClearsPreviousRawToken()
    {
        var account = new Account { Status = AccountStatus.Active };
        var installationId = Guid.NewGuid();
        var previous = new AccountNotificationDevice
        {
            AccountId = Guid.NewGuid(),
            InstallationId = Guid.NewGuid(),
            PushToken = "same-fcm-token",
            PushTokenHash = "OLD_HASH"
        };
        var accounts = Substitute.For<IIdentityAccountStore>();
        accounts.GetByIdAsync(account.Id, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(account);
        var devices = Substitute.For<IAccountNotificationDeviceStore>();
        devices.ExecuteRegistrationTransactionAsync(
                account.Id,
                installationId,
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<AccountNotificationDeviceResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<ApiResult<AccountNotificationDeviceResult>>>>(3)(
                call.ArgAt<CancellationToken>(4)));
        devices.GetByAccountAndInstallationAsync(account.Id, installationId, false, Arg.Any<CancellationToken>())
            .Returns((AccountNotificationDevice?)null);
        devices.GetActiveByPushTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(previous);
        var handler = new RegisterCurrentAccountNotificationDeviceCommandHandler(accounts, devices);

        var result = await handler.HandleAsync(new RegisterCurrentAccountNotificationDeviceCommand
        {
            AccountId = account.Id,
            InstallationId = installationId,
            Request = new RegisterCurrentAccountNotificationDeviceRequest
            {
                Platform = "android",
                PushToken = "same-fcm-token"
            }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Null(previous.PushToken);
        Assert.NotNull(previous.InvalidatedAt);
        Assert.Equal("TokenReassigned", previous.InvalidationReason);
    }

    [Fact]
    public void NotificationDeviceInvalidation_RemovesProviderCredentialButKeepsHashEvidence()
    {
        var device = new AccountNotificationDevice
        {
            PushToken = "provider-secret",
            PushTokenHash = "TOKEN_HASH"
        };

        device.Invalidate("Unregistered", DateTimeOffset.UtcNow);

        Assert.Null(device.PushToken);
        Assert.Equal("TOKEN_HASH", device.PushTokenHash);
        Assert.Equal("Unregistered", device.InvalidationReason);
    }
}
