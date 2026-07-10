using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class DiscardDraftRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly ArtifactUploadContentService _contentService;

    public DiscardDraftRobotArtifactTemplateCommandHandler(
        IRobotArtifactTemplateStore store,
        ArtifactUploadContentService contentService)
    {
        _store = store;
        _contentService = contentService;
    }

    public async Task<ApiResult<RobotArtifactTemplateDiscardResult>> HandleAsync(
        DiscardDraftRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.UserContext.IsSystemAdmin)
        {
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail("Robot artifact template not found.", 404);
        }

        var template = await _store.GetByIdAsync(command.TemplateId, tracked: true, cancellationToken);
        if (template is null)
        {
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail("Robot artifact template not found.", 404);
        }

        if (template.Status != RobotArtifactStatus.Draft)
        {
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail(
                "Only draft robot artifact templates can be discarded.",
                400);
        }

        var storageKey = template.StorageKey;
        var fileName = template.FileName;
        var outcome = await _store.DiscardDraftAsync(template, cancellationToken);
        if (outcome == RobotArtifactTemplateDiscardOutcome.Referenced)
        {
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail(
                "The robot artifact template is referenced and cannot be discarded.",
                409);
        }

        var objectDeleted = await _contentService.TryDeleteObjectAsync(storageKey, cancellationToken);
        return ApiResult<RobotArtifactTemplateDiscardResult>.Success(
            new RobotArtifactTemplateDiscardResult
            {
                RobotArtifactTemplateId = command.TemplateId,
                FileName = fileName,
                ObjectDeleted = objectDeleted
            },
            objectDeleted
                ? "Draft robot artifact template discarded successfully."
                : "Draft robot artifact template metadata discarded; object cleanup is pending.");
    }

}
