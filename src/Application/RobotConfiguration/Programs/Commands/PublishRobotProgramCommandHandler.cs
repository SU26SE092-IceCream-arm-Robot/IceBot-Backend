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

namespace Application.RobotConfiguration.Programs.Commands;

public sealed class PublishRobotProgramCommandHandler
{
    private readonly IRobotProgramStore _robotProgramStore;
    private readonly IRobotArtifactStore _robotArtifactStore;

    public PublishRobotProgramCommandHandler(
        IRobotProgramStore robotProgramStore,
        IRobotArtifactStore robotArtifactStore)
    {
        _robotProgramStore = robotProgramStore;
        _robotArtifactStore = robotArtifactStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        PublishRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _robotProgramStore.GetProgramForPublishAsync(command.ProgramId, cancellationToken);
        if (program is null || program.OrganizationId != command.OrganizationId)
        {
            return ApiResult<RobotProgramResult>.Fail("Robot program not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ProgramManage, command.UserContext, program.OrganizationId, program.StoreId, program.KioskId))
        {
            return ApiResult<RobotProgramResult>.Fail("Access denied.", 403);
        }

        try
        {
            var artifactSnapshots = await RobotProgramResultMapper.LoadArtifactSnapshotsAsync(
                _robotArtifactStore, program, cancellationToken);
            program.Publish(DateTimeOffset.UtcNow, artifactSnapshots);
            program.UpdatedByAccountId = command.UserContext.AccountId;
            await _robotProgramStore.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotProgramResult>.Success(
                RobotProgramResult.FromEntity(program, artifactSnapshots),
                "Robot program published successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotProgramResult>.Fail(ex.Message, 400);
        }
    }
}
