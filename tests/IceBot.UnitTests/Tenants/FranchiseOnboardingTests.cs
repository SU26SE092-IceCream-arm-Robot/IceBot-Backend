using Domain.Common;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace IceBot.UnitTests.Tenants;

public sealed class FranchiseOnboardingTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly string Checksum = new('A', 64);

    [Fact]
    public void Checkpoints_Require_Order_And_End_At_Review()
    {
        var workflow = FranchiseOnboarding.Start(
            OrganizationId, "setup-1", Checksum, "{}", ActorId, DateTimeOffset.UtcNow);

        workflow.MarkRunning(DateTimeOffset.UtcNow);
        Assert.Throws<DomainRuleException>(() => workflow.RecordKiosk(Guid.NewGuid()));

        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var installationId = Guid.NewGuid();
        workflow.RecordStore(storeId);
        workflow.RecordKiosk(kioskId);
        workflow.RecordPackageInstallation(installationId);
        workflow.MarkReady(DateTimeOffset.UtcNow);

        Assert.Equal(FranchiseOnboardingStatus.ReadyForActivation, workflow.Status);
        Assert.Equal(storeId, workflow.StoreId);
        Assert.Equal(kioskId, workflow.KioskId);
        Assert.Equal(installationId, workflow.PackageInstallationId);
        Assert.Throws<DomainRuleException>(() => workflow.Cancel("changed mind", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Failed_Workflow_Can_Be_Resumed_Without_Losing_Checkpoints()
    {
        var workflow = FranchiseOnboarding.Start(
            OrganizationId, "setup-2", Checksum, "{}", ActorId, DateTimeOffset.UtcNow);
        var storeId = Guid.NewGuid();

        workflow.MarkRunning(DateTimeOffset.UtcNow);
        workflow.RecordStore(storeId);
        workflow.MarkFailed("KIOSK_FAILED", "Kiosk provisioning failed.");
        workflow.MarkRunning(DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.Equal(FranchiseOnboardingStatus.Running, workflow.Status);
        Assert.Equal(storeId, workflow.StoreId);
        Assert.Null(workflow.FailureCode);
    }

    [Fact]
    public void Cancelled_Workflow_Cannot_Resume()
    {
        var workflow = FranchiseOnboarding.Start(
            OrganizationId, "setup-3", Checksum, "{}", ActorId, DateTimeOffset.UtcNow);
        workflow.Cancel("Operator cancelled setup.", DateTimeOffset.UtcNow);

        Assert.Equal(FranchiseOnboardingStatus.Cancelled, workflow.Status);
        Assert.Throws<DomainRuleException>(() => workflow.MarkRunning(DateTimeOffset.UtcNow));
    }
}
