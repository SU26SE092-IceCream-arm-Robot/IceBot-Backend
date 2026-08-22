using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionConfiguration.ValueObjects;
using Domain.ProductionConfiguration.Manifests;
using Application.ProductionConfiguration.Releases.Abstractions;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.Inventory.Abstractions;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Readiness;
using Microsoft.Extensions.Options;
using Application.RobotConfiguration.Artifacts.Abstractions;
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
                Array.Empty<string>(),
                (IReadOnlyCollection<(Guid ProductionProgramBindingId, string ProductionProgramBindingChecksum,
                    Guid RobotProgramId, int BindingOrder, IReadOnlyCollection<string> CapabilityCodes)>)
                [
                    (Guid.NewGuid(), new string('a', 64), Guid.NewGuid(), 1, ["ROBOT_ARM"])
                ])
        ]);

        var exception = Assert.Throws<DomainRuleException>(() =>
            release.Publish(DateTimeOffset.UtcNow, Guid.NewGuid(),
                new Dictionary<Guid, PublishedRobotProgramSnapshot>()));

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
                new[] { "oreo" },
                (IReadOnlyCollection<(Guid ProductionProgramBindingId, string ProductionProgramBindingChecksum,
                    Guid RobotProgramId, int BindingOrder, IReadOnlyCollection<string> CapabilityCodes)>)
                [(Guid.NewGuid(), new string('a', 64), programId, 1, ["ROBOT_ARM"])]
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
        Assert.Contains("\"SupportedOptionCodes\":[\"OREO\"]", content.Json);
        Assert.DoesNotContain("FullEdgeBundle", content.Json);
    }

    [Fact]
    public async Task InvalidReleaseIsRejectedBeforeObjectStorageIo()
    {
        var release = ConfigurationRelease.CreateDraft(Guid.NewGuid(), 1);
        var store = Substitute.For<IConfigurationReleaseStore>();
        store.GetReleaseForPublishAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        var storage = Substitute.For<IArtifactObjectStorage>();
        var handler = new PublishConfigurationReleaseCommandHandler(
            store,
            ReadinessGuard(),
            new ProductionDefinitionPublicationService());

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

    private static ProductionInventoryReadinessGuard ReadinessGuard() => new(
        Substitute.For<IInventoryReadinessEvaluator>(),
        Options.Create(new InventoryReadinessPolicyOptions()));
}
