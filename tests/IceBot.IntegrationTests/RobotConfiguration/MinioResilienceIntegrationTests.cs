using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.Net;
using System.Net.Sockets;
using DotNet.Testcontainers.Builders;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.RobotConfiguration;

public sealed class MinioResilienceIntegrationTests
{
    [IntegrationFact]
    public async Task ReadOnlyStatRecoversWhenMinioBecomesAvailableDuringRetryWindow()
    {
        const string accessKey = "icebot-resilience";
        const string secretKey = "icebot-resilience-secret";
        var hostPort = GetAvailablePort();
        await using var minio = new ContainerBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z")
            .WithEnvironment("MINIO_ROOT_USER", accessKey)
            .WithEnvironment("MINIO_ROOT_PASSWORD", secretKey)
            .WithPortBinding(hostPort, 9000)
            .WithCommand("server", "/data")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(
                request => request.ForPort(9000).ForPath("/minio/health/live")))
            .Build();
        var storage = new MinioArtifactObjectStorage(Options.Create(new RobotArtifactObjectStorageOptions
        {
            Endpoint = $"127.0.0.1:{hostPort}",
            AccessKey = accessKey,
            SecretKey = secretKey,
            BucketName = "icebot-resilience-artifacts",
            ReadRetryCount = 5,
            ReadRetryDelayMilliseconds = 1000
        }));
        var start = Task.Run(async () =>
        {
            await Task.Delay(500);
            await minio.StartAsync();
        });

        var exists = await storage.ExistsAsync($"transient/{Guid.NewGuid():N}.lua");
        await start;

        Assert.False(exists);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
