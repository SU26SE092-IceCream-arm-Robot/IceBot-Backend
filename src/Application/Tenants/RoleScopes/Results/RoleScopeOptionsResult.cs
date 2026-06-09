using System;
using System.Collections.Generic;

namespace Application.Tenants.RoleScopes.Results;

public sealed class RoleScopeOptionsResult
{
    public string RoleCode { get; init; } = null!;
    public IReadOnlyList<string> AllowedScopeTypes { get; init; } = null!;
    public bool RequiresScope { get; init; }
    public IReadOnlyList<RoleScopeOrganizationResult> Organizations { get; init; } = null!;
}

public sealed class RoleScopeOrganizationResult
{
    public Guid Id { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public IReadOnlyList<RoleScopeStoreResult> Stores { get; init; } = null!;
}

public sealed class RoleScopeStoreResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public IReadOnlyList<RoleScopeKioskResult> Kiosks { get; init; } = null!;
}

public sealed class RoleScopeKioskResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}
