using Application.Identity.Workforce.Staff;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Identity.Jobs;

public sealed class StaffSessionRevocationReconciler(
    IStaffWorkforceStore accounts,
    IStaffSessionRevoker sessionRevoker,
    ILogger<StaffSessionRevocationReconciler> logger)
{
    public async Task<int> ReconcileAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var accountIds = await accounts.ListDisabledStaffWithActiveSessionsAsync(batchSize, cancellationToken);
        var completed = 0;
        foreach (var accountId in accountIds)
        {
            try
            {
                await sessionRevoker.RevokeAllAsync(
                    accountId,
                    "Staff account is disabled.",
                    cancellationToken);
                completed++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                // The disabled account and active refresh token are durable retry evidence.
                logger.LogWarning(
                    exception,
                    "Staff session revocation will be retried for disabled account {AccountId}.",
                    accountId);
            }
        }

        return completed;
    }
}
