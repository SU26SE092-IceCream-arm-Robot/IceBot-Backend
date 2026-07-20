using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Commands;
using Application.Shared.Concurrency;
using Application.Shared.Ownership;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;
using IceBot.UnitTests.TestSupport;
using NSubstitute;

namespace IceBot.UnitTests.RobotConfiguration;

public sealed class RobotProgramArtifactAssignmentTests
{
    [Fact]
    public async Task ReplaceRejectsRetiredArtifact()
    {
        var organizationId = Guid.NewGuid();
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM", "Make ice cream", TenantScopeType.Organization, organizationId);
        var artifact = RobotArtifact.CreateDraft(
            organizationId, "DISPENSE", "Dispense", "robot-artifacts/dispense.lua", "dispense.lua",
            new string('a', 64), "FAIRINO_LUA_V1", "FR5", 100, DateTimeOffset.UtcNow,
            technicalContractId: Guid.NewGuid(), technicalContractChecksum: new string('b', 64));
        artifact.Publish();
        artifact.Retire();

        var programStore = Substitute.For<IRobotProgramStore>();
        programStore.GetProgramForEditAsync(program.Id, Arg.Any<CancellationToken>()).Returns(program);
        var artifactStore = Substitute.For<IRobotArtifactStore>();
        artifactStore.ListArtifactsByIdsAsync(
                organizationId,
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(artifact.Id)),
                Arg.Any<CancellationToken>())
            .Returns([artifact]);
        var handler = new ReplaceRobotProgramArtifactsCommandHandler(
            programStore,
            artifactStore,
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new ReplaceRobotProgramArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            ProgramId = program.Id,
            Artifacts = [new RobotProgramArtifactInput(artifact.Id, 1, 1, null)]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Empty(program.RobotProgramArtifacts);
        await programStore.DidNotReceive().SaveProgramReplacementAsync(
            Arg.Any<IReadOnlyCollection<RobotProgramArtifact>>(),
            Arg.Any<CancellationToken>());
    }
}
