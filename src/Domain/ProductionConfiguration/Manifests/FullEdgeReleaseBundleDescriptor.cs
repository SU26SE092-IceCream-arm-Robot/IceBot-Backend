namespace Domain.ProductionConfiguration.Manifests;

public sealed record FullEdgeReleaseBundleDescriptor(
    int FormatVersion,
    string StorageKey,
    string Checksum,
    long ContentLengthBytes,
    int ArtifactCount);
