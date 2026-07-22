namespace Application.RobotConfiguration.Storage.Services;

public sealed class UncommittedArtifactObjectSet(
    ArtifactUploadContentService contentService) : IAsyncDisposable
{
    private readonly HashSet<string> _storageKeys = new(StringComparer.Ordinal);
    private bool _committed;

    public void Track(string storageKey)
    {
        if (_committed)
            throw new InvalidOperationException("Committed artifact objects cannot be tracked.");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Artifact object storage key is required.", nameof(storageKey));
        _storageKeys.Add(storageKey);
    }

    public void Commit()
    {
        _committed = true;
        _storageKeys.Clear();
    }

    public async Task CompensateAsync()
    {
        if (_committed)
            return;
        foreach (var storageKey in _storageKeys.ToArray())
            await contentService.DeleteUncommittedObjectAsync(storageKey);
        _storageKeys.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        await CompensateAsync();
    }
}
