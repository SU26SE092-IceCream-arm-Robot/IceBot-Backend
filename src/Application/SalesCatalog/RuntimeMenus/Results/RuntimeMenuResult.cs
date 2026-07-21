namespace Application.SalesCatalog.RuntimeMenus.Results;

public sealed class RuntimeMenuResult
{
    public Guid SnapshotId { get; set; }

    public string Revision { get; set; } = null!;

    public Guid KioskId { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public List<RuntimeMenuItemResult> Items { get; set; } = new();
}
