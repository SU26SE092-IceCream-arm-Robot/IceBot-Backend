using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Commands;

public sealed class RetireRobotArtifactCommandHandler
{
    private readonly IRobotConfigurationStore _store;
    public RetireRobotArtifactCommandHandler(IRobotConfigurationStore store) => _store = store;

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        RetireRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _store.GetArtifactForPublishAsync(command.ArtifactId, cancellationToken);
        if (artifact is null || artifact.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        if (await _store.ArtifactIsReferencedByDraftProgramAsync(artifact.Id, cancellationToken))
            return ApiResult<RobotArtifactResult>.Fail("Remove the robot artifact from draft programs before retiring it.", 409);

        try
        {
            artifact.Retire();
            artifact.UpdatedByAccountId = command.UserContext.AccountId;
            await _store.SaveChangesAsync(cancellationToken);
            return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact), "Robot artifact retired successfully.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }
    }
}
