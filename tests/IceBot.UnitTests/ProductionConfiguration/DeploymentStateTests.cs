using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class DeploymentStateTests
{
    [Fact]
    public void CreatePending_RejectsLegacyValidationEvidence()
    {
        Assert.Throws<DomainRuleException>(() => KioskConfigurationDeployment.CreatePending(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new string('d', 64), 1, Guid.NewGuid().ToString("N"), DateTimeOffset.UtcNow,
            null, "legacy", "Legacy", "[]"));
    }

    [Fact]
    public void DuplicateInstalledReport_IsIdempotent()
    {
        var deployment = CreatePending();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var firstChanged = deployment.MarkInstalled(eventId, now, now);
        var duplicateChanged = deployment.MarkInstalled(eventId, now, now.AddSeconds(1));

        Assert.True(firstChanged);
        Assert.False(duplicateChanged);
        Assert.Equal(KioskConfigurationDeploymentStatus.Installed, deployment.Status);
        Assert.Equal(now, deployment.CloudReceivedAt);
    }

    [Fact]
    public void CommandTimeout_FailsPendingDeploymentAndCannotBeAppliedTwice()
    {
        var deployment = CreatePending();
        var observedAt = DateTimeOffset.UtcNow;

        deployment.MarkCommandExpired(observedAt);

        Assert.Equal(KioskConfigurationDeploymentStatus.Failed, deployment.Status);
        Assert.Equal("CommandExpired", deployment.FailureCode);
        Assert.Throws<DomainRuleException>(() => deployment.MarkCommandExpired(observedAt.AddMinutes(1)));
    }

    private static KioskConfigurationDeployment CreatePending() =>
        KioskConfigurationDeployment.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new string('d', 64),
            1,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            null,
            "validation-checksum",
            "UnprovenPhysicalBehavior",
            "[]");
}
