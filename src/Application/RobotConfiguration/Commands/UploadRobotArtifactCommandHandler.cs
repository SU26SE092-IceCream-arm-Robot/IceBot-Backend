using System.Security.Cryptography;
using System.Text.Json;
using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.RobotConfiguration.Entities;
using Microsoft.Extensions.Logging;

namespace Application.RobotConfiguration.Commands;

public sealed class UploadRobotArtifactCommandHandler
{
    private const int BufferSize = 81920;
    private readonly IRobotConfigurationStore _robotConfigurationStore;
    private readonly IArtifactObjectStorage _artifactObjectStorage;
    private readonly ILogger<UploadRobotArtifactCommandHandler> _logger;

    public UploadRobotArtifactCommandHandler(
        IRobotConfigurationStore robotConfigurationStore,
        IArtifactObjectStorage artifactObjectStorage,
        ILogger<UploadRobotArtifactCommandHandler> logger)
    {
        _robotConfigurationStore = robotConfigurationStore;
        _artifactObjectStorage = artifactObjectStorage;
        _logger = logger;
    }

    public async Task<ApiResult<RobotArtifactResult>> HandleAsync(
        UploadRobotArtifactCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!ScopeAccessRules.CanAccessScopedRow(command.UserContext, command.OrganizationId, null, null))
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

        if (command.ContentLengthBytes <= 0)
        {
            return ApiResult<RobotArtifactResult>.Fail("Robot artifact file must not be empty.", 400);
        }

        if (!string.IsNullOrWhiteSpace(command.MetadataJson) && !IsValidJson(command.MetadataJson))
        {
            return ApiResult<RobotArtifactResult>.Fail("MetadataJson must be a valid JSON string.", 400);
        }

        var bufferedContent = new MemoryStream();
        var checksum = await CopyAndHashAsync(command.Content, bufferedContent, cancellationToken);
        var normalizedArtifactCode = NormalizeCode(command.ArtifactCode);
        var normalizedRuntimeTargetCode = NormalizeCode(command.RuntimeTargetCode);
        var normalizedMachineModelCode = NormalizeCode(command.MachineModelCode);

        if (bufferedContent.Length != command.ContentLengthBytes)
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
            bufferedContent.Position = 0;
            writeResult = await _artifactObjectStorage.WriteImmutableAsync(
                new ArtifactObjectWriteRequest(
                    storageKey,
                    NormalizeContentType(command.ContentType),
                    bufferedContent.Length,
                    checksum),
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
            await TryDeleteUncommittedObjectAsync(writeResult.StorageKey);
            return ApiResult<RobotArtifactResult>.Fail(ex.Message, 400);
        }

        artifact.Id = artifactId;
        artifact.CreatedByAccountId = command.UserContext.AccountId;

        // Do not delete on an ambiguous database exception: the commit may have succeeded.
        // The grace-period cleanup job reconciles object storage against committed metadata.
        await _robotConfigurationStore.AddArtifactAsync(artifact, cancellationToken);
        await _robotConfigurationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<RobotArtifactResult>.Success(
            RobotArtifactResult.FromEntity(artifact),
            "Robot artifact uploaded successfully.",
            201);
    }

    private static async Task<string> CopyAndHashAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[BufferSize];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha256.TransformBlock(buffer, 0, read, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
    }

    private static string BuildStorageKey(Guid organizationId, Guid artifactId, string checksum)
    {
        return $"robot-artifacts/{organizationId:D}/{artifactId:D}/{checksum}.lua";
    }

    private static string NormalizeContentType(string? contentType)
    {
        return string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
    }

    private static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task TryDeleteUncommittedObjectAsync(string storageKey)
    {
        try
        {
            await _artifactObjectStorage.DeleteIfExistsAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to compensate uncommitted robot artifact object {StorageKey}; scheduled orphan cleanup will retry.",
                storageKey);
        }
    }
}
