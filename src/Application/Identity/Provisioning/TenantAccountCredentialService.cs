using System.Net;
using System.Security.Cryptography;
using Application.Email;
using Application.Identity.Abstractions;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Identity.Provisioning;

public sealed record TenantAccountCredentials(string TemporaryPassword);

// Temporary demo policy. The retained target onboarding flow uses AccountInvitationService
// so management never knows the user's password. Remove this service from create handlers
// when invitation onboarding and first-login credential setup are restored.
public sealed class TenantAccountCredentialService(
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ILogger<TenantAccountCredentialService> logger)
{
    public TenantAccountCredentials Prepare(Account account, DateTimeOffset now)
    {
        var temporaryPassword = $"{Convert.ToHexString(RandomNumberGenerator.GetBytes(8))}a!";
        account.Status = AccountStatus.Active;
        account.LocalLoginEnabled = true;
        account.Password = HashedPassword.From(passwordHasher.HashPassword(temporaryPassword));
        account.EmailConfirmed = true;
        account.EmailConfirmedAt = now;
        return new TenantAccountCredentials(temporaryPassword);
    }

    public async Task<bool> TrySendAsync(
        Account account,
        TenantAccountCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await emailSender.SendAsync(
                account.Email,
                "Your IceBot account",
                BuildEmail(account, credentials.TemporaryPassword),
                cancellationToken);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Failed to send initial credentials for account {AccountId}", account.Id);
            return false;
        }
    }

    private static string BuildEmail(Account account, string temporaryPassword) =>
        $"<p>Your IceBot account is ready.</p>" +
        $"<p>Username: <strong>{WebUtility.HtmlEncode(account.UserName)}</strong><br/>" +
        $"Email: <strong>{WebUtility.HtmlEncode(account.Email)}</strong><br/>" +
        $"Temporary password: <strong>{WebUtility.HtmlEncode(temporaryPassword)}</strong></p>" +
        "<p>You can change this password after signing in.</p>";
}
