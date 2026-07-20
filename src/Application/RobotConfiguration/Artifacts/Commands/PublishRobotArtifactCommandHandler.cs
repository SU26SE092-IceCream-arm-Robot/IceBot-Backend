using Application.RobotConfiguration.Artifacts.Queries;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Shared.Concurrency;
using Application.Tenants;
using Domain.Common;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class PublishRobotArtifactCommandHandler
{
    private readonly IRobotArtifactStore _robotArtifactStore;
    private readonly ArtifactPublicationValidator _publicationValidator;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public PublishRobotArtifactCommandHandler(
        IRobotArtifactStore robotArtifactStore,
        ArtifactPublicationValidator publicationValidator,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _robotArtifactStore = robotArtifactStore;
        _publicationValidator = publicationValidator;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        PublishRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        var observedArtifact = await _robotArtifactStore.GetArtifactByIdAsync(
            command.OrganizationId, command.ArtifactId, cancellationToken);
        if (observedArtifact is null)
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
        var observedContractId = observedArtifact.TechnicalContractId;
        var resources = observedContractId.HasValue
            ? new[]
            {
                TechnicalResourceMutationIdentity.Artifact(command.ArtifactId),
                TechnicalResourceMutationIdentity.Contract(observedContractId.Value)
            }
            : [TechnicalResourceMutationIdentity.Artifact(command.ArtifactId)];

        return await _mutations.ExecuteAsync(
            resources,
            async ct =>
            {
                var artifact = await _robotArtifactStore.GetArtifactForPublishAsync(
                    command.OrganizationId, command.ArtifactId, ct);
                if (artifact is null)
                    return ApiResult<RobotArtifactResult>.Fail("Robot artifact not found.", 404);
                if (artifact.TechnicalContractId != observedContractId)
                    return ApiResult<RobotArtifactResult>.Fail(
                        "Robot artifact technical contract changed concurrently; retry publication.", 409);

                try
                {
                    await _publicationValidator.ValidateAsync(artifact, ct);
                    artifact.Publish();
                    artifact.UpdatedByAccountId = command.UserContext.AccountId;
                    await _robotArtifactStore.SaveChangesAsync(ct);
                    return ApiResult<RobotArtifactResult>.Success(RobotArtifactResult.FromEntity(artifact), "Robot artifact published successfully.");
                }
                catch (DomainRuleException ex) { return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400); }
                catch (ArtifactObjectNotFoundException ex) { return ApiResult<RobotArtifactResult>.Fail(ex.Message, 409); }
                catch (ArtifactObjectIntegrityException ex) { return ApiResult<RobotArtifactResult>.Fail(ex.Message, 409); }
                catch (ArtifactObjectStorageUnavailableException ex) { return ApiResult<RobotArtifactResult>.Fail(ex.Message, 503); }
            }, cancellationToken);
    }
}
