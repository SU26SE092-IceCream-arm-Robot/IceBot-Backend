using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.ValueObjects;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ConfigurationReleaseTests
{
    [Fact]
    public void Publish_RejectsReleaseWithoutRoutes()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainRuleException>(() =>
            release.Publish(DateTimeOffset.UtcNow, Guid.NewGuid(),
                new Dictionary<Guid, PublishedRobotProgramSnapshot>()));

        Assert.Equal("Cannot publish a configuration release without execution routes.", exception.Message);
        Assert.Equal(ConfigurationReleaseStatus.Draft, release.Status);
    }

    [Fact]
    public void Publish_RejectsBindingWithoutPublishedProgramSnapshot()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        release.ReplaceRoutes(
        [
            (
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEFAULT",
                0,
                null,
                (IReadOnlyCollection<(Guid RobotProgramId, int BindingOrder, string CapabilityCode)>)
                [
                    (Guid.NewGuid(), 1, "ROBOT_ARM")
                ])
        ]);

        var exception = Assert.Throws<DomainRuleException>(() =>
            release.Publish(DateTimeOffset.UtcNow, Guid.NewGuid(),
                new Dictionary<Guid, PublishedRobotProgramSnapshot>()));

        Assert.Equal("Configuration release bindings require published robot program snapshots.", exception.Message);
        Assert.Equal(ConfigurationReleaseStatus.Draft, release.Status);
    }
}
