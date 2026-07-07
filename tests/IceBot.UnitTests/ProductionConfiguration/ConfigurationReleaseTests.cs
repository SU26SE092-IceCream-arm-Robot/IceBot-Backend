using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.ProductionConfiguration.Manifests;
using Application.ProductionConfiguration.Abstractions;
using Application.ProductionConfiguration.Commands;
using Application.ProductionConfiguration.Services;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration;
using Microsoft.Extensions.Options;
using Application.RobotConfiguration.Abstractions;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.ProductionConfiguration;

public sealed class ConfigurationReleaseTests
{
    [Fact]
    public void Publish_RejectsReleaseWithoutRoutes()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);

        var exception = Assert.Throws<DomainRuleException>(() =>
            release.Publish(DateTimeOffset.UtcNow, Guid.NewGuid(),
                new Dictionary<Guid, PublishedRobotProgramSnapshot>(), Bundle()));

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
                new Dictionary<Guid, PublishedRobotProgramSnapshot>(), Bundle()));

        Assert.Equal("Configuration release bindings require published robot program snapshots.", exception.Message);
        Assert.Equal(ConfigurationReleaseStatus.Draft, release.Status);
    }

    [Fact]
    public void ContentManifestIsSelfContainedAndExcludesBundleDescriptor()
    {
        var organizationId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var release = ConfigurationRelease.CreateDraft(organizationId, 1);
        release.ReplaceRoutes(
        [
            (
                Guid.NewGuid(),
                Guid.NewGuid(),
                "DEFAULT",
                0,
                null,
                (IReadOnlyCollection<(Guid RobotProgramId, int BindingOrder, string CapabilityCode)>)
                [(programId, 1, "ROBOT_ARM")]
            )
        ]);
        var snapshots = new Dictionary<Guid, PublishedRobotProgramSnapshot>
        {
            [programId] = new(
                programId,
                "MAKE",
                organizationId,
                1,
                new string('b', 64),
                [new PublishedRobotArtifactSnapshot(
                    Guid.NewGuid(), artifactId, 1, 1, null, new string('c', 64),
                    "robot-artifacts/source.lua", "FAIRINO_LUA_V1", "FR5", 123)])
        };

        var content = release.PreparePublication(Guid.NewGuid(), snapshots);

        Assert.Contains("\"ExecutionRoutes\"", content.Json);
        Assert.Contains($"\"BundleEntryName\":\"artifacts/{artifactId:D}.lua\"", content.Json);
        Assert.Contains("\"ContentLengthBytes\":123", content.Json);
        Assert.DoesNotContain("FullEdgeBundle", content.Json);
    }

    [Fact]
    public async Task InvalidReleaseIsRejectedBeforeObjectStorageIo()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        var store = Substitute.For<IProductionConfigurationStore>();
        store.GetReleaseForPublishAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var handler = new PublishConfigurationReleaseCommandHandler(
            store,
            new FullEdgeReleaseBundleService(storage),
            ReadinessGuard());

        var result = await handler.HandleAsync(new PublishConfigurationReleaseCommand
        {
            ReleaseId = release.Id,
            OrganizationId = release.OrganizationId,
            UserContext = TestData.SystemAdmin()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        await storage.DidNotReceive().ReadBytesAsync(
            Arg.Any<string>(),
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
        await storage.DidNotReceive().WriteImmutableAsync(
            Arg.Any<ArtifactObjectWriteRequest>(),
            Arg.Any<Stream>(),
            Arg.Any<CancellationToken>());
    }

    private static FullEdgeReleaseBundleDescriptor Bundle() =>
        new(1, "robot-artifacts/release-bundles/test.zip", new string('a', 64), 100, 1);

    private static ProductionInventoryReadinessGuard ReadinessGuard() => new(
        Substitute.For<IInventoryReadinessEvaluator>(),
        Options.Create(new InventoryReadinessPolicyOptions()));
}
