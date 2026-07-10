using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class BulkPublishRobotArtifactsCommandHandler
{
    private const int MaximumItemCount = 100;
    private readonly IRobotArtifactStore _store;

    public BulkPublishRobotArtifactsCommandHandler(IRobotArtifactStore store) => _store = store;

    public async Task<ApiResult<BulkRobotArtifactPublishResult>> HandleAsync(
        BulkPublishRobotArtifactsCommand command,
        CancellationToken cancellationToken = default)
    {
        var requestedIds = command.RobotArtifactIds.ToArray();
        var ids = requestedIds.Distinct().ToArray();
        if (requestedIds.Length == 0 || requestedIds.Length > MaximumItemCount ||
            requestedIds.Any(id => id == Guid.Empty) || ids.Length != requestedIds.Length)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(
                $"Bulk publish requires 1 to {MaximumItemCount} unique, non-empty robot artifact ids.", 400);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail("Access denied.", 403);
        }

        var artifacts = await _store.ListArtifactsByIdsAsync(command.OrganizationId, ids, cancellationToken);
        if (artifacts.Count != ids.Length)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail("One or more robot artifacts were not found.", 400);
        }

        if (artifacts.Any(artifact => artifact.Status is not RobotArtifactStatus.Draft and not RobotArtifactStatus.Published))
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(
                "Only Draft or Published robot artifacts can be included in bulk publish.", 400);
        }

        var alreadyPublishedIds = artifacts
            .Where(artifact => artifact.Status == RobotArtifactStatus.Published)
            .Select(artifact => artifact.Id)
            .ToHashSet();
        var drafts = artifacts.Where(artifact => artifact.Status == RobotArtifactStatus.Draft).ToArray();
        foreach (var artifact in drafts)
        {
            artifact.Publish();
            artifact.UpdatedByAccountId = command.UserContext.AccountId;
        }

        if (drafts.Length > 0)
        {
            await _store.SaveChangesAsync(cancellationToken);
        }

        var items = artifacts
            .OrderBy(artifact => artifact.ArtifactCode)
            .ThenBy(artifact => artifact.FileName)
            .Select(artifact => new BulkRobotArtifactPublishItemResult
            {
                RobotArtifactId = artifact.Id,
                ArtifactCode = artifact.ArtifactCode,
                FileName = artifact.FileName,
                Status = artifact.Status.ToString(),
                WasAlreadyPublished = alreadyPublishedIds.Contains(artifact.Id)
            })
            .ToArray();

        return ApiResult<BulkRobotArtifactPublishResult>.Success(
            new BulkRobotArtifactPublishResult
            {
                TotalCount = items.Length,
                PublishedCount = drafts.Length,
                AlreadyPublishedCount = alreadyPublishedIds.Count,
                Items = items
            },
            "Robot artifacts published successfully.");
    }
}
