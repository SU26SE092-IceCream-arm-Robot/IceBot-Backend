using Application.ProductionConfiguration.Deployments.Abstractions;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class ReconcileAcceptedDeploymentReportTimeoutsCommandHandler
{
    private readonly IConfigurationDeploymentStore _store;
    public ReconcileAcceptedDeploymentReportTimeoutsCommandHandler(IConfigurationDeploymentStore store) => _store = store;

    public async Task<AcceptedDeploymentReportTimeoutResult> HandleAsync(
        DateTimeOffset observedAt,
        TimeSpan reportTimeout,
        int maxDeploymentsPerProfile,
        CancellationToken cancellationToken = default)
    {
        if (reportTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(reportTimeout));
        if (maxDeploymentsPerProfile <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDeploymentsPerProfile));

        var acceptedBefore = observedAt.Subtract(reportTimeout);
        var fullEdgeCount = await _store.FailFullEdgeDeploymentsMissingAcceptedCommandReportAsync(
            acceptedBefore, observedAt, maxDeploymentsPerProfile, cancellationToken);
        var controllerCount = await _store.FailControllerDeploymentsMissingAcceptedCommandReportAsync(
            acceptedBefore, observedAt, maxDeploymentsPerProfile, cancellationToken);
        return new AcceptedDeploymentReportTimeoutResult(fullEdgeCount, controllerCount);
    }
}

public sealed record AcceptedDeploymentReportTimeoutResult(
    int FullEdgeDeploymentCount,
    int ControllerDeploymentCount)
{
    public int TotalCount => FullEdgeDeploymentCount + ControllerDeploymentCount;
}
