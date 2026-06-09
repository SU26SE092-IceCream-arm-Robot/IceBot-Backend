using System.Collections.Generic;

namespace Application.Identity.Roles.Results;

public sealed class ManagementRoleResult
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsAssignable { get; init; }
    public IReadOnlyList<string> AllowedScopeTypes { get; init; } = null!;
    public bool RequiresScope { get; init; }
}
