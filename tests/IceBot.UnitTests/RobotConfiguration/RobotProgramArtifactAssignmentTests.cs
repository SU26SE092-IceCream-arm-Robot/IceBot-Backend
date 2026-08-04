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
    public async Task ReplaceRejectsNonContiguousRunOrder()
    {
        var organizationId = Guid.NewGuid();
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM", "Make ice cream", TenantScopeType.Organization, organizationId);
        var handler = new ReplaceRobotProgramArtifactsCommandHandler(
            Substitute.For<IRobotProgramStore>(),
            Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new ReplaceRobotProgramArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            ProgramId = program.Id,
            Artifacts =
            [
                new RobotProgramArtifactInput(Guid.NewGuid(), 1, 1, null),
                new RobotProgramArtifactInput(Guid.NewGuid(), 3, 1, null)
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Robot program artifact run orders must be contiguous, starting at one.", result.Message);
    }

    [Fact]
    public async Task ReplaceRejectsDuplicateArtifact()
    {
        var organizationId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        var handler = new ReplaceRobotProgramArtifactsCommandHandler(
            Substitute.For<IRobotProgramStore>(),
            Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new ReplaceRobotProgramArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            ProgramId = Guid.NewGuid(),
            Artifacts =
            [
                new RobotProgramArtifactInput(artifactId, 1, 1, null),
                new RobotProgramArtifactInput(artifactId, 2, 1, null)
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("A robot artifact can appear only once in a robot program.", result.Message);
    }

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

    [Fact]
    public async Task ReplaceRejectsStaleProgramVersion()
    {
        var organizationId = Guid.NewGuid();
        var program = RobotProgram.CreateDraft(
            "MAKE_ICE_CREAM", "Make ice cream", TenantScopeType.Organization, organizationId);
        program.UpdatedAt = DateTimeOffset.UtcNow;

        var programStore = Substitute.For<IRobotProgramStore>();
        programStore.GetProgramForEditAsync(program.Id, Arg.Any<CancellationToken>()).Returns(program);
        var handler = new ReplaceRobotProgramArtifactsCommandHandler(
            programStore,
            Substitute.For<IRobotArtifactStore>(),
            Substitute.For<ITechnicalResourceMutationPolicy>(),
            InlineTechnicalResourceMutationCoordinator.Instance);

        var result = await handler.HandleAsync(new ReplaceRobotProgramArtifactsCommand
        {
            UserContext = TestData.SystemAdmin(),
            OrganizationId = organizationId,
            ProgramId = program.Id,
            ExpectedLastModifiedAt = program.UpdatedAt.Value.AddSeconds(-1),
            Artifacts = [new RobotProgramArtifactInput(Guid.NewGuid(), 1, 1, null)]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Robot program changed since it was loaded. Reload it before saving the artifact order.", result.Message);
        await programStore.DidNotReceive().SaveProgramReplacementAsync(
            Arg.Any<IReadOnlyCollection<RobotProgramArtifact>>(),
            Arg.Any<CancellationToken>());
    }
}
