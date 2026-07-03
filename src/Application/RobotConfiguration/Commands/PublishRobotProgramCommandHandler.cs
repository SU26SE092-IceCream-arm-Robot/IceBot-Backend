using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Application.RobotConfiguration.Mapping;

namespace Application.RobotConfiguration.Commands;

public sealed class PublishRobotProgramCommandHandler
{
    private readonly IRobotConfigurationStore _robotConfigurationStore;

    public PublishRobotProgramCommandHandler(IRobotConfigurationStore robotConfigurationStore)
    {
        _robotConfigurationStore = robotConfigurationStore;
    }

    public async Task<ApiResult<RobotProgramResult>> HandleAsync(
        PublishRobotProgramCommand command,
        CancellationToken cancellationToken = default)
    {
        var program = await _robotConfigurationStore.GetProgramForPublishAsync(command.ProgramId, cancellationToken);
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
                _robotConfigurationStore, program, cancellationToken);
            program.Publish(DateTimeOffset.UtcNow, artifactSnapshots);
            program.UpdatedByAccountId = command.UserContext.AccountId;
            await _robotConfigurationStore.SaveChangesAsync(cancellationToken);
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
