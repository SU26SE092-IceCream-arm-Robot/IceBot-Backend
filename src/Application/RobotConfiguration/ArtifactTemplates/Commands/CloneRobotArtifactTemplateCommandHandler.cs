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
using Domain.RobotConfiguration.ArtifactContracts;
using Application.RobotConfiguration.ArtifactContracts;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class CloneRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactStore _store;
    private readonly IRobotArtifactTemplateStore _templateStore;
    private readonly IArtifactObjectStorage _storage;
    private readonly ArtifactUploadContentService _contentService;
    private readonly IRobotArtifactTechnicalContractStore _technicalContracts;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public CloneRobotArtifactTemplateCommandHandler(
        IRobotArtifactStore store,
        IRobotArtifactTemplateStore templateStore,
        IArtifactObjectStorage storage,
        ArtifactUploadContentService contentService,
        IRobotArtifactTechnicalContractStore technicalContracts,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _templateStore = templateStore;
        _storage = storage;
        _contentService = contentService;
        _technicalContracts = technicalContracts;
        _mutations = mutationCoordinator;
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

        var code = command.ArtifactCode.Trim().ToUpperInvariant();
        var observedTemplate = await _templateStore.GetByIdAsync(
            command.TemplateId, tracked: false, cancellationToken);
        if (observedTemplate is null || observedTemplate.Status != RobotArtifactStatus.Published)
        {
            return ApiResult<RobotArtifactResult>.Fail("Published robot artifact template not found.", 404);
        }

        var observedContractId = observedTemplate.TechnicalContractId;
        var resources = new List<TechnicalResourceMutationIdentity>
        {
            TechnicalResourceMutationIdentity.ArtifactDefinition(command.OrganizationId, code),
            TechnicalResourceMutationIdentity.Template(command.TemplateId)
        };
        if (observedContractId.HasValue)
            resources.Add(TechnicalResourceMutationIdentity.Contract(observedContractId.Value));
        return await _mutations.ExecuteAsync(
            resources,
            ct => CloneLockedAsync(command, code, observedContractId, ct),
            cancellationToken);
    }

    private async Task<ApiResult<RobotArtifactResult>> CloneLockedAsync(
        CloneRobotArtifactTemplateCommand command,
        string code,
        Guid? observedContractId,
        CancellationToken cancellationToken)
    {
        var template = await _templateStore.GetByIdAsync(
            command.TemplateId, tracked: false, cancellationToken);
        if (template is null || template.Status != RobotArtifactStatus.Published)
        {
            return ApiResult<RobotArtifactResult>.Fail(
                "The robot artifact template is no longer published; retry with an active template.", 409);
        }

        if (template.TechnicalContractId != observedContractId)
        {
            return ApiResult<RobotArtifactResult>.Fail(
                "The robot artifact template technical contract changed concurrently; retry cloning.", 409);
        }

        if (observedContractId.HasValue)
        {
            if (string.IsNullOrWhiteSpace(template.TechnicalContractChecksum))
                return ApiResult<RobotArtifactResult>.Fail(
                    "The template technical declaration checksum is missing.", 409);
            var technicalContract = await _technicalContracts.GetAsync(
                observedContractId.Value,
                false,
                cancellationToken);
            if (technicalContract is null ||
                technicalContract.OrganizationId.HasValue ||
                technicalContract.Status != RobotArtifactContractStatus.Published ||
                !string.Equals(technicalContract.ContractChecksum, template.TechnicalContractChecksum, StringComparison.Ordinal))
            {
                return ApiResult<RobotArtifactResult>.Fail(
                    "The template technical declaration is no longer published or checksum-consistent.", 409);
            }
        }

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
                template.Id,
                template.TechnicalContractId,
                template.TechnicalContractChecksum);
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
        catch (ArtifactObjectNotFoundException ex)
        {
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectIntegrityException ex)
        {
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 409);
        }
        catch (ArtifactObjectStorageUnavailableException)
        {
            return ApiResult<RobotArtifactResult>.Fail(
                "Artifact object storage is temporarily unavailable.", 503);
        }
    }
}
