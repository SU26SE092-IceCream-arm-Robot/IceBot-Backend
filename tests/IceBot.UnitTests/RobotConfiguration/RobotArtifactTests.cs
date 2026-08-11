using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotArtifactTests
{
    [Fact]
    public void CreateDraft_NormalizesMetadataAndStartsAsDraft()
    {
        var artifact = CreateDraft();

        Assert.Equal(RobotArtifactStatus.Draft, artifact.Status);
        Assert.Equal("PREPARE", artifact.ArtifactCode);
        Assert.Equal("Prepare cup", artifact.ArtifactName);
        Assert.Equal("prepare.lua", artifact.FileName);
        Assert.Equal("FAIRINO-LUA-V1", artifact.RuntimeTargetCode);
        Assert.Equal("FR5", artifact.MachineModelCode);
    }

    [Fact]
    public void Publish_TransitionsDraftToPublishedAndRejectsSecondPublish()
    {
        var artifact = CreateDraft();

        artifact.Publish();

        Assert.Equal(RobotArtifactStatus.Published, artifact.Status);
        Assert.Throws<DomainRuleException>(artifact.Publish);
    }

    [Fact]
    public void Publish_AllowsOptionalTechnicalDeclaration()
    {
        var artifact = CreateDraft();
        artifact.AssignTechnicalContract(Guid.NewGuid(), new string('c', 64));

        artifact.Publish();

        Assert.Equal(RobotArtifactStatus.Published, artifact.Status);
    }

    [Fact]
    public void Retire_RejectsDraftArtifact()
    {
        var artifact = CreateDraft();

        var exception = Assert.Throws<DomainRuleException>(artifact.Retire);

        Assert.Equal("Draft robot artifacts should be deleted or disabled, not retired.", exception.Message);
    }

    private static RobotArtifact CreateDraft() => RobotArtifact.CreateDraft(
        Guid.NewGuid(),
        " PREPARE ",
        " Prepare cup ",
        "robot-artifacts/test/prepare.lua",
        "prepare.lua",
        new string('a', 64),
        " FAIRINO-LUA-V1 ",
        " FR5 ",
        128,
        DateTimeOffset.UtcNow);
}
