using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Domain.RobotConfiguration.Entities;
using Domain.Tenants.Enums;
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
        var secondStep = program.AddArtifact(secondArtifact.Id, 20, "{\"speed\":2}");
        var firstStep = program.AddArtifact(firstArtifact.Id, 10, "{\"speed\":1}");
        TestData.SetProperty(secondStep, nameof(RobotProgramArtifact.RobotArtifact), secondArtifact);
        TestData.SetProperty(firstStep, nameof(RobotProgramArtifact.RobotArtifact), firstArtifact);

        program.Publish(DateTimeOffset.UtcNow);

        using var manifest = JsonDocument.Parse(program.ProgramManifestJson!);
        var artifacts = manifest.RootElement.GetProperty("Artifacts").EnumerateArray().ToArray();
        Assert.Equal(10, artifacts[0].GetProperty("RunOrder").GetInt32());
        Assert.Equal(firstArtifact.Id, artifacts[0].GetProperty("RobotArtifact").GetProperty("Id").GetGuid());
        Assert.Equal(20, artifacts[1].GetProperty("RunOrder").GetInt32());
        var expectedChecksum = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(program.ProgramManifestJson!))).ToLowerInvariant();
        Assert.Equal(expectedChecksum, program.ProgramManifestChecksum);
    }
}
