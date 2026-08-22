namespace Application.SalesCatalog.RuntimeMenus.Results;

public sealed class RuntimeMenuResult
{
    public Guid SnapshotId { get; set; }

    public string Revision { get; set; } = null!;

    public Guid KioskId { get; set; }

    public DateTimeOffset GeneratedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public List<RuntimeMenuItemResult> Items { get; set; } = new();

    public RuntimeMenuAdmissionResult? Admission { get; set; }
}

public sealed class RuntimeMenuAdmissionResult
{
    public bool CanPlaceOrder { get; set; }
    public bool CanOpenPayment { get; set; }
    public List<RuntimeMenuAdmissionBlockerResult> Blockers { get; set; } = new();
    public DateTimeOffset? EvidenceValidUntil { get; set; }
}

public sealed class RuntimeMenuAdmissionBlockerResult
{
    public string Code { get; set; } = null!;
    public string Scope { get; set; } = null!;
}
