using Application.RobotConfiguration.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.RobotConfiguration.ObjectStorage;

public sealed class RobotArtifactObjectStorageStartupValidator : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RobotArtifactObjectStorageStartupValidator> _logger;

    public RobotArtifactObjectStorageStartupValidator(
        IServiceScopeFactory scopeFactory,
        ILogger<RobotArtifactObjectStorageStartupValidator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IArtifactObjectStorage>();
        await storage.EnsureReadyAsync(cancellationToken);
        _logger.LogInformation("Robot artifact object storage startup validation succeeded.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
