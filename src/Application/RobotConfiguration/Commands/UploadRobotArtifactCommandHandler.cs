using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.RobotConfiguration.Services;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Commands;

public sealed class UploadRobotArtifactCommandHandler
{
    private readonly IRobotConfigurationStore _robotConfigurationStore;
    private readonly ArtifactUploadContentService _contentService;

    public UploadRobotArtifactCommandHandler(
        IRobotConfigurationStore robotConfigurationStore,
        ArtifactUploadContentService contentService)
    {
        _robotConfigurationStore = robotConfigurationStore;
        _contentService = contentService;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        UploadRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.ArtifactUpload, command.UserContext, command.OrganizationId, null, null))
        {
            return ApiResult<RobotArtifactResult>.Fail("Access denied.", 403);
        }

        if (!await _robotConfigurationStore.OrganizationExistsAsync(command.OrganizationId, cancellationToken))
        {
            return ApiResult<RobotArtifactResult>.Fail("Organization not found.", 404);
        }

        if (!command.FileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact file must use the .lua extension.", 400);
        }

        if (command.ContentLengthBytes <= 0 || command.ContentLengthBytes > ArtifactUploadContentService.MaximumFileSizeBytes)
        {
            return ApiResult<RobotArtifactResult>.Fail(
                $"Robot artifact file must be between 1 byte and {ArtifactUploadContentService.MaximumFileSizeBytes} bytes.", 400);
        }

        if (!ArtifactUploadContentService.IsValidMetadataJson(command.MetadataJson))
        {
            return ApiResult<RobotArtifactResult>.Fail("MetadataJson must be a valid JSON string.", 400);
        }

        await using var bufferedContent = await _contentService.BufferAndHashAsync(
            command.Content,
            command.ContentLengthBytes,
            cancellationToken);
        var checksum = bufferedContent.Checksum;
        var normalizedArtifactCode = NormalizeCode(command.ArtifactCode);
        var normalizedRuntimeTargetCode = NormalizeCode(command.RuntimeTargetCode);
        var normalizedMachineModelCode = NormalizeCode(command.MachineModelCode);

        if (bufferedContent.Stream.Length != command.ContentLengthBytes)
        {
            return ApiResult<RobotArtifactResult>.Fail("Uploaded content length does not match the request length.", 400);
        }

        var existingArtifact = await _robotConfigurationStore.GetArtifactByCodeAndChecksumAsync(
            command.OrganizationId,
            normalizedArtifactCode,
            checksum,
            cancellationToken);
        if (existingArtifact is not null)
        {
            return ApiResult<RobotArtifactResult>.Success(
                RobotArtifactResult.FromEntity(existingArtifact),
                "Matching robot artifact already exists; existing metadata returned.");
        }

        var artifactId = Guid.NewGuid();
        var storageKey = BuildStorageKey(command.OrganizationId, artifactId, checksum);

        ArtifactObjectWriteResult writeResult;
        try
        {
            writeResult = await _contentService.WriteImmutableAsync(
                storageKey,
                command.ContentType,
                bufferedContent,
                cancellationToken);
        }
        catch (ArtifactObjectAlreadyExistsException)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact object already exists.", 409);
        }

        RobotArtifact artifact;
        try
        {
            artifact = RobotArtifact.CreateDraft(
                command.OrganizationId,
                normalizedArtifactCode,
                command.ArtifactName,
                writeResult.StorageKey,
                command.FileName,
                writeResult.Checksum,
                normalizedRuntimeTargetCode,
                normalizedMachineModelCode,
                writeResult.ContentLengthBytes,
                command.ExportedAt ?? DateTimeOffset.UtcNow,
                command.Description,
                command.MetadataJson?.Trim());

        }
        catch (DomainRuleException ex)
        {
            await _contentService.DeleteUncommittedObjectAsync(writeResult.StorageKey);
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }

        artifact.Id = artifactId;
        artifact.CreatedByAccountId = command.UserContext.AccountId;

        // A concurrent request may win the unique artifact identity after the initial lookup.
        // Resolve that conflict to the committed winner and remove only this request's known-loser object.
        var insertResult = await _robotConfigurationStore.InsertArtifactOrGetExistingAsync(artifact, cancellationToken);
        if (!insertResult.Created)
        {
            await _contentService.DeleteUncommittedObjectAsync(writeResult.StorageKey);
            return ApiResult<RobotArtifactResult>.Success(
                RobotArtifactResult.FromEntity(insertResult.Artifact),
                "Matching robot artifact already exists; concurrent upload resolved to existing metadata.");
        }

        return ApiResult<RobotArtifactResult>.Success(
            RobotArtifactResult.FromEntity(insertResult.Artifact),
            "Robot artifact uploaded successfully.",
            201);
    }

    private static string BuildStorageKey(Guid organizationId, Guid artifactId, string checksum)
    {
        return $"robot-artifacts/{organizationId:D}/{artifactId:D}/{checksum}.lua";
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

}
