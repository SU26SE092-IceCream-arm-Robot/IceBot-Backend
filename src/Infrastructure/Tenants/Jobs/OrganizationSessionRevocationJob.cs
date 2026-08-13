using Application.Identity.Tokens.Services;
using Application.Tenants.Abstractions;
using Domain.Tenants.Enums;
using Infrastructure.Operations.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Infrastructure.Tenants.Jobs;

public sealed class OrganizationSessionRevocationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<OrganizationSessionRevocationOptions> options,
    ILogger<OrganizationSessionRevocationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));
        do
        {
            await ReconcileAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var candidateFailures = 0;
        try
        {
            await using var discoveryScope = scopeFactory.CreateAsyncScope();
            var discoveryStore = discoveryScope.ServiceProvider.GetRequiredService<IOrganizationStore>();
            var dueTransitions = await discoveryStore.ListDueSessionRevocationTransitionsAsync(
                DateTimeOffset.UtcNow,
                options.Value.BatchSize,
                cancellationToken);

            foreach (var dueTransition in dueTransitions)
            {
                try
                {
                    if (!await ProcessAsync(dueTransition.Id, cancellationToken))
                    {
                        candidateFailures++;
                        OperationalAutomationMetrics.RecordCandidateFailure("organization_session_revocation");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    candidateFailures++;
                    OperationalAutomationMetrics.RecordCandidateFailure("organization_session_revocation");
                    logger.LogError(
                        exception,
                        "Organization session revocation processing failed for transition {TransitionId}.",
                        dueTransition.Id);
                }
            }

            OperationalAutomationMetrics.RecordRun(
                "organization_session_revocation",
                candidateFailures == 0 ? "succeeded" : "partial",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Organization session revocation reconciliation failed.");
            OperationalAutomationMetrics.RecordRun("organization_session_revocation", "failed", stopwatch.Elapsed);
        }
    }

    private async Task<bool> ProcessAsync(Guid transitionId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOrganizationStore>();
        var refreshTokens = scope.ServiceProvider.GetRequiredService<RefreshTokenService>();

        return await store.ExecuteInTransactionAsync(async () =>
        {
            var transition = await store.GetStatusTransitionByIdAsync(
                transitionId,
                asNoTracking: false,
                cancellationToken);
            if (transition is null || transition.SessionRevocationStatus == OrganizationLifecycleSideEffectStatus.Completed)
            {
                return true;
            }

            try
            {
                var accountIds = await store.ListAccountIdsWithOrganizationScopeAsync(
                    transition.OrganizationId,
                    cancellationToken);
                foreach (var accountId in accountIds)
                {
                    await refreshTokens.RevokeAllForAccountAsync(
                        accountId,
                        "Organization access is unavailable.",
                        ipAddress: null,
                        userAgent: null);
                }

                transition.SessionRevocationAttemptCount++;
                transition.SessionRevocationStatus = OrganizationLifecycleSideEffectStatus.Completed;
                transition.SessionRevocationCompletedAt = DateTimeOffset.UtcNow;
                transition.NextSessionRevocationAttemptAt = null;
                transition.SessionRevocationLastError = null;
                await store.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Revoked active refresh sessions for Organization {OrganizationId}, transition {TransitionId}, accounts {AccountCount}.",
                    transition.OrganizationId,
                    transition.Id,
                    accountIds.Count);
                return true;
            }
            catch (Exception exception)
            {
                transition.SessionRevocationAttemptCount++;
                transition.SessionRevocationStatus = OrganizationLifecycleSideEffectStatus.RetryScheduled;
                transition.NextSessionRevocationAttemptAt = DateTimeOffset.UtcNow.AddSeconds(options.Value.RetryDelaySeconds);
                transition.SessionRevocationLastError = exception.Message[..Math.Min(exception.Message.Length, 2000)];
                await store.SaveChangesAsync(cancellationToken);
                logger.LogWarning(
                    exception,
                    "Organization session revocation will retry for transition {TransitionId}.",
                    transition.Id);
                return false;
            }
        }, cancellationToken);
    }
}
