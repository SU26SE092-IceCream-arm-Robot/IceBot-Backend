using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Domain.RobotConfiguration.ArtifactTemplates;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.Shared.Wrappers;
using Domain.Common;
using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class PublishRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly ArtifactPublicationValidator _publicationValidator;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public PublishRobotArtifactTemplateCommandHandler(
        IRobotArtifactTemplateStore store,
        ArtifactPublicationValidator publicationValidator,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _publicationValidator = publicationValidator;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactTemplateResult>> HandleAsync(
        PublishRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.UserContext.IsSystemAdmin)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Access denied.", 403);
        }

        var observedTemplate = await _store.GetByIdAsync(command.TemplateId, tracked: false, cancellationToken);
        if (observedTemplate is null)
            return ApiResult<RobotArtifactTemplateResult>.Fail("Robot artifact template not found.", 404);
        var observedContractId = observedTemplate.TechnicalContractId;
        var resources = observedContractId.HasValue
            ? new[]
            {
                TechnicalResourceMutationIdentity.Contract(observedContractId.Value),
                TechnicalResourceMutationIdentity.Template(command.TemplateId)
            }
            : [TechnicalResourceMutationIdentity.Template(command.TemplateId)];

        return await _mutations.ExecuteAsync(resources, async ct =>
        {
            var template = await _store.GetByIdAsync(command.TemplateId, tracked: true, ct);
            if (template is null) return ApiResult<RobotArtifactTemplateResult>.Fail("Robot artifact template not found.", 404);
            if (template.TechnicalContractId != observedContractId)
                return ApiResult<RobotArtifactTemplateResult>.Fail(
                    "Robot artifact template technical contract changed concurrently; retry publication.", 409);
            try
            {
                await _publicationValidator.ValidateAsync(template, ct);
                template.Publish();
                template.UpdatedByAccountId = command.UserContext.AccountId;
                await _store.SaveChangesAsync(ct);
                return ApiResult<RobotArtifactTemplateResult>.Success(RobotArtifactTemplateResult.FromEntity(template));
            }
            catch (DomainRuleException ex) { return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 400); }
            catch (ArtifactObjectNotFoundException ex) { return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 409); }
            catch (ArtifactObjectIntegrityException ex) { return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 409); }
            catch (ArtifactObjectStorageUnavailableException ex) { return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 503); }
        }, cancellationToken);
    }
}
