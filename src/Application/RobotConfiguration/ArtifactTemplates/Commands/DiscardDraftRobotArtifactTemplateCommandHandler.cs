using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.Storage.Services;
using Application.Shared.Wrappers;
using Domain.RobotConfiguration.Artifacts;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class DiscardDraftRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly ArtifactUploadContentService _contentService;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public DiscardDraftRobotArtifactTemplateCommandHandler(
        IRobotArtifactTemplateStore store,
        ArtifactUploadContentService contentService,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _contentService = contentService;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactTemplateDiscardResult>> HandleAsync(
        DiscardDraftRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.UserContext.IsSystemAdmin)
        {
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail("Robot artifact template not found.", 404);
        }

        var preparation = await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Template(command.TemplateId)],
            async ct =>
            {
                var template = await _store.GetByIdAsync(command.TemplateId, tracked: true, ct);
                if (template is null)
                    return ApiResult<DiscardPreparation>.Fail("Robot artifact template not found.", 404);
                if (template.Status != RobotArtifactStatus.Draft)
                    return ApiResult<DiscardPreparation>.Fail(
                        "Only draft robot artifact templates can be discarded.", 400);
                var outcome = await _store.DiscardDraftAsync(template, ct);
                return outcome == RobotArtifactTemplateDiscardOutcome.Referenced
                    ? ApiResult<DiscardPreparation>.Fail(
                        "The robot artifact template is referenced and cannot be discarded.", 409)
                    : ApiResult<DiscardPreparation>.Success(
                        new DiscardPreparation(template.StorageKey, template.FileName));
            }, cancellationToken);
        if (!preparation.Succeeded || preparation.Data is null)
            return ApiResult<RobotArtifactTemplateDiscardResult>.Fail(
                preparation.Message ?? "Robot artifact template could not be discarded.", preparation.StatusCode);

        var objectDeleted = await _contentService.TryDeleteObjectAsync(preparation.Data.StorageKey, cancellationToken);
        return ApiResult<RobotArtifactTemplateDiscardResult>.Success(
            new RobotArtifactTemplateDiscardResult
            {
                RobotArtifactTemplateId = command.TemplateId,
                FileName = preparation.Data.FileName,
                ObjectDeleted = objectDeleted
            },
            objectDeleted
                ? "Draft robot artifact template discarded successfully."
                : "Draft robot artifact template metadata discarded; object cleanup is pending.");
    }

    private sealed record DiscardPreparation(string StorageKey, string FileName);

}
