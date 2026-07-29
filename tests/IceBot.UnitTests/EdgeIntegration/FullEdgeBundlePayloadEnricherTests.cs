using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.Text.Json;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using NSubstitute;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class FullEdgeBundlePayloadEnricherTests
{
    [Fact]
    public async Task InvalidDeploymentPayloadIsRejectedBeforeDelivery()
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        var command = EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "{invalid-json",
            DateTimeOffset.UtcNow,
            deploymentId: Guid.NewGuid(),
            deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);

        var exception = await Assert.ThrowsAsync<InvalidArtifactCommandPayloadException>(
            () => new ArtifactCommandPayloadEnricher(storage).EnrichAsync(command));

        Assert.Contains(command.Id.ToString(), exception.Message, StringComparison.Ordinal);
        await storage.DidNotReceive().CreateReadUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FullEdgeDeploymentGetsBundleAndIndividualArtifactDownloadUrls()
    {
        var storage = Substitute.For<IArtifactObjectStorage>();
        storage.CreateReadUrlAsync("robot-artifacts/release-bundles/release.zip", Arg.Any<CancellationToken>())
            .Returns(new ArtifactObjectReadUrlResult("https://storage.test/release.zip", DateTimeOffset.UtcNow.AddMinutes(5)));
        storage.CreateReadUrlAsync("robot-artifacts/source.lua", Arg.Any<CancellationToken>())
            .Returns(new ArtifactObjectReadUrlResult("https://storage.test/source.lua", DateTimeOffset.UtcNow.AddMinutes(5)));
        var payload = JsonSerializer.Serialize(new
        {
            FullEdgeBundle = new { StorageKey = "robot-artifacts/release-bundles/release.zip", Checksum = "bundle" },
            Artifacts = new[] { new { StorageKey = "robot-artifacts/source.lua", ArtifactChecksum = "artifact" } }
        });
        var command = EdgeCommand.Create(
            EdgeCommandType.DeployConfiguration,
            Guid.NewGuid(),
            Guid.NewGuid(),
            payload,
            DateTimeOffset.UtcNow,
            deploymentId: Guid.NewGuid(),
            deploymentKind: DeploymentCommandTargetKind.FullEdgeConfiguration);

        var enriched = await new ArtifactCommandPayloadEnricher(storage).EnrichAsync(command);

        using var document = JsonDocument.Parse(enriched);
        Assert.Equal(
            "https://storage.test/release.zip",
            document.RootElement.GetProperty("FullEdgeBundle").GetProperty("DownloadUrl").GetString());
        Assert.Equal(
            "https://storage.test/source.lua",
            document.RootElement.GetProperty("Artifacts")[0].GetProperty("DownloadUrl").GetString());
        await storage.Received(2).CreateReadUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
