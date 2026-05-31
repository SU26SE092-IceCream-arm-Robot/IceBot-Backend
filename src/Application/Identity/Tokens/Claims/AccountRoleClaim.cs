namespace Application.Identity.Tokens.Claims;

public sealed record AccountRoleClaim(
    string RoleCode,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId);
