using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Entities;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class EdgeCommandStore : IEdgeCommandStore
{
    private readonly IceBotDbContext _dbContext;

    public EdgeCommandStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KioskExecutionEndpoint?> GetEndpointForCommandAuthAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public async Task<IReadOnlyList<EdgeCommand>> ListDispatchableAsync(
        Guid kioskId,
        Guid endpointId,
        int maxCommands,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        var commands = await _dbContext.EdgeCommands
            .Include(command => command.DeliveryAttempts)
            .Where(command =>
                command.KioskId == kioskId &&
                command.TargetExecutionEndpointId == endpointId &&
                (command.Status == EdgeCommandStatus.PendingDelivery ||
                    command.Status == EdgeCommandStatus.Delivered))
            .OrderBy(command => command.CreatedAt)
            .Take(maxCommands)
            .ToListAsync(cancellationToken);

        foreach (var command in commands.ToArray())
        {
            if (command.RejectIfExpired(observedAt))
            {
                commands.Remove(command);
            }
        }

        return commands;
    }

    public Task<EdgeCommand?> GetByIdAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands
            .Include(command => command.DeliveryAttempts)
            .FirstOrDefaultAsync(command => command.Id == commandId, cancellationToken);
    }

    public async Task<IReadOnlyList<EdgeCommand>> ListExpiredDeploymentCommandsAsync(
        DateTimeOffset observedAt,
        int maxCommands,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EdgeCommands
            .Where(command =>
                command.CommandType == EdgeCommandType.DeployConfiguration &&
                command.DeploymentId != null &&
                command.DeploymentKind != null &&
                command.CommandExpiryAt != null &&
                command.CommandExpiryAt < observedAt &&
                (command.Status == EdgeCommandStatus.PendingDelivery ||
                    command.Status == EdgeCommandStatus.Delivered ||
                    (command.Status == EdgeCommandStatus.Rejected && command.RejectionCode == "CommandExpired")))
            .OrderBy(command => command.CommandExpiryAt)
            .Take(maxCommands)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(EdgeCommand command, CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AddAsync(command, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
