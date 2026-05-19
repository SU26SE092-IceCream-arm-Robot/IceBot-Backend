using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authorization;

public sealed class ScopedRoleRequirement : IAuthorizationRequirement
{
    public ScopedRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public IReadOnlyCollection<string> AllowedRoles { get; }
}
