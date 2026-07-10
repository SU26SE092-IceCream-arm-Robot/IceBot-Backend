using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Mapping;
using Application.RobotConfiguration.Programs.Results;
using Application.RobotConfiguration.Programs.Queries;
using Application.RobotConfiguration.Programs.Commands;
using Domain.RobotConfiguration.Programs;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.RobotConfiguration.Artifacts;
using Domain.Tenants.Enums;
using Domain.RobotConfiguration.Programs.Manifests;
using IceBot.UnitTests.TestSupport;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotProgramManifestTests
{
    [Fact]
    public void Publish_SerializesArtifactsByRunOrderAndCreatesMatchingChecksum()
    {
        var organizationId = Guid.NewGuid();
        var firstArtifact = TestData.PublishedArtifact(organizationId, "FIRST", "first.lua", 'a');
        var secondArtifact = TestData.PublishedArtifact(organizationId, "SECOND", "second.lua", 'b');
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM",
            "Make ice cream",
            TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(secondArtifact.Id, 20, "{\"speed\":2}");
        program.AddArtifact(firstArtifact.Id, 10, "{\"speed\":1}");

        program.Publish(DateTimeOffset.UtcNow,
        [
            Snapshot(firstArtifact),
            Snapshot(secondArtifact)
        ]);

        using var manifest = JsonDocument.Parse(program.ProgramManifestJson!);
        var artifacts = manifest.RootElement.GetProperty("Artifacts").EnumerateArray().ToArray();
        Assert.Equal(10, artifacts[0].GetProperty("RunOrder").GetInt32());
        Assert.Equal(firstArtifact.Id, artifacts[0].GetProperty("RobotArtifact").GetProperty("Id").GetGuid());
        Assert.Equal(20, artifacts[1].GetProperty("RunOrder").GetInt32());
        var expectedChecksum = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(program.ProgramManifestJson!))).ToLowerInvariant();
        Assert.Equal(expectedChecksum, program.ProgramManifestChecksum);
    }

    [Fact]
    public void Publish_RejectsSnapshotThatDoesNotMatchProgramMembership()
    {
        var organizationId = Guid.NewGuid();
        var assignedArtifact = TestData.PublishedArtifact(organizationId, "ASSIGNED", "assigned.lua", 'a');
        var unrelatedArtifact = TestData.PublishedArtifact(organizationId, "OTHER", "other.lua", 'b');
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM",
            "Make ice cream",
            TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(assignedArtifact.Id, 10);

        var exception = Assert.Throws<Domain.Common.DomainRuleException>(() =>
            program.Publish(DateTimeOffset.UtcNow, [Snapshot(unrelatedArtifact)]));

        Assert.Equal(
            "Robot program publication requires published robot artifact snapshots.",
            exception.Message);
    }

    [Fact]
    public void Parse_ReturnsTypedPublishedManifest()
    {
        var organizationId = Guid.NewGuid();
        var artifact = TestData.PublishedArtifact(organizationId, "ARTIFACT", "artifact.lua", 'c');
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM",
            "Make ice cream",
            TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(artifact.Id, 10, "{\"speed\":2}");
        program.Publish(DateTimeOffset.UtcNow, [Snapshot(artifact)]);

        var manifest = RobotProgramManifestBuilder.Parse(program.ProgramManifestJson!);

        Assert.Equal(program.Id, manifest.Id);
        var item = Assert.Single(manifest.Artifacts);
        Assert.Equal(artifact.Id, item.RobotArtifact.Id);
        Assert.Equal(artifact.Checksum, item.RobotArtifact.Checksum);
    }

    private static RobotArtifactManifestSnapshot Snapshot(RobotArtifact artifact) => new(
        artifact.Id,
        artifact.ArtifactCode,
        artifact.ArtifactName,
        artifact.FileName,
        artifact.Status,
        artifact.Checksum,
        artifact.StorageKey,
        artifact.RuntimeTargetCode,
        artifact.MachineModelCode,
        artifact.ContentLengthBytes);
}
