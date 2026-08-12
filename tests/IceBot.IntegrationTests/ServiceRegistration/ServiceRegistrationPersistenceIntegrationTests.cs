using Application.ContentManagement;
using Application.Email;
using Application.ServiceRegistration;
using Application.ServiceRegistration.Abstractions;
using Domain.ContentManagement.Entities;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.ServiceRegistration.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.ServiceRegistration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.ServiceRegistration;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ServiceRegistrationPersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task PublishedPrivacy_SubmissionAndProvisioning_PersistOneTenantAndScopedOrgAdmin()
    {
        var actorId = Guid.NewGuid();
        await SeedActorAsync(actorId);
        var policyRevisionId = await CreatePublishedPrivacyPolicyAsync(actorId);
        var key = $"service-registration-{Guid.NewGuid():N}";

        await using var db = fixture.CreateDbContext();
        var store = new ServiceRegistrationStore(db);
        var service = new ServiceRegistrationService(store, new NoopProvisioner());
        var request = new SubmitServiceRegistrationRequest
        {
            ContactName = "Service owner",
            Email = $"owner-{Guid.NewGuid():N}@example.test",
            BusinessName = "Pilot kiosk",
            ExpectedLocationCount = 1,
            PrivacyPolicyRevisionId = policyRevisionId,
            PrivacyPolicyAccepted = true
        };

        var submitted = await service.SubmitAsync(key, request);
        var replayed = await service.SubmitAsync(key, request);
        Assert.Equal(201, submitted.StatusCode);
        Assert.Equal(submitted.Data!.Id, replayed.Data!.Id);

        var orgAdmin = await db.Roles.SingleOrDefaultAsync(x => x.Code == "OrgAdmin");
        if (orgAdmin is null)
        {
            orgAdmin = new Role { Code = "OrgAdmin", Name = "OrgAdmin", IsActive = true, IsSystemRole = true };
            db.Roles.Add(orgAdmin);
            await db.SaveChangesAsync();
        }

        var logger = new CaptureLogger();
        var provisioner = new ServiceRegistrationProvisioner(
            db,
            new RecordingEmailSender(),
            Options.Create(new EmailOptions { InvitationBaseUrl = "https://portal.example.test/invitations/accept" }),
            logger);
        var organizationCode = $"SR-{Guid.NewGuid():N}"[..20];
        var outcome = await provisioner.ProvisionAsync(submitted.Data.Id, actorId, new ServiceRegistrationProvisioningRequest
        {
            OrganizationCode = organizationCode,
            OrganizationName = "Pilot kiosk",
            AdminUserName = $"owner-{Guid.NewGuid():N}"[..24],
            AdminEmail = request.Email,
            AdminFullName = "Service owner",
            LocalLoginEnabled = true,
            ExpectedRevision = 1
        }, retry: false);

        Assert.True(outcome.Succeeded, $"{outcome.Message} {logger.Exception}");
        Assert.Equal(ServiceRegistrationStatus.Provisioned, outcome.Registration!.Status);
        var registration = await db.ServiceRegistrations.SingleAsync(x => x.Id == submitted.Data.Id);
        var account = await db.Accounts.SingleAsync(x => x.Id == registration.ProvisionedOrgAdminAccountId);
        var assignment = await db.AccountRoles.SingleAsync(x => x.AccountId == account.Id);

        Assert.NotNull(registration.ProvisionedOrganizationId);
        Assert.Equal(registration.ProvisionedOrganizationId, assignment.OrganizationId);
        Assert.Equal(orgAdmin.Id, assignment.RoleId);
        Assert.Equal(1, await db.AccountInvitations.CountAsync(x => x.AccountId == account.Id));
    }

    private async Task<Guid> CreatePublishedPrivacyPolicyAsync(Guid actorId)
    {
        await using var db = fixture.CreateDbContext();
        var page = ContentPage.Create($"privacy-policy-{Guid.NewGuid():N}", $"privacy-policy-{Guid.NewGuid():N}", "Privacy", "<p>Policy</p>", actorId, DateTimeOffset.UtcNow);
        db.ContentPages.Add(page);
        await db.SaveChangesAsync();

        var revision = page.Publish(actorId, page.Revision, DateTimeOffset.UtcNow);
        db.ContentPageRevisions.Add(revision);
        await db.SaveChangesAsync();
        return revision.Id;
    }

    private async Task SeedActorAsync(Guid actorId)
    {
        await using var db = fixture.CreateDbContext();
        db.Accounts.Add(new Account
        {
            Id = actorId,
            UserName = $"system-admin-{Guid.NewGuid():N}",
            Email = $"system-admin-{Guid.NewGuid():N}@example.test",
            FullName = "System administrator",
            Status = AccountStatus.Active
        });
        await db.SaveChangesAsync();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopProvisioner : IServiceRegistrationProvisioner
    {
        public Task<ServiceRegistrationProvisioningOutcome> ProvisionAsync(Guid registrationId, Guid actorId, ServiceRegistrationProvisioningRequest request, bool retry, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class CaptureLogger : ILogger<ServiceRegistrationProvisioner>
    {
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null) Exception = exception;
        }
    }
}
