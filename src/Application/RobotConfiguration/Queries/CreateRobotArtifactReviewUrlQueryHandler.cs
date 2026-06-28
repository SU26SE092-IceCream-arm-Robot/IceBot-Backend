using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.RobotConfiguration.Queries;

public sealed class CreateRobotArtifactReviewUrlQueryHandler
{
    private readonly IRobotConfigurationStore _store;
    private readonly IArtifactObjectStorage _objectStorage;

    public CreateRobotArtifactReviewUrlQueryHandler(
        IRobotConfigurationStore store,
        IArtifactObjectStorage objectStorage)
    {
        _store = store;
        _objectStorage = objectStorage;
    }

    public async Task<ApiResult<RobotArtifactReviewUrlResult>> HandleAsync(
        CreateRobotArtifactReviewUrlQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactRead, query.UserContext, query.OrganizationId, null, null))
            return ApiResult<RobotArtifactReviewUrlResult>.Fail("Robot artifact not found.", 404);

        var artifact = await _store.GetArtifactByIdAsync(query.OrganizationId, query.ArtifactId, cancellationToken);
        if (artifact is null)
            return ApiResult<RobotArtifactReviewUrlResult>.Fail("Robot artifact not found.", 404);
        if (!await _objectStorage.ExistsAsync(artifact.StorageKey, cancellationToken))
            return ApiResult<RobotArtifactReviewUrlResult>.Fail("Robot artifact object is unavailable.", 409);

        var readUrl = await _objectStorage.CreateReadUrlAsync(artifact.StorageKey, cancellationToken);
        return ApiResult<RobotArtifactReviewUrlResult>.Success(new RobotArtifactReviewUrlResult
        {
            RobotArtifactId = artifact.Id,
            FileName = artifact.FileName,
            Checksum = artifact.Checksum,
            ContentLengthBytes = artifact.ContentLengthBytes,
            Url = readUrl.Url,
            ExpiresAt = readUrl.ExpiresAt
        });
    }
}
