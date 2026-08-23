using Application.ClientDevices.Security;
using Domain.Devices.ClientDevices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Devices.ClientDevices;

/// <summary>
/// Fails startup when an active tablet credential cannot be verified with the
/// configured versioned HMAC keys.
/// </summary>
public sealed class ClientDeviceCredentialKeyValidatorHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ClientDeviceSecurityOptions> securityOptions) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
        var requiredVersions = await dbContext.ClientDeviceCredentials
            .AsNoTracking()
            .Where(credential => credential.Status == ClientDeviceCredentialStatus.Active)
            .Select(credential => credential.HashKeyVersion)
            .Distinct()
            .OrderBy(version => version)
            .ToListAsync(cancellationToken);

        var unavailableVersions = requiredVersions
            .Where(version => !securityOptions.Value.HashKeys.TryGetValue(version, out var key) ||
                              string.IsNullOrWhiteSpace(key))
            .ToArray();
        if (unavailableVersions.Length != 0)
        {
            throw new InvalidOperationException(
                "Active client-device credentials reference unavailable hash-key versions: " +
                string.Join(", ", unavailableVersions) + ".");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
