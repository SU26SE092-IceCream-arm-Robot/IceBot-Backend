using Application.Tenants.Abstractions;
using System.Security.Claims;

namespace WebAPI.Middlewares;

/// <summary>
/// Re-evaluates Organization-owned role scopes after JWT authentication. This
/// keeps a pre-transition token from authorizing a suspended or inactive
/// Organization until its normal expiry.
/// </summary>
public sealed class OrganizationAccessScopeMiddleware
{
    private const string RoleScopeClaimType = "role_scope";
    private readonly RequestDelegate _next;

    public OrganizationAccessScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOrganizationAccessStateReader organizationAccess)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var scopes = principal.FindAll(RoleScopeClaimType)
            .Select(claim => TryParseScope(claim.Value))
            .Where(scope => scope is not null)
            .Select(scope => scope!)
            .ToArray();

        if (scopes.Length == 0)
        {
            await _next(context);
            return;
        }

        var globalScopes = scopes.Where(IsGlobalSystemAdminScope).ToArray();
        var tenantScopes = scopes.Except(globalScopes).ToArray();
        var activeTenantScopes = await organizationAccess.FilterActiveScopesAsync(tenantScopes, context.RequestAborted);
        var acceptedScopes = globalScopes.Concat(activeTenantScopes).ToArray();

        context.User = ReplaceRoleClaims(principal, acceptedScopes);
        await _next(context);
    }

    private static ClaimsPrincipal ReplaceRoleClaims(
        ClaimsPrincipal principal,
        IReadOnlyCollection<OrganizationScopeReference> acceptedScopes)
    {
        var identities = principal.Identities
            .Select(identity =>
            {
                var filteredIdentity = new ClaimsIdentity(
                    identity.Claims.Where(claim =>
                        claim.Type != ClaimTypes.Role &&
                        claim.Type != RoleScopeClaimType),
                    identity.AuthenticationType,
                    identity.NameClaimType,
                    identity.RoleClaimType);

                return filteredIdentity;
            })
            .ToList();

        var authenticatedIdentity = identities.FirstOrDefault(identity => identity.IsAuthenticated)
                                  ?? identities.First();
        foreach (var scope in acceptedScopes)
        {
            authenticatedIdentity.AddClaim(new Claim(ClaimTypes.Role, scope.RoleCode));
            authenticatedIdentity.AddClaim(new Claim(RoleScopeClaimType, FormatScope(scope)));
        }

        return new ClaimsPrincipal(identities);
    }

    private static OrganizationScopeReference? TryParseScope(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 4 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return null;
        }

        return new OrganizationScopeReference(
            parts[0],
            ParseNullableId(parts[1]),
            ParseNullableId(parts[2]),
            ParseNullableId(parts[3]));
    }

    private static bool IsGlobalSystemAdminScope(OrganizationScopeReference scope) =>
        string.Equals(scope.RoleCode, "SystemAdmin", StringComparison.OrdinalIgnoreCase) &&
        !scope.OrganizationId.HasValue &&
        !scope.StoreId.HasValue &&
        !scope.KioskId.HasValue;

    private static Guid? ParseNullableId(string value) =>
        value != "*" && Guid.TryParse(value, out var id) ? id : null;

    private static string FormatScope(OrganizationScopeReference scope) =>
        string.Join("|",
            scope.RoleCode,
            scope.OrganizationId?.ToString() ?? "*",
            scope.StoreId?.ToString() ?? "*",
            scope.KioskId?.ToString() ?? "*");
}
