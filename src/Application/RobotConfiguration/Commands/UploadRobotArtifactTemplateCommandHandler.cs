using Application.RobotConfiguration.Abstractions;
using Application.RobotConfiguration.Results;
using Application.RobotConfiguration.Services;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Commands;

public sealed class UploadRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly ArtifactUploadContentService _contentService;

    public UploadRobotArtifactTemplateCommandHandler(
        IRobotArtifactTemplateStore store,
        ArtifactUploadContentService contentService)
    {
        _store = store;
        _contentService = contentService;
    }

    public async Task<ApiResult<RobotArtifactTemplateResult>> HandleAsync(
        UploadRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.UserContext.IsSystemAdmin)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Access denied.", 403);
        }

        if (!command.FileName.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Template file must use the .lua extension.", 400);
        }

        if (command.ContentLengthBytes <= 0 ||
            command.ContentLengthBytes > ArtifactUploadContentService.MaximumFileSizeBytes)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail(
                $"Template file must be between 1 byte and {ArtifactUploadContentService.MaximumFileSizeBytes} bytes.",
                400);
        }

        if (!ArtifactUploadContentService.IsValidMetadataJson(command.MetadataJson))
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("MetadataJson must be valid JSON.", 400);
        }

        await using var content = await _contentService.BufferAndHashAsync(
            command.Content,
            command.ContentLengthBytes,
            cancellationToken);
        if (content.Stream.Length != command.ContentLengthBytes)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail(
                "Uploaded content length does not match the request length.",
                400);
        }

        var code = NormalizeCode(command.TemplateCode);
        var existing = await _store.GetByCodeAndChecksumAsync(code, content.Checksum, cancellationToken);
        if (existing is not null)
        {
            return ApiResult<RobotArtifactTemplateResult>.Success(
                RobotArtifactTemplateResult.FromEntity(existing),
                "Matching template already exists.");
        }

        var id = Guid.NewGuid();
        var storageKey = $"robot-artifact-templates/{id:D}/{content.Checksum}.lua";
        ArtifactObjectWriteResult written;
        try
        {
            written = await _contentService.WriteImmutableAsync(
                storageKey,
                command.ContentType,
                content,
                cancellationToken);
        }
        catch (ArtifactObjectAlreadyExistsException)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Template object already exists.", 409);
        }

        try
        {
            var template = RobotArtifactTemplate.CreateDraft(
                code,
                command.TemplateName,
                written.StorageKey,
                command.FileName,
                written.Checksum,
                NormalizeCode(command.RuntimeTargetCode),
                NormalizeCode(command.MachineModelCode),
                written.ContentLengthBytes,
                command.ExportedAt ?? DateTimeOffset.UtcNow,
                command.Description,
                command.MetadataJson?.Trim());
            template.Id = id;
            template.CreatedByAccountId = command.UserContext.AccountId;

            var inserted = await _store.InsertOrGetExistingAsync(template, cancellationToken);
            if (!inserted.Created)
            {
                await _contentService.DeleteUncommittedObjectAsync(storageKey);
            }

            return ApiResult<RobotArtifactTemplateResult>.Success(
                RobotArtifactTemplateResult.FromEntity(inserted.Template),
                inserted.Created ? "Robot artifact template uploaded." : "Matching template already exists.",
                inserted.Created ? 201 : 200);
        }
        catch (DomainRuleException ex)
        {
            await _contentService.DeleteUncommittedObjectAsync(storageKey);
            return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 400);
        }
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
}
