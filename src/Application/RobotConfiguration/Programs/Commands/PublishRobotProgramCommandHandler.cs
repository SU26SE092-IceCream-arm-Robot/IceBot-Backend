using Application.RobotConfiguration.Programs.ReadModels;
using Application.RobotConfiguration.Programs.Queries;
using Domain.RobotConfiguration.Programs.Manifests;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Programs.Abstractions;
using Application.RobotConfiguration.Programs.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Application.RobotConfiguration.Programs.Mapping;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class PublishRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _robotProgramStore;
    private readonly IRobotArtifactStore _robotArtifactStore;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public PublishRobotProgramCommandHandler(
        IRobotProgramStore robotProgramStore,
        IRobotArtifactStore robotArtifactStore,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _robotProgramStore = robotProgramStore;
        _robotArtifactStore = robotArtifactStore;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        PublishRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Program(command.ProgramId)],
            async ct =>
            {
                var program = await _robotProgramStore.GetProgramForPublishAsync(command.ProgramId, ct);
                if (program is null || program.OrganizationId != command.OrganizationId)
                    return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
                if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
                    return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
                try
                {
                    var artifactSnapshots = await RobotProgramResultMapper.LoadArtifactSnapshotsAsync(
                        _robotArtifactStore, program, ct);
                    program.Publish(DateTimeOffset.UtcNow, artifactSnapshots);
                    program.UpdatedByAccountId = command.UserContext.AccountId;
                    await _robotProgramStore.SaveChangesAsync(ct);
                    return ApiResult<RobotProgramResult>.Success(
                        RobotProgramResult.FromEntity(program, artifactSnapshots),
                        "Robot program published successfully.");
                }
                catch (DomainRuleException ex) { return ApiResult<RobotProgramResult>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
