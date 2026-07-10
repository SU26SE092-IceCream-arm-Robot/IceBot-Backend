using Application.RobotConfiguration.Artifacts.Queries;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class RetireRobotArtifactCommandHandler
{
    private readonly IRobotArtifactStore _store;
    public RetireRobotArtifactCommandHandler(IRobotArtifactStore store) => _store = store;

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        RetireRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        var artifact = await _store.GetArtifactForPublishAsync(command.OrganizationId, command.ArtifactId, cancellationToken);
        if (artifact is null)
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
