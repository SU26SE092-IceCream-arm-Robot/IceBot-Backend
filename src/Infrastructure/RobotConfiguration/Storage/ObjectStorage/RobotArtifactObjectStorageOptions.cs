using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.RobotConfiguration.Storage.ObjectStorage;

public sealed class RobotArtifactObjectStorageOptions
{
    public const string SectionName = "RobotArtifacts:ObjectStorage";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    public string? DownloadEndpoint { get; init; }

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string BucketName { get; init; } = "icebot-robot-artifacts";

    public bool UseSsl { get; init; }

    public bool? DownloadUseSsl { get; init; }

    public bool AutoCreateBucket { get; init; }

    [Range(0, 5)]
    public int ReadRetryCount { get; init; } = 2;

    [Range(1, 10000)]
    public int ReadRetryDelayMilliseconds { get; init; } = 200;

    [Range(60, 604800)]
    public int DownloadUrlExpirySeconds { get; init; } = 900;

    public bool OrphanCleanupEnabled { get; init; } = true;

    [Range(1, 720)]
    public int OrphanGracePeriodHours { get; init; } = 24;

    [Range(1, 168)]
    public int OrphanCleanupIntervalHours { get; init; } = 24;

    [Range(1, 10000)]
    public int OrphanCleanupMaxDeletesPerRun { get; init; } = 100;
}
