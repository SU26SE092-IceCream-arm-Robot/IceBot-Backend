using Application.RobotConfiguration.Artifacts.Queries;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Shared.Ownership;
using Domain.Common;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class RetireRobotArtifactCommandHandler
{
    private readonly IRobotArtifactStore _store;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public RetireRobotArtifactCommandHandler(
        IRobotArtifactStore store,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _technicalOwnership = technicalOwnership;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        RetireRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Artifact(command.ArtifactId)],
            async ct =>
            {
                var artifact = await _store.GetArtifactForPublishAsync(command.OrganizationId, command.ArtifactId, ct);
                if (artifact is null) return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
                var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotArtifact, artifact.Id, ct);
                if (ownershipError is not null) return ApiResult<RobotArtifactResult>.Fail(ownershipError, 409);
                if (await _store.ArtifactIsReferencedByDraftProgramAsync(artifact.Id, ct))
                    return ApiResult<RobotArtifactResult>.Fail("Remove the robot artifact from draft programs before retiring it.", 409);
                try
                {
                    artifact.Retire();
                    artifact.UpdatedByAccountId = command.UserContext.AccountId;
                    await _store.SaveChangesAsync(ct);
                    return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact), "Robot artifact retired successfully.");
                }
                catch (DomainRuleException ex) { return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
