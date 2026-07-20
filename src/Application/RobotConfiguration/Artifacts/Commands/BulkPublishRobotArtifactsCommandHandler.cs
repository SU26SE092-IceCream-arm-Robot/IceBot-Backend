using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.RobotConfiguration.Artifacts;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Common;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.Artifacts.Commands;

public sealed class BulkPublishRobotArtifactsCommandHandler
{
    private const int MaximumItemCount = 100;
    private readonly IRobotArtifactStore _store;
    private readonly ArtifactPublicationValidator _publicationValidator;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public BulkPublishRobotArtifactsCommandHandler(
        IRobotArtifactStore store,
        ArtifactPublicationValidator publicationValidator,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _publicationValidator = publicationValidator;
        _mutations = mutationCoordinator;
    }

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

        var observedSnapshots = await _store.ListArtifactManifestSnapshotsAsync(
            command.OrganizationId, ids, cancellationToken);
        if (observedSnapshots.Count != ids.Length)
            return ApiResult<BulkRobotArtifactPublishResult>.Fail("One or more robot artifacts were not found.", 400);
        var observedContractIds = observedSnapshots.ToDictionary(
            item => item.RobotArtifactId, item => item.TechnicalContractId);
        var resources = ids.Select(TechnicalResourceMutationIdentity.Artifact)
            .Concat(observedSnapshots.Where(item => item.TechnicalContractId.HasValue)
                .Select(item => TechnicalResourceMutationIdentity.Contract(item.TechnicalContractId!.Value)))
            .ToArray();

        return await _mutations.ExecuteAsync(
            resources,
            async ct => await PublishLockedAsync(command, ids, observedContractIds, ct),
            cancellationToken);
    }

    private async Task<ApiResult<BulkRobotArtifactPublishResult>> PublishLockedAsync(
        BulkPublishRobotArtifactsCommand command,
        Guid[] ids,
        IReadOnlyDictionary<Guid, Guid?> observedContractIds,
        CancellationToken cancellationToken)
    {
        var artifacts = await _store.ListArtifactsByIdsAsync(command.OrganizationId, ids, cancellationToken);
        if (artifacts.Count != ids.Length)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail("One or more robot artifacts were not found.", 400);
        }
        if (artifacts.Any(artifact =>
                !observedContractIds.TryGetValue(artifact.Id, out var contractId) ||
                contractId != artifact.TechnicalContractId))
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(
                "One or more robot artifact technical contracts changed concurrently; retry publication.", 409);

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
        try
        {
            foreach (var artifact in drafts)
            {
                await _publicationValidator.ValidateAsync(artifact, cancellationToken);
            }

            foreach (var artifact in drafts)
            {
                artifact.Publish();
                artifact.UpdatedByAccountId = command.UserContext.AccountId;
            }
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectNotFoundException ex)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectIntegrityException ex)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectStorageUnavailableException ex)
        {
            return ApiResult<BulkRobotArtifactPublishResult>.Fail(ex.Message, 503);
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
