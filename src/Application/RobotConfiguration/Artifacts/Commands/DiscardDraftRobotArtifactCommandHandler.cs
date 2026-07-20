using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Application.Shared.Ownership;
using Application.Shared.Concurrency;
using Domain.RobotConfiguration.Artifacts;
using Microsoft.Extensions.Logging;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class DiscardDraftRobotArtifactCommandHandler
{
    private readonly IRobotArtifactStore _store;
    private readonly IArtifactObjectStorage _objectStorage;
    private readonly ILogger<DiscardDraftRobotArtifactCommandHandler> _logger;
    private readonly ITechnicalResourceMutationPolicy _technicalOwnership;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public DiscardDraftRobotArtifactCommandHandler(
        IRobotArtifactStore store,
        IArtifactObjectStorage objectStorage,
        ILogger<DiscardDraftRobotArtifactCommandHandler> logger,
        ITechnicalResourceMutationPolicy technicalOwnership,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _objectStorage = objectStorage;
        _logger = logger;
        _technicalOwnership = technicalOwnership;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactDiscardResult>> HandleAsync(
        DiscardDraftRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
            return ApiResult<RobotArtifactDiscardResult>.Fail("Robot artifact not found.", 404);

        var preparation = await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Artifact(command.ArtifactId)],
            async ct =>
            {
                var artifact = await _store.GetArtifactForPublishAsync(command.OrganizationId, command.ArtifactId, ct);
                if (artifact is null)
                    return ApiResult<DiscardPreparation>.Fail("Robot artifact not found.", 404);
                if (artifact.Status != RobotArtifactStatus.Draft)
                    return ApiResult<DiscardPreparation>.Fail("Only draft robot artifacts can be discarded.", 400);
                var ownershipError = await _technicalOwnership.ValidateDefinitionMutationAsync(
                    TechnicalResourceKind.RobotArtifact, artifact.Id, ct);
                if (ownershipError is not null)
                    return ApiResult<DiscardPreparation>.Fail(ownershipError, 409);

                var outcome = await _store.DiscardDraftArtifactAsync(artifact, ct);
                return outcome == RobotArtifactDiscardOutcome.Referenced
                    ? ApiResult<DiscardPreparation>.Fail("Remove the robot artifact from all draft programs before discarding it.", 409)
                    : ApiResult<DiscardPreparation>.Success(new DiscardPreparation(artifact.StorageKey, artifact.FileName));
            }, cancellationToken);

        if (!preparation.Succeeded || preparation.Data is null)
            return ApiResult<RobotArtifactDiscardResult>.Fail(
                preparation.Message ?? "Robot artifact could not be discarded.", preparation.StatusCode);

        var storageKey = preparation.Data.StorageKey;
        var fileName = preparation.Data.FileName;

        var objectDeleted = true;
        try
        {
            await _objectStorage.DeleteIfExistsAsync(storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            objectDeleted = false;
            _logger.LogWarning(ex,
                "Draft robot artifact {RobotArtifactId} metadata was deleted but object {StorageKey} requires orphan cleanup.",
                command.ArtifactId,
                storageKey);
        }

        return ApiResult<RobotArtifactDiscardResult>.Success(new RobotArtifactDiscardResult
        {
            RobotArtifactId = command.ArtifactId,
            FileName = fileName,
            ObjectDeleted = objectDeleted
        }, objectDeleted
            ? "Draft robot artifact discarded successfully."
            : "Draft robot artifact metadata discarded; object cleanup is pending.");
    }

    private sealed record DiscardPreparation(string StorageKey, string FileName);
}
