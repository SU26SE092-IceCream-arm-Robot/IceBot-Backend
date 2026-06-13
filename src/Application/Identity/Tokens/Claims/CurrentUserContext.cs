using System;
using System.Collections.Generic;

namespace Application.Identity.Tokens.Claims;

public sealed class CurrentUserContext
{
    public Guid AccountId { get; init; }

    public bool IsSystemAdmin { get; init; }

    public IReadOnlySet<Guid> AllowedOrganizationIds { get; init; } = new HashSet<Guid>();

    public IReadOnlySet<Guid> AllowedStoreIds { get; init; } = new HashSet<Guid>();

    public IReadOnlySet<Guid> AllowedKioskIds { get; init; } = new HashSet<Guid>();

    public IReadOnlyCollection<UserRoleScope> RoleScopes { get; init; } = Array.Empty<UserRoleScope>();
}

public sealed record UserRoleScope(
    string RoleCode,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId
);
