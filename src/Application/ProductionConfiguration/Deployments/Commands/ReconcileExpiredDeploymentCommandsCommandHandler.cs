using Application.EdgeIntegration.Abstractions;
using Application.ProductionConfiguration.Deployments.Abstractions;
using Domain.Common;
using Domain.ProductionConfiguration.Enums;
using Domain.Sync.Enums;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class ReconcileExpiredDeploymentCommandsCommandHandler
{
    private readonly IEdgeCommandStore _edgeCommandStore;
    private readonly IConfigurationDeploymentStore _deploymentStore;

    public ReconcileExpiredDeploymentCommandsCommandHandler(
        IEdgeCommandStore edgeCommandStore,
        IConfigurationDeploymentStore deploymentStore)
    {
        _edgeCommandStore = edgeCommandStore;
        _deploymentStore = deploymentStore;
    }

    public async Task<DeploymentTimeoutReconciliationResult> HandleAsync(
        DateTimeOffset observedAt,
        int maxCommands,
        CancellationToken cancellationToken = default)
    {
        if (maxCommands <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCommands));

        var commands = await _edgeCommandStore.ListExpiredDeploymentCommandsAsync(
            observedAt, maxCommands, cancellationToken);
        var reconciled = 0;
        var missingDeployments = 0;
        var failures = new List<DeploymentTimeoutReconciliationFailure>();

        foreach (var command in commands)
        {
            try
            {
                if (!command.DeploymentId.HasValue)
                {
                    failures.Add(new DeploymentTimeoutReconciliationFailure(
                        command.Id,
                        "Deployment command has no deployment reference."));
                    continue;
                }

                var wasAlreadyRejectedAsExpired =
                    command.Status == EdgeCommandStatus.Rejected &&
                    string.Equals(command.RejectionCode, "CommandExpired", StringComparison.Ordinal);
                if (!wasAlreadyRejectedAsExpired && !command.RejectIfExpired(observedAt))
                    continue;

                if (command.DeploymentKind == DeploymentCommandTargetKind.FullEdgeConfiguration)
                {
                    var deployment = await _deploymentStore.GetFullEdgeDeploymentForReconciliationAsync(
                        command.DeploymentId.Value, cancellationToken);
                    if (deployment is null)
                    {
                        missingDeployments++;
                        continue;
                    }

                    if (deployment.Status == KioskConfigurationDeploymentStatus.Pending)
                    {
                        deployment.MarkCommandExpired(observedAt);
                        reconciled++;
                    }
                }
                else if (command.DeploymentKind == DeploymentCommandTargetKind.LowCostArtifactSet)
                {
                    var deployment = await _deploymentStore.GetControllerDeploymentForReconciliationAsync(
                        command.DeploymentId.Value, cancellationToken);
                    if (deployment is null)
                    {
                        missingDeployments++;
                        continue;
                    }

                    if (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending)
                    {
                        deployment.MarkCommandExpired(observedAt);
                        reconciled++;
                    }
                }
                else
                {
                    failures.Add(new DeploymentTimeoutReconciliationFailure(
                        command.Id,
                        "Deployment command kind is not supported."));
                }
            }
            catch (DomainRuleException exception)
            {
                failures.Add(new DeploymentTimeoutReconciliationFailure(command.Id, exception.Message));
            }
        }

        await _edgeCommandStore.SaveChangesAsync(cancellationToken);
        return new DeploymentTimeoutReconciliationResult(
            commands.Count,
            reconciled,
            missingDeployments,
            failures);
    }
}

public sealed record DeploymentTimeoutReconciliationResult(
    int ExpiredCommandCount,
    int ReconciledDeploymentCount,
    int MissingDeploymentCount,
    IReadOnlyList<DeploymentTimeoutReconciliationFailure> Failures);

public sealed record DeploymentTimeoutReconciliationFailure(Guid CommandId, string Reason);
