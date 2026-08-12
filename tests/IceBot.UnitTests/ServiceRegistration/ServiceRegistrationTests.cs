using Domain.Common;
using Domain.ServiceRegistration.Entities;
using Domain.ServiceRegistration.Enums;
using ServiceRegistrationEntity = Domain.ServiceRegistration.Entities.ServiceRegistration;

namespace IceBot.UnitTests.ServiceRegistration;

public sealed class ServiceRegistrationTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public void Lifecycle_Rejects_Invalid_And_Stale_Transitions()
    {
        var registration = NewRegistration();

        registration.StartReview(ActorId, registration.Revision, DateTimeOffset.UtcNow);
        Assert.Equal(ServiceRegistrationStatus.UnderReview, registration.Status);
        Assert.Throws<DomainRuleException>(() => registration.StartReview(ActorId, registration.Revision, DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => registration.Reject(ActorId, "No longer needed.", 1, DateTimeOffset.UtcNow));

        registration.Reject(ActorId, "No longer needed.", registration.Revision, DateTimeOffset.UtcNow);
        Assert.Equal(ServiceRegistrationStatus.Rejected, registration.Status);
        Assert.Throws<DomainRuleException>(() => registration.BeginProvisioning(ActorId, "{}", registration.Revision, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Failed_Provisioning_Can_Retry_Without_Changing_Approved_Snapshot()
    {
        var registration = NewRegistration();
        registration.BeginProvisioning(ActorId, "{\"organizationCode\":\"DEMO\"}", registration.Revision, DateTimeOffset.UtcNow);
        registration.RecordProvisioningFailure("PROVISIONING_CONFLICT", "Organization code is already used.", DateTimeOffset.UtcNow);

        registration.BeginProvisioning(ActorId, "{\"organizationCode\":\"DEMO\"}", registration.Revision, DateTimeOffset.UtcNow, retry: true);
        registration.CompleteProvisioning(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(ServiceRegistrationStatus.Provisioned, registration.Status);
        Assert.Null(registration.ProvisioningFailureCode);
        Assert.Throws<DomainRuleException>(() => registration.BeginProvisioning(ActorId, "{}", registration.Revision, DateTimeOffset.UtcNow, retry: true));
    }

    private static ServiceRegistrationEntity NewRegistration() => ServiceRegistrationEntity.Submit(
        "SR-2026-TEST", "registration-test-key", new string('A', 64), "Contact", "contact@example.test", "contact@example.test",
        null, null, "Example business", null, null, null, 1, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
}
