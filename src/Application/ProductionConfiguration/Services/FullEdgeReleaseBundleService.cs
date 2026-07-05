using System.IO.Compression;
using System.Security.Cryptography;
using Application.RobotConfiguration.Abstractions;
using Domain.Common;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Manifests;
using Domain.ProductionConfiguration.ValueObjects;

namespace Application.ProductionConfiguration.Services;

public sealed class FullEdgeReleaseBundleService
{
    private const int FormatVersion = 1;
    private const long MaximumBundleSizeBytes = 100 * 1024 * 1024;
    private static readonly DateTimeOffset StableEntryTimestamp = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IArtifactObjectStorage _storage;

    public FullEdgeReleaseBundleService(IArtifactObjectStorage storage)
    {
        _storage = storage;
    }

    public async Task<FullEdgeReleaseBundleDescriptor> BuildAndStoreAsync(
        ConfigurationRelease release,
        IReadOnlyDictionary<Guid, PublishedRobotProgramSnapshot> programSnapshots,
        string releaseContentManifestJson,
        CancellationToken cancellationToken = default)
    {
        var artifacts = programSnapshots.Values
            .SelectMany(program => program.Artifacts)
            .DistinctBy(artifact => artifact.RobotArtifactId)
            .OrderBy(artifact => artifact.RobotArtifactId)
            .ToArray();
        if (artifacts.Length == 0)
        {
            throw new DomainRuleException("A Full Edge release bundle requires at least one robot artifact.");
        }

        if (artifacts.Sum(artifact => artifact.ContentLengthBytes) > MaximumBundleSizeBytes)
        {
            throw new DomainRuleException($"Full Edge release artifact content exceeds the {MaximumBundleSizeBytes}-byte limit.");
        }

        var stagingPath = Path.Combine(Path.GetTempPath(), $"icebot-release-{Guid.NewGuid():N}.zip");
        try
        {
            await using var bundle = new FileStream(
                stagingPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using (var archive = new ZipArchive(bundle, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var artifact in artifacts)
                {
                    var bytes = await _storage.ReadBytesAsync(
                        artifact.StorageKey,
                        artifact.ContentLengthBytes,
                        cancellationToken);
                    var checksum = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                    if (!string.Equals(checksum, artifact.Checksum, StringComparison.Ordinal) ||
                        bytes.LongLength != artifact.ContentLengthBytes)
                    {
                        throw new ArtifactObjectIntegrityException(
                            artifact.StorageKey,
                            "A robot artifact failed checksum or size verification while building the Full Edge release bundle.");
                    }

                    var entryName = $"artifacts/{artifact.RobotArtifactId:D}.lua";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    entry.LastWriteTime = StableEntryTimestamp;
                    await using (var target = entry.Open())
                    {
                        await target.WriteAsync(bytes, cancellationToken);
                    }

                }

                var manifestEntry = archive.CreateEntry("release-content-manifest.json", CompressionLevel.Optimal);
                manifestEntry.LastWriteTime = StableEntryTimestamp;
                await using var manifestStream = manifestEntry.Open();
                await using var manifestContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(releaseContentManifestJson));
                await manifestContent.CopyToAsync(manifestStream, cancellationToken);
            }

            if (bundle.Length > MaximumBundleSizeBytes)
            {
                throw new DomainRuleException($"Full Edge release bundle exceeds the {MaximumBundleSizeBytes}-byte limit.");
            }

            bundle.Position = 0;
            var bundleChecksum = Convert.ToHexString(await SHA256.HashDataAsync(bundle, cancellationToken)).ToLowerInvariant();
            bundle.Position = 0;
            var storageKey = $"robot-artifacts/release-bundles/{release.OrganizationId:D}/{release.Id:D}/{bundleChecksum}.zip";
            if (!await _storage.ExistsAsync(storageKey, cancellationToken))
            {
                try
                {
                    await _storage.WriteImmutableAsync(
                        new ArtifactObjectWriteRequest(storageKey, "application/zip", bundle.Length, bundleChecksum),
                        bundle,
                        cancellationToken);
                }
                catch (ArtifactObjectAlreadyExistsException)
                {
                    // A concurrent publisher created the same immutable bundle.
                }
            }

            return new FullEdgeReleaseBundleDescriptor(
                FormatVersion,
                storageKey,
                bundleChecksum,
                bundle.Length,
                artifacts.Length);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }
}
