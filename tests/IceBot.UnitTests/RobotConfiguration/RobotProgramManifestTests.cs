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
using System.Text.Json.Nodes;
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
    public void Publish_AllowsBlackBoxArtifactsWithoutTechnicalDeclaration()
    {
        var organizationId = Guid.NewGuid();
        var artifact = RobotArtifact.CreateDraft(
            organizationId,
            "BLACK_BOX",
            "Black box",
            $"robot-artifacts/{organizationId:D}/black-box.lua",
            "black-box.lua",
            new string('f', 64),
            "FAIRINO_LUA_V1",
            "FR5",
            128,
            DateTimeOffset.UtcNow);
        artifact.Publish();
        var program = RobotProgram.CreateDraft(
            "BLACK_BOX_PROGRAM",
            "Black box program",
            TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(artifact.Id, 1);

        program.Publish(DateTimeOffset.UtcNow, [Snapshot(artifact)]);

        var manifestArtifact = Assert.Single(
            RobotProgramManifestBuilder.Parse(program.ProgramManifestJson!).Artifacts).RobotArtifact;
        Assert.Null(manifestArtifact.TechnicalContractId);
        Assert.Null(manifestArtifact.TechnicalContractChecksum);
        Assert.Equal(artifact.Checksum, manifestArtifact.Checksum);
        Assert.Equal(artifact.StorageKey, manifestArtifact.StorageKey);
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
    public void Publish_RejectsArtifactsWithDifferentRuntimeProfiles()
    {
        var organizationId = Guid.NewGuid();
        var fr5 = TestData.PublishedArtifact(organizationId, "FR5_STEP", "fr5.lua", 'a');
        var fr3 = RobotArtifact.CreateDraft(
            organizationId, "FR3_STEP", "FR3 step", $"robot-artifacts/{organizationId:D}/fr3.lua",
            "fr3.lua", new string('b', 64), "FAIRINO_LUA_V1", "FR3", 128, DateTimeOffset.UtcNow);
        fr3.Publish();
        var program = RobotProgram.CreateDraft(
            "MIXED_PROFILE", "Mixed profile", TenantScopeType.Organization, organizationId);
        program.AddArtifact(fr5.Id, 1);
        program.AddArtifact(fr3.Id, 2);

        var exception = Assert.Throws<Domain.Common.DomainRuleException>(() =>
            program.Publish(DateTimeOffset.UtcNow, [Snapshot(fr5), Snapshot(fr3)]));

        Assert.Contains("only one runtime target and machine model", exception.Message,
            StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal(2, manifest.SchemaVersion);
        Assert.Equal(RobotProgramRestartPolicy.ManualOnly, manifest.RestartPolicy);
        var item = Assert.Single(manifest.Artifacts);
        Assert.Equal(artifact.Id, item.RobotArtifact.Id);
        Assert.Equal(artifact.Checksum, item.RobotArtifact.Checksum);
    }

    [Fact]
    public void Publish_PreservesOptionConditionalExecution()
    {
        var organizationId = Guid.NewGuid();
        var artifact = TestData.PublishedArtifact(organizationId, "TOPPING", "topping.lua", 'd');
        var program = RobotProgram.CreateDraft("MAKE_TOPPING", "Make topping",
            TenantScopeType.Organization, organizationId);
        program.AddArtifact(artifact.Id, 10, requiredOptionCode: "extra_nuts");

        program.Publish(DateTimeOffset.UtcNow, [Snapshot(artifact)]);

        var item = Assert.Single(RobotProgramManifestBuilder.Parse(program.ProgramManifestJson!).Artifacts);
        Assert.Equal("EXTRA_NUTS", item.RequiredOptionCode);
    }

    [Fact]
    public void Parse_LegacySchema1ManifestWithoutRestartPolicy_DefaultsToManualOnly()
    {
        var organizationId = Guid.NewGuid();
        var artifact = TestData.PublishedArtifact(organizationId, "LEGACY", "legacy.lua", 'e');
        var program = RobotProgram.CreateDraft(
            "LEGACY_PROGRAM",
            "Legacy program",
            TenantScopeType.Organization,
            organizationId);
        program.AddArtifact(artifact.Id, 10);
        program.Publish(DateTimeOffset.UtcNow, [Snapshot(artifact)]);
        var node = JsonNode.Parse(program.ProgramManifestJson!)!.AsObject();
        node[nameof(RobotProgramManifestDocument.SchemaVersion)] = 1;
        node.Remove(nameof(RobotProgramManifestDocument.RestartPolicy));

        var manifest = RobotProgramManifestBuilder.Parse(node.ToJsonString());

        Assert.Equal(RobotProgramRestartPolicy.ManualOnly, manifest.RestartPolicy);
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
        artifact.ContentLengthBytes,
        artifact.TechnicalContractId,
        artifact.TechnicalContractChecksum,
        artifact.RuntimeProfileSource);
}
