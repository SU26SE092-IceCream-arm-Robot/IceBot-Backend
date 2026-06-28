using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.RobotConfiguration.Enums;
using Microsoft.Extensions.Logging;

namespace Application.RobotConfiguration.Commands;

public sealed class DiscardDraftRobotArtifactCommandHandler
{
    private readonly IRobotConfigurationStore _store;
    private readonly IArtifactObjectStorage _objectStorage;
    private readonly ILogger<DiscardDraftRobotArtifactCommandHandler> _logger;

    public DiscardDraftRobotArtifactCommandHandler(
        IRobotConfigurationStore store,
        IArtifactObjectStorage objectStorage,
        ILogger<DiscardDraftRobotArtifactCommandHandler> logger)
    {
        _store = store;
        _objectStorage = objectStorage;
        _logger = logger;
    }

    public async Task<ApiResult<RobotArtifactDiscardResult>> HandleAsync(
        DiscardDraftRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, command.OrganizationId, null, null))
            return ApiResult<RobotArtifactDiscardResult>.Fail("Robot artifact not found.", 404);

        var artifact = await _store.GetArtifactForPublishAsync(command.ArtifactId, cancellationToken);
        if (artifact is null || artifact.OrganizationId != command.OrganizationId)
            return ApiResult<RobotArtifactDiscardResult>.Fail("Robot artifact not found.", 404);
        if (artifact.Status != RobotArtifactStatus.Draft)
            return ApiResult<RobotArtifactDiscardResult>.Fail("Only draft robot artifacts can be discarded.", 400);

        var storageKey = artifact.StorageKey;
        var fileName = artifact.FileName;
        var outcome = await _store.DiscardDraftArtifactAsync(artifact, cancellationToken);
        if (outcome == RobotArtifactDiscardOutcome.Referenced)
            return ApiResult<RobotArtifactDiscardResult>.Fail("Remove the robot artifact from all draft programs before discarding it.", 409);

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
}
