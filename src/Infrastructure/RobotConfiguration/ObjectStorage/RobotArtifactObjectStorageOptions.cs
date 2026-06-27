using System.ComponentModel.DataAnnotations;

namespace Infrastructure.RobotConfiguration.ObjectStorage;

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

    [Range(60, 604800)]
    public int DownloadUrlExpirySeconds { get; init; } = 900;
}
