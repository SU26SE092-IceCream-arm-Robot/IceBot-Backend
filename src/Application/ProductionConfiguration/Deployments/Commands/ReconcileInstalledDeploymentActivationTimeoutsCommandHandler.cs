using Application.ProductionConfiguration.Deployments.Abstractions;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class ReconcileInstalledDeploymentActivationTimeoutsCommandHandler
{
    private readonly IConfigurationDeploymentStore _store;
    public ReconcileInstalledDeploymentActivationTimeoutsCommandHandler(IConfigurationDeploymentStore store) => _store = store;

    public async Task<InstalledDeploymentActivationTimeoutResult> HandleAsync(
        DateTimeOffset observedAt,
        TimeSpan activationTimeout,
        int maxDeploymentsPerProfile,
        CancellationToken cancellationToken = default)
    {
        if (activationTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(activationTimeout));
        if (maxDeploymentsPerProfile <= 0) throw new ArgumentOutOfRangeException(nameof(maxDeploymentsPerProfile));
        var installedBefore = observedAt.Subtract(activationTimeout);
        var fullEdge = await _store.FailFullEdgeDeploymentsMissingActivationReportAsync(installedBefore, observedAt, maxDeploymentsPerProfile, cancellationToken);
        var controller = await _store.FailControllerDeploymentsMissingActivationReportAsync(installedBefore, observedAt, maxDeploymentsPerProfile, cancellationToken);
        return new InstalledDeploymentActivationTimeoutResult(fullEdge, controller);
    }
}

public sealed record InstalledDeploymentActivationTimeoutResult(int FullEdgeDeploymentCount, int ControllerDeploymentCount)
{
    public int TotalCount => FullEdgeDeploymentCount + ControllerDeploymentCount;
}
