using Application.RobotConfiguration.Artifacts.Abstractions;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class CloneRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactStore _store;
    private readonly IRobotArtifactTemplateStore _templateStore;
    private readonly IArtifactObjectStorage _storage;
    private readonly ArtifactUploadContentService _contentService;

    public CloneRobotArtifactTemplateCommandHandler(
        IRobotArtifactStore store,
        IRobotArtifactTemplateStore templateStore,
        IArtifactObjectStorage storage,
        ArtifactUploadContentService contentService)
    {
        _store = store;
        _templateStore = templateStore;
        _storage = storage;
        _contentService = contentService;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        CloneRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.ArtifactUpload,
                command.UserContext,
                command.OrganizationId,
                null,
                null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        if (!await _store.OrganizationExistsAsync(command.OrganizationId, cancellationToken))
        {
            return ApiResult<RobotArtifactResult>.Fail("Organization not found.", 404);
        }

        if (!ArtifactUploadContentService.IsValidMetadataJson(command.MetadataJson))
        {
            return ApiResult<RobotArtifactResult>.Fail("MetadataJson must be valid JSON.", 400);
        }

        var template = await _templateStore.GetByIdAsync(command.TemplateId, cancellationToken: cancellationToken);
        if (template is null || template.Status != RobotArtifactStatus.Published)
        {
            return ApiResult<RobotArtifactResult>.Fail("Published robot artifact template not found.", 404);
        }

        var code = command.ArtifactCode.Trim().ToUpperInvariant();
        var existing = await _store.GetArtifactByCodeAndChecksumAsync(
            command.OrganizationId,
            code,
            template.Checksum,
            cancellationToken);
        if (existing is not null)
        {
            return existing.SourceRobotArtifactTemplateId == template.Id
                ? ApiResult<RobotArtifactResult>.Success(
                    RobotArtifactResult.FromEntity(existing),
                    "Matching template clone already exists.")
                : ApiResult<RobotArtifactResult>.Fail(
                    "Artifact identity already exists without matching template lineage. Use a different artifact code.",
                    409);
        }

        var artifactId = Guid.NewGuid();
        var destinationKey = $"robot-artifacts/{command.OrganizationId:D}/{artifactId:D}/{template.Checksum}.lua";
        try
        {
            await _storage.CopyImmutableAsync(
                template.StorageKey,
                new ArtifactObjectWriteRequest(
                    destinationKey,
                    "application/octet-stream",
                    template.ContentLengthBytes,
                    template.Checksum),
                cancellationToken);

            var artifact = RobotArtifact.CreateDraft(
                command.OrganizationId,
                code,
                command.ArtifactName,
                destinationKey,
                template.FileName,
                template.Checksum,
                template.RuntimeTargetCode,
                template.MachineModelCode,
                template.ContentLengthBytes,
                template.ExportedAt,
                command.Description ?? template.Description,
                command.MetadataJson?.Trim() ?? template.MetadataJson,
                template.Id);
            artifact.Id = artifactId;
            artifact.CreatedByAccountId = command.UserContext.AccountId;

            var inserted = await _store.InsertArtifactOrGetExistingAsync(artifact, cancellationToken);
            if (!inserted.Created)
            {
                await _contentService.DeleteUncommittedObjectAsync(destinationKey);
                if (inserted.Artifact.SourceRobotArtifactTemplateId != template.Id)
                {
                    return ApiResult<RobotArtifactResult>.Fail(
                        "Artifact identity was concurrently created without matching template lineage. Use a different artifact code.",
                        409);
                }
            }

            return ApiResult<RobotArtifactResult>.Success(
                RobotArtifactResult.FromEntity(inserted.Artifact),
                inserted.Created
                    ? "Template cloned to organization draft."
                    : "Matching organization artifact already exists.",
                inserted.Created ? 201 : 200);
        }
        catch (DomainRuleException ex)
        {
            await _contentService.DeleteUncommittedObjectAsync(destinationKey);
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }
        catch (ArtifactObjectAlreadyExistsException)
        {
            return ApiResult<RobotArtifactResult>.Fail("Destination artifact object already exists.", 409);
        }
    }
}
