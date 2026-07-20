using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Domain.RobotConfiguration.ArtifactTemplates;
using Application.RobotConfiguration.ArtifactTemplates.Abstractions;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.Shared.Wrappers;
using Domain.Common;
using Application.Shared.Concurrency;

namespace Application.RobotConfiguration.ArtifactTemplates.Commands;

public sealed class RetireRobotArtifactTemplateCommandHandler
{
    private readonly IRobotArtifactTemplateStore _store;
    private readonly ITechnicalResourceMutationCoordinator _mutations;

    public RetireRobotArtifactTemplateCommandHandler(
        IRobotArtifactTemplateStore store,
        ITechnicalResourceMutationCoordinator mutationCoordinator)
    {
        _store = store;
        _mutations = mutationCoordinator;
    }

    public async Task<ApiResult<RobotArtifactTemplateResult>> HandleAsync(
        RetireRobotArtifactTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!command.UserContext.IsSystemAdmin)
        {
            return ApiResult<RobotArtifactTemplateResult>.Fail("Access denied.", 403);
        }

        return await _mutations.ExecuteAsync(
            [TechnicalResourceMutationIdentity.Template(command.TemplateId)],
            async ct =>
            {
                var template = await _store.GetByIdAsync(command.TemplateId, tracked: true, ct);
                if (template is null)
                    return ApiResult<RobotArtifactTemplateResult>.Fail("Robot artifact template not found.", 404);
                try
                {
                    template.Retire();
                    template.UpdatedByAccountId = command.UserContext.AccountId;
                    await _store.SaveChangesAsync(ct);
                    return ApiResult<RobotArtifactTemplateResult>.Success(RobotArtifactTemplateResult.FromEntity(template));
                }
                catch (DomainRuleException ex) { return ApiResult<RobotArtifactTemplateResult>.Fail(ex.Message, 400); }
            }, cancellationToken);
    }
}
