namespace Application.Tenants.TenantTree.Results;

public sealed class TenantTreeResult
{
    public IReadOnlyList<TenantTreeOrganizationResult> Organizations { get; init; } =
        Array.Empty<TenantTreeOrganizationResult>();
}

public sealed class TenantTreeOrganizationResult
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Status { get; init; } = null!;
    public IReadOnlyList<TenantTreeStoreResult> Stores { get; init; } =
        Array.Empty<TenantTreeStoreResult>();
}

public sealed class TenantTreeStoreResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Status { get; init; } = null!;
    public IReadOnlyList<TenantTreeKioskResult> Kiosks { get; init; } =
        Array.Empty<TenantTreeKioskResult>();
}

public sealed class TenantTreeKioskResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string Status { get; init; } = null!;
}
